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

// Aplica migraciones pendientes al arrancar (cómodo para Docker).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NovelaDbContext>();
    db.Database.Migrate();

    // Carga el ejemplo de dominio público si la BD está vacía (idempotente).
    await EastLynneSeeder.SeedAsync(db);
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
