# Security Policy

## Supported versions

CenitStoryTeller is pre-1.0. Only the `main` branch receives security fixes.

| Version | Supported          |
| ------- | ------------------ |
| main    | :white_check_mark: |
| < 0.1   | :x:                |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, email **security@cenit-labs.com** with:

- A description of the issue and its potential impact.
- Steps to reproduce, ideally with a minimal proof of concept.
- The affected version / commit SHA.
- Your name and affiliation (if you'd like to be credited).

You can expect:

- An acknowledgement within **72 hours**.
- A more detailed response within **7 days**, indicating next steps.
- A coordinated disclosure timeline, typically **30 to 90 days**.

We follow [coordinated disclosure](https://en.wikipedia.org/wiki/Coordinated_disclosure):
once a fix is ready, we publish a security advisory crediting the reporter
(unless they prefer to remain anonymous).

## Scope

In scope:

- The CenitStoryTeller application (Core, Data, Web projects).
- The Docker image we publish.
- The included prompts and example dataset.

Out of scope:

- Vulnerabilities in third-party LLM providers (OpenAI, Gemini, Ollama).
- Issues that require physical access to a machine running the app.
- DoS via expensive LLM queries (rate limiting is the operator's responsibility).
- Self-XSS, attacks requiring social engineering of an authenticated admin.

## What we treat as sensitive

- LLM API keys (`Llm__ApiKey`).
- Database connection strings.
- User-generated canon and prose (in production deployments).
- Audit log entries (`RegistroPaso.CanonSnapshot`).
