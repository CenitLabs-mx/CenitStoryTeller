# CenitStoryTeller

[![ci](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/ci.yml/badge.svg)](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/ci.yml)
[![codeql](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/codeql.yml/badge.svg)](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/codeql.yml)
[![license: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

> Agentic framework for writing novels with a coherent canon and automated
> quality control. The user orchestrates; subagents generate prose and
> validate it dimension by dimension before anything counts as written.

**Status:** pre-1.0, actively developed. Public-domain demo dataset works
end-to-end; APIs may still shift. Read [`CHANGELOG.md`](CHANGELOG.md).

[English](README.md) · [Español](README_ES.md)

---

## Preview

<!-- Replace these placeholders with real screenshots — see docs/images/README.md -->

| Workspace | Acid test failed → regenerate |
|---|---|
| ![Obra workspace](docs/images/01-workspace.png) | ![Acid failed](docs/images/02-acid-failed.png) |

| Per-user LLM config | Beat reorder |
|---|---|
| ![Configuracion](docs/images/03-configuracion.png) | ![Beat reorder](docs/images/04-beat-reorder.png) |

## What problem this solves

LLMs are confident liars over long-form narrative: characters speak out of
character, locations contradict themselves, dead people show up alive,
romance arcs appear with no setup. CenitStoryTeller treats the LLM as a
component, not the author — it generates prose against a **persisted canon**
and validates the output across four dimensions before promoting it to
"written":

| Dimension      | What it checks                                                                |
|----------------|-------------------------------------------------------------------------------|
| **Physical**   | Character bodies, injuries, age, vital state. No one acts after dying.        |
| **Psychological** | Decisions consistent with each character's wound, desire, need. No OOC turns. |
| **Environmental** | Plausible for this place and era; lore obeyed.                            |
| **Chemical**   | Relationships earned and seeded — no romance out of nowhere.                  |

Each generated scene is a **versioned, auditable artifact** — you can
compare drafts, see which model produced which version, and review the
exact canon snapshot the agent had in context.

## Architecture

```
┌──────────────────────┐         ┌─────────────────────────────┐
│  User (Blazor UI)    │         │  Canon (Postgres + EF Core) │
│  - Define obra       │◄────────┤  Personajes, Ubicaciones,   │
│  - Trigger drafts    │         │  Beats, Eventos, Capítulos  │
│  - Approve versions  │         └──────────────┬──────────────┘
└──────────┬───────────┘                        │
           │                                    │
           ▼                                    ▼
┌──────────────────────┐         ┌─────────────────────────────┐
│  Motor de Historia   │  draft  │  Continuidad + Prueba Ácido │
│  (cheap model, T=.9) ├────────►│  (strong model, T=.2)       │
│  Beats + canon       │         │  4 dimensions, JSON verdict │
│  → prose             │         └──────────────┬──────────────┘
└──────────────────────┘                        │
                                                ▼
                                  ┌──────────────────────────┐
                                  │  RegistroPaso (audit log)│
                                  │  every step, every agent │
                                  └──────────────────────────┘
```

- **`Core`** — domain entities, `ILlmClient` abstraction, `AcidTest`s.
- **`Data`** — `NovelaDbContext`, repositories, `GeneracionService`,
  `ModernizacionService`, `AcidTestRunner`.
- **`Web`** — Blazor Server UI on .NET 8.

Three LLM providers built in: **OpenAI**, **Gemini**, **Ollama** (for fully
local runs with Llama/Gemma/Mistral).

For a deeper walkthrough — sequence diagrams of every flow, the acid
test rubric, the compendio cache, multi-tenancy, and how to swap or
translate the prompts — read [`docs/architecture.md`](docs/architecture.md).

## Quick start

```bash
git clone https://github.com/cenit-labs/CenitStoryTeller.git
cd CenitStoryTeller
cp .env.example .env        # fill in Llm__ApiKey
docker compose up --build
```

App on http://localhost:8080. The included **East Lynne** demo
(Ellen Wood, 1861 — public domain) loads automatically: full canon
plus five sample chapters modernized to a YA / Wattpad register, so
you can see every workflow end-to-end without writing anything yourself.

### Database migrations

The webserver does **not** apply migrations on startup (to avoid races
between multiple instances). Run them explicitly:

```bash
dotnet run --project src/CenitStoryTeller.Web -- --migrate
```

The `--migrate` flag applies pending migrations, seeds the East Lynne demo
(idempotent), and exits. The Docker image chains `--migrate` before
starting the webserver. For multi-instance deployments, run `--migrate`
as a dedicated init step (k8s init container, CI job, etc.).

### Local dev (without Docker for the app)

```bash
docker compose up -d db                                          # Postgres only
dotnet run --project src/CenitStoryTeller.Web -- --migrate       # schema + demo
dotnet run --project src/CenitStoryTeller.Web                    # http://localhost:5050
```

## Tests

```bash
dotnet test CenitStoryTeller.slnx
```

CI runs the same on every push and PR. Tests use SQLite in-memory and a
`FakeLlmClient` — no API key or network needed.

## Contributing

PRs welcome. See [`CONTRIBUTING.md`](CONTRIBUTING.md) for project layout,
how to run things locally, and the PR checklist. By participating you
agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

Security issues: please follow [`SECURITY.md`](SECURITY.md) — do **not**
open public GitHub issues for vulnerabilities.

## Roadmap

- Manual approval gate between chapter draft and acid test.
- Soft-delete UI (the repository layer supports it; no UI yet).
- Export to EPUB / DOCX / Markdown.
- Multi-user accounts (currently single-tenant local).
- Subplot and timeline visualization.

## License

MIT — see [`LICENSE`](LICENSE).
