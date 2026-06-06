# CenitStoryTeller

Agentic framework for writing novels with a coherent canon and automated quality control ("Acid Test"). The user orchestrates; subagents generate and validate.

## Architecture
- **Story Engine** — converts beats + canon into prose, scene by scene.
- **Continuity & Acid Test** — validates coherence and plausibility (physical, psychological, environmental, chemical) before approving a scene.
- **Relational Canon** (EF Core): Characters, Locations, Beats, Events, Chapters.
- **Traceability**: every generation remains as a comparable version + auditable log.
- **Interchangeable LLM Provider**: OpenAI, Gemini, or local models (Ollama/Gemma).

## Quick Start

```bash
git clone https://github.com/<your-username>/CenitStoryTeller.git
cd CenitStoryTeller
cp .env.example .env        # fill in your LLM_API_KEY
docker compose up --build
```

Open http://localhost:8080

### Database migrations

The webserver does **not** apply migrations on startup (to avoid races between
multiple instances). Run them explicitly:

```bash
dotnet run --project src/CenitStoryTeller.Web -- --migrate
```

The `--migrate` flag applies pending migrations, seeds the East Lynne demo
(idempotent), and exits. The Docker image runs this once before starting the
webserver — for multi-instance deployments, run it as a dedicated init step.

## Included Demo: East Lynne (Public Domain)
In `examples/east-lynne/` is a 19th-century novel modernized with the "modernize" workflow: full canon (characters, beats, events) + 5 sample chapters. Load it to see the framework end-to-end.

## License
MIT — see LICENSE.
