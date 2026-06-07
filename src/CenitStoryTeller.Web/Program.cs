using CenitStoryTeller.Web;
using CenitStoryTeller.Web.Components;
using CenitStoryTeller.Web.Email;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Core.AcidTests;
using CenitStoryTeller.Data;
using CenitStoryTeller.Data.Entities;
using CenitStoryTeller.Data.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddNovelaLlm(builder.Configuration);
builder.Services.AddNovelaAcidTests();
builder.Services.AddNovelaData(builder.Configuration);
builder.Services.AddNovelaRepositories();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Identity sobre NovelaDbContext. Reglas de password relajadas en dev — en prod
// se endurecen vía configuración. Email confirmation requerido: el flujo de
// registro envía un link de confirmación antes de habilitar login.
builder.Services.AddIdentity<Usuario, IdentityRole<Guid>>(o =>
    {
        o.SignIn.RequireConfirmedEmail = true;
        o.Password.RequireDigit = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireUppercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequiredLength = 8;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<NovelaDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/login";
    o.LogoutPath = "/logout";
    o.AccessDeniedPath = "/login";
});

// Email: SendGrid si hay API key configurada, console (a logs) si no.
builder.Services.Configure<SendGridOptions>(builder.Configuration.GetSection(SendGridOptions.SectionName));
var sendGridKey = builder.Configuration["SendGrid:ApiKey"];
if (!string.IsNullOrWhiteSpace(sendGridKey))
    builder.Services.AddSingleton<IAppEmailSender, SendGridEmailSender>();
else
    builder.Services.AddSingleton<IAppEmailSender, ConsoleEmailSender>();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.IEmailSender<Usuario>, IdentityEmailSender>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
