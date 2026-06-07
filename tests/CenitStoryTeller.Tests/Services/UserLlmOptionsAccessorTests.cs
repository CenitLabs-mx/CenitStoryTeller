using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CenitStoryTeller.Core.Llm;
using CenitStoryTeller.Data.Entities;
using CenitStoryTeller.Data.Services;
using CenitStoryTeller.Tests.Fakes;
using Xunit;

namespace CenitStoryTeller.Tests.Services;

public class UserLlmOptionsAccessorTests : IDisposable
{
    private readonly TestDb _test = TestDb.Crear();
    public void Dispose() => _test.Dispose();

    private static readonly LlmOptions Fallback = new()
    {
        Provider = "openai",
        ApiKey = "fallback-key",
        ModelDraft = "fallback-draft",
        ModelReview = "fallback-review"
    };

    private UserLlmOptionsAccessor Construir(StubCurrentUser user, TestDb.Suite suite) =>
        new(user, suite.ConfigLlm, Options.Create(Fallback));

    // El FK ConfiguracionLlm → AspNetUsers exige que el Usuario exista.
    private void SeedearUsuario(Guid uid)
    {
        using var ctx = _test.NuevoCtx();
        ctx.Users.Add(new Usuario
        {
            Id = uid,
            UserName = $"u-{uid:N}",
            NormalizedUserName = $"U-{uid:N}".ToUpperInvariant(),
            Email = $"{uid:N}@example.com",
            NormalizedEmail = $"{uid:N}@example.com".ToUpperInvariant(),
            EmailConfirmed = true
        });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task SinUsuario_DevuelveFallbackDeAppsettings()
    {
        var suite = _test.NuevaSuite();
        var accessor = Construir(StubCurrentUser.Anonymous(), suite);

        var opt = await accessor.ObtenerAsync();

        Assert.Equal("fallback-key", opt.ApiKey);
        Assert.Equal("fallback-draft", opt.ModelDraft);
    }

    [Fact]
    public async Task UsuarioSinConfig_DevuelveFallback()
    {
        var uid = Guid.NewGuid();
        var suite = _test.NuevaSuite(StubCurrentUser.De(uid));
        var accessor = Construir(StubCurrentUser.De(uid), suite);

        var opt = await accessor.ObtenerAsync();

        Assert.Equal("fallback-key", opt.ApiKey);
    }

    [Fact]
    public async Task UsuarioConConfig_DevuelveLaSuya()
    {
        var uid = Guid.NewGuid();
        SeedearUsuario(uid);
        var suite = _test.NuevaSuite(StubCurrentUser.De(uid));

        await suite.ConfigLlm.GuardarAsync(new ConfiguracionLlm
        {
            UsuarioId = uid,
            Provider = "gemini",
            ApiKey = "user-key",
            ModelDraft = "gemini-flash",
            ModelReview = "gemini-pro"
        });
        await suite.Uow.SaveChangesAsync();

        var accessor = Construir(StubCurrentUser.De(uid), suite);
        var opt = await accessor.ObtenerAsync();

        Assert.Equal("gemini", opt.Provider);
        Assert.Equal("user-key", opt.ApiKey);
        Assert.Equal("gemini-flash", opt.ModelDraft);
        Assert.Equal("gemini-pro", opt.ModelReview);
    }

    [Fact]
    public async Task UsuarioConApiKeyVacia_CaeAlFallback()
    {
        // ApiKey vacía se trata como "no configurado todavía" — protege contra
        // guardar accidentalmente una config a medias y dejar sin servicio.
        var uid = Guid.NewGuid();
        SeedearUsuario(uid);
        var suite = _test.NuevaSuite(StubCurrentUser.De(uid));

        await suite.ConfigLlm.GuardarAsync(new ConfiguracionLlm
        {
            UsuarioId = uid,
            Provider = "openai",
            ApiKey = "",
            ModelDraft = "x",
            ModelReview = "y"
        });
        await suite.Uow.SaveChangesAsync();

        var accessor = Construir(StubCurrentUser.De(uid), suite);
        var opt = await accessor.ObtenerAsync();

        Assert.Equal("fallback-key", opt.ApiKey);
    }

    [Fact]
    public async Task UpsertReemplaza_NoCrearNuevaFila()
    {
        var uid = Guid.NewGuid();
        SeedearUsuario(uid);
        var suite = _test.NuevaSuite(StubCurrentUser.De(uid));

        await suite.ConfigLlm.GuardarAsync(new ConfiguracionLlm
        {
            UsuarioId = uid, Provider = "openai", ApiKey = "k1",
            ModelDraft = "a", ModelReview = "b"
        });
        await suite.Uow.SaveChangesAsync();

        await suite.ConfigLlm.GuardarAsync(new ConfiguracionLlm
        {
            UsuarioId = uid, Provider = "gemini", ApiKey = "k2",
            ModelDraft = "c", ModelReview = "d"
        });
        await suite.Uow.SaveChangesAsync();

        var ahora = await suite.ConfigLlm.ObtenerPorUsuarioAsync(uid);
        Assert.NotNull(ahora);
        Assert.Equal("gemini", ahora!.Provider);
        Assert.Equal("k2", ahora.ApiKey);
        Assert.Equal(1, suite.Db.ConfiguracionesLlm.Count(c => c.UsuarioId == uid));
    }
}
