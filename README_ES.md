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

## Demo incluida: East Lynne (dominio público)
En `examples/east-lynne/` está una novela del s. XIX modernizada con el flujo "moderniza": canon completo (personajes, beats, eventos) + 5 capítulos de muestra. Cárgala para ver el framework de punta a punta.

## Licencia
MIT — ver LICENSE.
