# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] — 2026-06-07

First public-ready cut. Multi-tenant, per-user LLM config, the full
generate → acid → regenerate-with-observations loop, and the acid test
now evaluates against past + future of the obra (not the chapter in
isolation).

### Added — Auth and multi-tenancy
- ASP.NET Identity with email confirmation and password reset.
  Identity-aware `CenitStoryTellerDbContext` (`IdentityDbContext<Usuario,...>`).
- `Obra.UsuarioId` nullable for tenant ownership; `NULL` = global demo.
  East Lynne stays demo-visible to everyone but read-only.
- `ICurrentUser` accessor (`HttpCurrentUser` in Web, `AnonymousCurrentUser`
  fallback). Global query filter on `Obra` combines soft-delete with
  tenancy: `EliminadaEn == null AND (UsuarioId == null OR == current)`.
- `ObraRepository` auto-stamps `UsuarioId` on `AddAsync` and rejects
  mutations of demos or other users' obras with
  `UnauthorizedAccessException`.
- Auth pages (Login, Register, RegisterConfirmation, ConfirmEmail,
  ForgotPassword, ResetPassword, Logout) as Blazor SSR with MudBlazor.
- Render mode refactor: SSR is the default, individual interactive
  pages opt in via `@rendermode InteractiveServer` — needed so the
  Identity cookie sign-in actually runs during the HTTP request.
- `IAppEmailSender` abstraction with `ConsoleEmailSender` (dev) and
  `SendGridEmailSender` (prod, via `SendGrid` SDK). Bridged into
  Identity through `IdentityEmailSender`.

### Added — Per-user LLM configuration
- `ConfiguracionLlm` entity (1:1 with `Usuario`, unique index, cascade).
  Stores Provider / ApiKey / BaseUrl / ModelDraft / ModelReview.
- `ILlmOptionsAccessor` (Core) and `ILlmClientFactory` (Core async
  factory). Default `AppsettingsLlmOptionsAccessor` reads from
  `appsettings.json`; `UserLlmOptionsAccessor` in Data prefers the
  current user's row, falls back to appsettings when none.
- `ILlmClient` is no longer registered directly. `GeneracionService`,
  `ModernizacionService`, `AcidTestRunner`, and the inedita wizard
  inject `ILlmClientFactory` + `ILlmOptionsAccessor` and resolve per
  call.
- `/configuracion` page (MudBlazor): provider dropdown, API key,
  models, BaseUrl, "Probar conexión" button that pings the model.

### Added — Workflow
- `GenerarBorradoresPendientesAsync(obraId, IProgress<BorradorProgreso>)`:
  drafts v1 of every chapter that doesn't have a version yet, one at a
  time, reporting per-chapter progress. Idempotent.
- Auto-draft wired into the modernization page and the inedita wizard
  (both AI and manual branches). Synchronous from the user's POV —
  Blazor Server keeps the page alive over the multi-minute LLM run
  via SignalR. Per-chapter progress bar in both flows.
- `RegenerarConObservacionesAsync(versionAnteriorId, observaciones)`:
  produces a new `CapituloVersion` whose prompt to the model includes
  both the previous failed prose and the user's observations.
- `VersionPanel` surfaces a "Regenerar con observaciones" block when
  the acid verdict is anything other than `Aprobado`. Hallazgos +
  Parches arrive pre-filled in an editable textarea.

### Added — Acid test sees past + future
- `ICompendioService` produces `Compendio(Pasado, Futuro)` for an
  obra at a given chapter Orden:
  - **Pasado**: LLM-summarized condensation of the `EsFinal` versions
    of all earlier chapters. Uses `ModelDraft`, temperature 0.2.
  - **Futuro**: deterministic listing of `Estado != Validado` beats
    with `Orden > current` — no LLM.
- Cached in `IMemoryCache` keyed on a SHA256 of the approved version
  ids in scope. Same set => cache hit. Approve a new chapter and the
  key shifts to a miss.
- `AcidContext` gains `Pasado` and `Futuro` (init-only, default empty
  → backward compatible). `AcidTestBase.ConstruirUser` injects them
  into every dimensional prompt.
- New `ICapituloRepository.ListVersionesFinalesPorObraAsync` so the
  compendio service has a dedicated finals query without overloading
  `GetConCanonAsync`.

### Added — UI navigation
- MudBlazor app shell: `MudAppBar` with brand link, hamburger, user
  menu (Configuración, Salir). `MudDrawer` in mini-variant: Inicio,
  Nueva obra, Configuración. Auth-aware.
- Obra workspace: `MudBreadcrumbs` (Inicio › Obra) + `MudTabs` with
  six tabs — Capítulos, Personajes, Ubicaciones, Beats, **Eventos**,
  **Auditoría**. Eventos surfaces canon data the user couldn't reach
  before; Auditoría links to the existing bitácora.
- `Pasos.razor` rebuilt with `MudTimeline`. `Versiones.razor` gains
  the breadcrumb trail back to Obra and Inicio.

