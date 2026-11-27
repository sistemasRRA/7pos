using SevenPos.Core.Tickets;

namespace SevenPos.UI.Cash;

/// <summary>What PaymentForm hands to its page: the payment (null = none now) and, for cash, the change to give back.</summary>
public record PaymentInput(NewPayment? Payment, decimal? Change);
