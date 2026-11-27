using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SevenPos.Core.Data;

namespace SevenPos.Core.Tickets;

/// <summary>One physical garment the cashier received, with the extras the customer chose.</summary>
public record NewGarment(Guid ProductId, IReadOnlyCollection<Guid> ModifierIds);

public record NewTicket(Guid CashierId, Guid GuestId, IReadOnlyList<NewGarment> Garments);

public enum TicketError { NotAllowed, GuestNotFound, NoGarments, ProductNotAvailable, ModifierNotAllowed, MoreThanOneOptionPerGroup }

public record TicketCreation(Ticket? Ticket, TicketError? Error)
{
    public bool Succeeded => Ticket is not null;
}

/// <summary>Ticket capture at the counter. Rules: docs/features/ticket-wizard.md.</summary>
public class TicketService(IDbContextFactory<PosDbContext> dbFactory, PriceListService prices, TimeProvider clock)
{
    /// <summary>No 0/O, 1/I/L: the customer reads this code aloud or types it.</summary>
    private const string PublicOrderAlphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";
    private const int PublicOrderLength = 8;

    /// <summary>
    /// How many times to retry when another terminal took the same folio (or, very rarely,
    /// the same public order ID) between our read and our insert.
    /// </summary>
    private const int MaxAttempts = 10;

    /// <summary>
    /// Creates a ticket with one row per garment. Prices always come from the branch's
    /// price list at this moment, never from the screen (ADR 0011), and are frozen on
    /// the garment rows.
    /// </summary>
    public async Task<TicketCreation> CreateAsync(NewTicket request)
    {
        Ticket? ticket = null;
        TicketError? error = null;

        for (var attempt = 1; ticket is null && error is null; attempt++)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            try
            {
                (ticket, error) = await TryCreateAsync(db, request);
            }
            catch (DbUpdateException e) when (
                e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } && attempt < MaxAttempts)
            {
                // Someone else got this folio first. Start over with a fresh context and the next folio.
            }
        }

        return new(ticket, error);
    }

    private async Task<TicketCreation> TryCreateAsync(PosDbContext db, NewTicket request)
    {
        var cashier = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == request.CashierId);
        if (cashier is not { IsActive: true } || cashier.Role == UserRole.Marketing) return new(null, TicketError.NotAllowed);
        if (request.Garments.Count == 0) return new(null, TicketError.NoGarments);

        var guest = await db.Guests.SingleOrDefaultAsync(g => g.Id == request.GuestId && g.IsActive);
        if (guest is null) return new(null, TicketError.GuestNotFound);

        var catalog = (await prices.GetBranchCatalogAsync(cashier.BranchId)).ToDictionary(e => e.Product.Id);
        var priced = new List<(Guid ProductId, PriceResult Price)>();
        foreach (var requested in request.Garments)
        {
            if (!catalog.TryGetValue(requested.ProductId, out var entry)) return new(null, TicketError.ProductNotAvailable);

            var offered = entry.Product.Modifiers.ToDictionary(pm => pm.ModifierId, pm => pm.Modifier!);
            var chosen = new List<Modifier>();
            foreach (var modifierId in requested.ModifierIds.Distinct())
            {
                if (!offered.TryGetValue(modifierId, out var modifier)) return new(null, TicketError.ModifierNotAllowed);
                chosen.Add(modifier);
            }
            // Each group is one question (Tamaño, Almidón…): at most one answer.
            if (chosen.GroupBy(m => m.ModifierGroupId).Any(g => g.Count() > 1)) return new(null, TicketError.MoreThanOneOptionPerGroup);
            priced.Add((entry.Product.Id, PriceCalculator.Calculate(entry.Price, chosen)));
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var branchCode = await db.Branches.Where(b => b.Id == cashier.BranchId).Select(b => b.Code).SingleAsync();
        var folio = (await db.Tickets.Where(t => t.BranchId == cashier.BranchId).MaxAsync(t => (int?)t.Folio) ?? 0) + 1;

        // The shift whose drawer is open when the ticket is created (none yet: payments will require one).
        var shiftId = await db.Shifts
            .Where(s => s.BranchId == cashier.BranchId && s.ClosedAtUtc == null)
            .Select(s => (Guid?)s.Id)
            .SingleOrDefaultAsync();

        var ticket = new Ticket
        {
            Folio = folio,
            PublicOrderId = RandomNumberGenerator.GetString(PublicOrderAlphabet, PublicOrderLength),
            BranchId = cashier.BranchId,
            GuestId = guest.Id,
            CashierId = cashier.Id,
            ShiftId = shiftId,
            CreatedAtUtc = now,
            Garments = priced.Select((g, index) => new Garment
            {
                Code = GarmentCode(branchCode, BusinessTime.LocalDate(now), folio, index + 1),
                ProductId = g.ProductId,
                ListPrice = g.Price.BasePrice,
                PriceCharged = g.Price.Total,
                Modifiers = g.Price.Charges
                    .Select(c => new GarmentModifier { ModifierId = c.Modifier.Id, ChargeApplied = c.Amount })
                    .ToList(),
            }).ToList(),
        };
        // Discounts arrive with roadmap 1b; until then the customer owes the full total.
        ticket.Subtotal = ticket.Garments.Sum(g => g.PriceCharged);
        ticket.Discounts = 0;
        ticket.Total = ticket.Subtotal - ticket.Discounts;
        ticket.BalanceDue = ticket.Total;

        db.Tickets.Add(ticket);
        guest.LastVisitUtc = now;
        db.AuditLogs.Add(new AuditLog
        {
            UserId = cashier.Id,
            BranchId = cashier.BranchId,
            Action = "TICKET_CREATED",
            NewValue = JsonSerializer.Serialize(new
            {
                ticketId = ticket.Id, folio, total = ticket.Total, garments = ticket.Garments.Count,
            }),
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync();
        return new(ticket, null);
    }

    /// <summary>The ticket with its guest, and its garments in label order with their products and extras.</summary>
    public async Task<Ticket?> GetAsync(Guid ticketId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var ticket = await db.Tickets
            .Include(t => t.Guest)
            .Include(t => t.Garments).ThenInclude(g => g.Product)
            .Include(t => t.Garments).ThenInclude(g => g.Modifiers).ThenInclude(m => m.Modifier).ThenInclude(m => m!.Group)
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null) return null;

        ticket.Garments = ticket.Garments.OrderBy(g => g.Code).ToList();
        foreach (var garment in ticket.Garments)
            garment.Modifiers = garment.Modifiers.OrderBy(m => m.Modifier!.Group!.SortOrder).ThenBy(m => m.Modifier!.SortOrder).ToList();
        return ticket;
    }

    /// <summary>
    /// The label printed on each garment: [BRANCH]-[YYYYMMDD]-[FOLIO]-[SEQ], with the local
    /// date the ticket was created. E.g. DEMO-20261126-000042-03.
    /// </summary>
    public static string GarmentCode(string branchCode, DateOnly localDate, int folio, int sequence) =>
        $"{branchCode}-{localDate:yyyyMMdd}-{folio:D6}-{sequence:D2}";
}
