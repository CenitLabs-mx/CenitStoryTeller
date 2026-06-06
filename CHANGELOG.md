# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Soft delete on `Obra` with global query filter and `EliminarAsync` /
  `RestaurarAsync` repository methods.
- `--migrate` CLI flag on the Web project that applies pending EF migrations
  and seeds the demo, then exits — so the webserver no longer touches the
  schema on startup.
- Test coverage for `AcidTestBase.Parse`, `AcidTestRunner`, `GeneracionService`,
  and `ModernizacionService` (21 new tests, 30 total). Includes a
  `FakeLlmClient`, `FakePromptProvider`, and SQLite in-memory test database
  scaffolding.
- GitHub Actions CI workflow: build + test on push and PR.
- Community health files: `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`,
  `SECURITY.md`, `CHANGELOG.md`.
- `.editorconfig` and `.gitattributes` for consistent formatting.

### Changed
- **BREAKING:** Renamed the project from `NovelaEngine` to `CenitStoryTeller`
  across all assemblies, namespaces, and the solution file. The entities
  namespace moved from the misplaced `NovelaEngine.Data.Entities` to
  `CenitStoryTeller.Core.Entities`.
- `IAcidTest.Nombre` (string) replaced with `Dimension` (`AcidDimension`
  enum). Eliminates the fragile `StartsWith("fís")` matching when fusing
  dimensional findings.
- Extracted `ModernizacionService` from `GeneracionService` — the
  modernization flow no longer drags chapter / acid-test dependencies.

### Fixed
- `ModernizacionService` pre-assigned `Id = Guid.NewGuid()` on new
  Personajes / Ubicaciones / Beats / Eventos added via navigation
  collections, which caused EF Core to track them as Modified instead of
  Added and throw `DbUpdateConcurrencyException` on save. Removed the
  pre-assignment so EF generates Guids client-side at SaveChanges.

### Removed
- Dead code: `Core/Agents/MotorDeHistoria.cs` and
  `Core/Agents/ContinuidadAcido.cs` stubs (superseded by `GeneracionService`
  and `AcidTestRunner`).
