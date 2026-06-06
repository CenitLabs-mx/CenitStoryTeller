# Contributing to CenitStoryTeller

Thanks for your interest. CenitStoryTeller is an open-source agentic
framework for novel writing — pull requests, bug reports, and discussion
are all welcome.

## Quick start

Prerequisites:
- .NET 8 SDK
- Docker (for Postgres)
- An LLM API key (OpenAI, Gemini) — or Ollama running locally

```bash
git clone https://github.com/cenit-labs/CenitStoryTeller.git
cd CenitStoryTeller
cp .env.example .env       # fill in Llm__ApiKey
docker compose up -d db    # start Postgres only
dotnet run --project src/CenitStoryTeller.Web -- --migrate   # apply schema + seed demo
dotnet run --project src/CenitStoryTeller.Web                # serve on http://localhost:5050
```

Or fully containerized: `docker compose up --build` (app on `:8080`).

## Project layout

```
src/
  CenitStoryTeller.Core/   # Domain entities, LLM client abstraction, AcidTests
  CenitStoryTeller.Data/   # EF Core DbContext, repositories, services
  CenitStoryTeller.Web/    # Blazor Server UI + Program.cs
tests/
  CenitStoryTeller.Tests/  # xUnit + SQLite in-memory
prompts/                   # Versioned system prompts (motor-de-historia, continuidad-acido, moderniza)
examples/east-lynne/       # Public-domain demo dataset
```

## Running tests

```bash
dotnet test CenitStoryTeller.slnx
```

CI runs the same on every push and PR.

## Pull request checklist

- [ ] `dotnet build` and `dotnet test` are green locally.
- [ ] New behavior has tests (use `FakeLlmClient` + `TestDb` from
      `tests/CenitStoryTeller.Tests/Fakes/`).
- [ ] Public API changes are documented in `CHANGELOG.md`.
- [ ] If you changed the DB schema, you ran
      `dotnet ef migrations add <Name> --project src/CenitStoryTeller.Data --startup-project src/CenitStoryTeller.Web`
      and committed the generated migration.
- [ ] Comments are kept minimal — names should carry intent.
- [ ] Commit messages are imperative and explain *why*, not just *what*.

## Reporting bugs

Use the GitHub issue tracker. Include:
- What you expected vs. what happened.
- Minimal reproduction steps.
- Logs (sanitize API keys first).
- LLM provider + model in use.

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).
Be kind, be specific, assume good faith.

## License

By contributing, you agree your contributions are licensed under
the MIT License (see [LICENSE](LICENSE)).