### Added — Beat structure editing
- `IBeatService` with two operations that keep `Orden` contiguous:
  - `InsertarEnPosicionAsync(obraId, posicion, nuevo)`: bumps Orden
    on every beat with `Orden >= posicion`, adds the new one in the
    hole, and creates a matching capítulo at the same position
    (renumbering existing capítulos too).
  - `MoverAsync(beatId, ±1)`: swaps Orden with the neighbor and
    mirrors the swap on the aligned capítulos.
- `ObraDetalle` beats tab: ↑/↓ buttons per row (disabled at extremes)
  and "Insertar aquí" links between every row + at the end.

### Added — Images
- `Personaje.ImagenBytes` + `ImagenContentType` (nullable). Same on
  `Ubicacion`. Stored as `bytea` in Postgres.
- Minimal API endpoints under `/api`: GET (anonymous, for `<img>`
  rendering), POST (multipart, 2 MB max, whitelisted JPEG/PNG/WebP/
  GIF), DELETE. Cookie-authorized at the endpoint group.
- `InputFile` in the personaje and ubicacion edit modals with live
  data-URL preview and "Quitar" link. Personaje cards show a 56px
  circular avatar (gray silhouette when empty); ubicación cards a
  140px-tall banner image.

### Added — Tests, CI, docs
- 65 tests total (was 9 pre-modernization). New coverage for
  `UserLlmOptionsAccessor`, `CompendioService`, `BeatService`,
  `GenerarBorradoresPendientesAsync`, `RegenerarConObservacionesAsync`,
  and tenancy isolation on `ObraRepository`. SQLite in-memory
  scaffolding shared via `TestDb` + `FakeLlmClient` +
  `FakeLlmClientFactory` + `FakeLlmOptionsAccessor`.
- GitHub Actions CI: build + test on push / PR. CodeQL workflow with
  weekly schedule.
- Community files: CONTRIBUTING, CODE_OF_CONDUCT, SECURITY,
  CHANGELOG, FUNDING, issue + PR templates, .editorconfig + .gitattributes.

### Changed
- **BREAKING:** Renamed the project from `NovelaEngine` to
  `CenitStoryTeller` across all assemblies, namespaces, and the
  solution file. Entities namespace moved from the misplaced
  `NovelaEngine.Data.Entities` to `CenitStoryTeller.Core.Entities`.
- `IAcidTest.Nombre` (string) → `Dimension` (`AcidDimension` enum).
  Replaces the fragile `StartsWith("fís")` matching when fusing
  dimensional findings.
- Extracted `ModernizacionService` from `GeneracionService`. The
  modernization flow no longer drags chapter / acid-test dependencies.
- `Obra` gains soft delete (`EliminadaEn` + global query filter +
  `EliminarAsync` / `RestaurarAsync`).
- Schema migrations no longer apply on web startup. Pass `--migrate`
  to apply pending migrations + seed the demo, then exit. The
  Dockerfile chains the two so single-container flow is unchanged.
- README rewritten (EN + ES) with badges, problem statement, ASCII
  architecture, pre-1.0 status, contributing pointer.

### Fixed
- `ModernizacionService` pre-assigned `Id = Guid.NewGuid()` on new
  Personajes / Ubicaciones / Beats / Eventos added via navigation
  collections. EF Core's convention treated that as Modified and
  threw `DbUpdateConcurrencyException` on save. Now lets EF generate
  the Guids at SaveChanges so they're tracked as Added.

### Removed
- Dead code: `Core/Agents/MotorDeHistoria.cs` and
  `Core/Agents/ContinuidadAcido.cs` stubs (superseded by
  `GeneracionService` and `AcidTestRunner`).

### Added — Pre-publish polish
- ObraDetalle.razor split into focused per-tab components under
  `Components/Pages/ObraTabs/`: ObraTabPersonajes, ObraTabUbicaciones,
  ObraTabBeats, ObraTabCapitulos, ObraTabEventos. The parent page drops
  from ~800 lines to ~110 — header + breadcrumbs + 6 thin
  `<MudTabPanel><ObraTabX .../></MudTabPanel>`. Every Bootstrap modal
  is now a MudDialog; every card uses MudCard / MudAvatar / MudImage.
- Second public-domain demo: **El corazón delator** (Poe, 1843).
  Three personajes, two ubicaciones, nine beats, six events, seven
  capítulos. Different shape from East Lynne (short horror monologue
  vs long serialized melodrama) — proves the framework isn't tuned
  to one genre. Loaded automatically by `--migrate` alongside East
  Lynne, idempotent.
- `docs/architecture.md` — 9-section deep dive with Mermaid diagrams:
  layout, end-to-end flow, the four-dimension acid test, the
  compendio cache, LLM provider abstraction, multi-tenancy, data
  model, and a prompts customization guide.
- README "Preview" section with placeholder paths for screenshots
  the operator can drop into `docs/images/`.

### Known limitations
- API keys persisted in plain text in `ConfiguracionLlm.ApiKey`. Fine
  for single-tenant local installs; add at-rest encryption before
  running on shared infrastructure.
- The legacy "Añadir Beat" button (append at end) still uses the
  in-component direct DbContext path. "Insertar aquí" goes through
  `BeatService` and handles renumbering — both consistent because
  the append path lands past the end.
- No UI / integration tests yet — only service tests with a fake
  LLM and SQLite.
