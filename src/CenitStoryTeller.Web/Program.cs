using CenitStoryTeller.Web.Components;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Core.AcidTests;
using CenitStoryTeller.Data;
using CenitStoryTeller.Data.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddNovelaLlm(builder.Configuration);
builder.Services.AddNovelaAcidTests();
builder.Services.AddNovelaData(builder.Configuration);
builder.Services.AddNovelaRepositories();

var app = builder.Build();

// Modo migración: `dotnet run -- --migrate` aplica migraciones y siembra el demo, luego sale.
// El arranque normal NO toca el esquema — eso es responsabilidad del operador (CI, k8s init,
// docker entrypoint dedicado) para evitar carreras entre instancias.
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NovelaDbContext>();
    app.Logger.LogInformation("Applying pending migrations...");
    db.Database.Migrate();
    app.Logger.LogInformation("Seeding East Lynne demo (idempotent)...");
    await EastLynneSeeder.SeedAsync(db);
    app.Logger.LogInformation("Migrate + seed done. Exiting.");
    return;
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
    .AddInteractiveServerRenderMode();

app.Run();
