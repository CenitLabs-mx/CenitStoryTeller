# CenitStoryTeller

Framework agéntico para escribir novelas con canon coherente y control de calidad
automático ("Prueba del Ácido"). El usuario orquesta; los subagentes generan y validan.

## Arquitectura
- **Motor de Historia** — convierte beats + canon en prosa, escena por escena.
- **Continuidad & Prueba del Ácido** — valida coherencia y plausibilidad (física, psicológica, ambiental, química) antes de dar por buena una escena.
- **Canon relacional** (EF Core): Personajes, Ubicaciones, Beats, Eventos, Capítulos.
- **Trazabilidad**: cada generación queda como versión comparable + registro auditable.
- **Proveedor LLM intercambiable**: OpenAI, Gemini o modelos locales (Ollama/Gemma).

## Arranque rápido

```bash
git clone https://github.com/<tu-usuario>/CenitStoryTeller.git
cd CenitStoryTeller
cp .env.example .env        # rellena tu LLM_API_KEY
docker compose up --build
```

Abre http://localhost:8080

### Migraciones de base de datos

El servidor web **no** aplica migraciones al arrancar (para evitar carreras
entre múltiples instancias). Ejecútalas explícitamente:

```bash
dotnet run --project src/CenitStoryTeller.Web -- --migrate
```

El flag `--migrate` aplica las migraciones pendientes, siembra el demo de East
Lynne (idempotente) y sale. La imagen Docker lo corre una vez antes de
levantar el servidor — en despliegues multi-instancia, ejecútalo como init
container dedicado.

## Demo incluida: East Lynne (dominio público)
En `examples/east-lynne/` está una novela del s. XIX modernizada con el flujo "moderniza": canon completo (personajes, beats, eventos) + 5 capítulos de muestra. Cárgala para ver el framework de punta a punta.

## Licencia
MIT — ver LICENSE.
