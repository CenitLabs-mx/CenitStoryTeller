# CenitStoryTeller

[![ci](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/ci.yml/badge.svg)](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/ci.yml)
[![codeql](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/codeql.yml/badge.svg)](https://github.com/cenit-labs/CenitStoryTeller/actions/workflows/codeql.yml)
[![licencia: MIT](https://img.shields.io/badge/licencia-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)

> Framework agéntico para escribir novelas con un canon coherente y control
> de calidad automático. El usuario orquesta; los subagentes generan prosa
> y la validan dimensión por dimensión antes de darla por escrita.

**Estado:** pre-1.0, en desarrollo activo. El dataset de dominio público
funciona de punta a punta; las APIs aún pueden cambiar. Consulta
[`CHANGELOG.md`](CHANGELOG.md).

[English](README.md) · [Español](README_ES.md)

---

## Vista previa

<!-- Sustituye estos placeholders por screenshots reales — ver docs/images/README.md -->

| Workspace de obra | Ácido falló → regenerar |
|---|---|
| ![Workspace de obra](docs/images/01-workspace.png) | ![Ácido fallido](docs/images/02-acid-failed.png) |

| Configuración LLM por usuario | Reordenar beats |
|---|---|
| ![Configuración](docs/images/03-configuracion.png) | ![Reordenar beats](docs/images/04-beat-reorder.png) |

## Qué problema resuelve

Los LLM son mentirosos confiados en narrativa larga: los personajes se
salen de personaje, las ubicaciones se contradicen, los muertos aparecen
vivos, las relaciones románticas surgen sin siembra previa. CenitStoryTeller
trata al LLM como un componente, no como autor — genera prosa contra un
**canon persistido** y valida la salida en cuatro dimensiones antes de
promoverla a "escrita":

| Dimensión       | Qué verifica                                                                       |
|-----------------|------------------------------------------------------------------------------------|
| **Física**      | Cuerpos, heridas, edad, estado vital. Nadie actúa después de morir.                |
| **Psicológica** | Decisiones coherentes con la herida, deseo y necesidad de cada personaje.          |
| **Ambiental**   | Plausible para el lugar y la época; respeta el lore.                               |
| **Química**     | Relaciones ganadas y sembradas — no hay romance que aparezca de la nada.           |

Cada escena generada es un **artefacto versionado y auditable** — puedes
comparar borradores, ver qué modelo produjo qué versión, y revisar el
snapshot exacto del canon que vio el agente.

## Arquitectura

```
┌──────────────────────┐         ┌─────────────────────────────┐
│  Usuario (Blazor UI) │         │  Canon (Postgres + EF Core) │
│  - Definir obra      │◄────────┤  Personajes, Ubicaciones,   │
│  - Disparar drafts   │         │  Beats, Eventos, Capítulos  │
│  - Aprobar versiones │         └──────────────┬──────────────┘
└──────────┬───────────┘                        │
           │                                    │
           ▼                                    ▼
┌──────────────────────┐         ┌─────────────────────────────┐
│  Motor de Historia   │  draft  │  Continuidad + Prueba Ácido │
│  (modelo barato T=.9)├────────►│  (modelo fuerte T=.2)       │
│  Beats + canon       │         │  4 dimensiones, JSON        │
│  → prosa             │         └──────────────┬──────────────┘
└──────────────────────┘                        │
                                                ▼
                                  ┌──────────────────────────┐
                                  │  RegistroPaso (auditoría)│
                                  │  cada paso, cada agente  │
                                  └──────────────────────────┘
```

- **`Core`** — entidades de dominio, abstracción `ILlmClient`, `AcidTest`s.
- **`Data`** — `NovelaDbContext`, repositorios, `GeneracionService`,
  `ModernizacionService`, `AcidTestRunner`.
- **`Web`** — UI Blazor Server sobre .NET 8.

Tres proveedores LLM incluidos: **OpenAI**, **Gemini**, **Ollama** (para
correr 100% local con Llama/Gemma/Mistral).

Para profundizar — diagramas de secuencia de cada flujo, la rúbrica del
ácido, el caché del compendio, multi-tenancy, y cómo cambiar o traducir
los prompts — lee [`docs/architecture.md`](docs/architecture.md).

## Arranque rápido

```bash
git clone https://github.com/cenit-labs/CenitStoryTeller.git
cd CenitStoryTeller
cp .env.example .env        # rellena tu Llm__ApiKey
docker compose up --build
```

App en http://localhost:8080. Dos demos de dominio público se cargan
automáticamente para que puedas probar todos los flujos sin escribir
prosa tú mismo:

- **East Lynne** (Ellen Wood, 1861) — melodrama serializado largo
  modernizado a registro juvenil / Wattpad. Reparto amplio, muchos
  capítulos.
- **El corazón delator** (Edgar Allan Poe, 1843) — monólogo de horror
  breve. Tres personajes, dos ubicaciones, nueve beats. Forma muy
  distinta; muestra que el framework no está afinado a un solo género.

### Migraciones de base de datos

El servidor web **no** aplica migraciones al arrancar (para evitar
carreras entre múltiples instancias). Ejecútalas explícitamente:

```bash
dotnet run --project src/CenitStoryTeller.Web -- --migrate
```

El flag `--migrate` aplica las migraciones pendientes, siembra el demo
de East Lynne (idempotente) y sale. La imagen Docker encadena
`--migrate` antes del servidor. Para despliegues multi-instancia,
ejecuta `--migrate` como paso de init dedicado (init container en k8s,
job de CI, etc.).

### Dev local (sin Docker para la app)

```bash
docker compose up -d db                                          # solo Postgres
dotnet run --project src/CenitStoryTeller.Web -- --migrate       # schema + demo
dotnet run --project src/CenitStoryTeller.Web                    # http://localhost:5050
```

## Tests

```bash
dotnet test CenitStoryTeller.slnx
```

CI corre lo mismo en cada push y PR. Los tests usan SQLite en memoria
y un `FakeLlmClient` — no necesitan API key ni red.

## Contribuir

PRs bienvenidas. Ver [`CONTRIBUTING.md`](CONTRIBUTING.md) para la
estructura del proyecto, cómo correr todo localmente, y el checklist
de PR. Al participar aceptas el [Código de Conducta](CODE_OF_CONDUCT.md).

Problemas de seguridad: sigue [`SECURITY.md`](SECURITY.md) — **no**
abras issues públicos para vulnerabilidades.

## Roadmap

- Compuerta de aprobación manual entre borrador y prueba del ácido.
- UI de soft-delete (el repositorio ya lo soporta; falta la UI).
- Exportación a EPUB / DOCX / Markdown.
- Cuentas multi-usuario (hoy single-tenant local).
- Visualización de subtramas y timeline.

## Licencia

MIT — ver [`LICENSE`](LICENSE).
