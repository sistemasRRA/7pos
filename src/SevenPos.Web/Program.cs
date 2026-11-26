using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Data;
using SevenPos.Core.Guests;
using SevenPos.Core.Organization;
using SevenPos.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Central PostgreSQL database. Locally the connection string comes from user secrets
// (outside the repo, see docs/getting-started.md); in other environments from the
// ConnectionStrings__Pos environment variable. It is never stored in the repository.
var connectionString = builder.Configuration.GetConnectionString("Pos")
    ?? throw new InvalidOperationException(
        "Missing connection string 'Pos'. For local development run: " +
        "dotnet user-secrets set \"ConnectionStrings:Pos\" \"Host=localhost;Port=5432;Database=sevenpos;Username=sevenpos;Password=<your password>\" --project src/SevenPos.Web");
builder.Services.AddDbContextFactory<PosDbContext>(o => PosDatabase.Configure(o, connectionString));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<GuestService>();
// Who is logged in. Scoped = one per browser connection (Blazor Server circuit).
builder.Services.AddScoped<PosSession>();

var app = builder.Build();

// Create/update tables and load sample data on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PosDbContext>>().CreateDbContext();
    db.Database.Migrate();
    await SeedData.LoadAsync(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(SevenPos.UI.UiAssembly).Assembly);

app.Run();
