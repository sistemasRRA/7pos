using Microsoft.EntityFrameworkCore;
using SevenPos.Core.Data;
using SevenPos.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Local SQLite database (a single file, no server).
builder.Services.AddDbContextFactory<PosDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Pos")));

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
