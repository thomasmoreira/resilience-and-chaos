# resilience-and-chaos

Um serviço (Orders) que chama um downstream instável (Payments) através de um pipeline de resiliência com Polly v8 (bulkhead, retry, circuit breaker, timeout, fallback), com injeção de caos controlável em runtime via Simmy. Um teste automatizado mede que, mesmo com o downstream totalmente fora, a disponibilidade do serviço se mantém pelo fallback. Orquestrado com .NET Aspire.

[![CI](https://github.com/thomasmoreira/resilience-and-chaos/actions/workflows/ci.yml/badge.svg)](https://github.com/thomasmoreira/resilience-and-chaos/actions/workflows/ci.yml)

## Visão geral

```mermaid
flowchart LR
  Client([Client]) -->|POST /orders| ORD[Orders]
  ORD -->|Polly pipeline + Simmy| PAY[Payments]
  ORD -->|fallback| FB[(degradação graciosa)]
  ORD -. OTLP .-> DASH[[Aspire Dashboard]]
```

O Orders chama o Payments por um pipeline Polly. O Simmy injeta caos (falha ou latência) em runtime. Quando o Payments cai, o circuito abre e o fallback assume, respondendo de forma degradada em vez de retornar erro. Um experimento automatizado mede que a disponibilidade aguenta esse cenário.

| Componente | Papel |
|---|---|
| **Orders** | Pipeline Polly v8 (bulkhead, retry, circuit breaker, timeout, fallback) e rate limiter na entrada |
| **Payments** | Downstream simples, alvo do caos |
| **Simmy** | Injeção de latência e falha por configuração, ligável e desligável em runtime |
| **Experimento** | Teste que mede a disponibilidade sob outage (a prova automatizada do SLO) |
| **OpenTelemetry** | Traces e métricas no dashboard do Aspire (o stack completo de observabilidade é o lab [observability-from-scratch](https://github.com/thomasmoreira/observability-from-scratch)) |

## O pipeline

```
rate limiter (inbound)  →  /orders  →  [ bulkhead → retry → circuit breaker → timeout → chaos ]  →  Payments
                                                                                                  fallback ⤴ (degrada)
```

Cada estratégia tem um papel: o bulkhead isola concorrência; o retry absorve falhas transitórias; o circuit breaker abre rápido sob falha sustentada (fail-fast); o timeout corta tentativas lentas; o chaos (Simmy) é injetado logo antes da chamada real; e o fallback transforma a falha em degradação graciosa.

Hedging não é usado aqui de propósito: ele dispara chamadas paralelas e usa a primeira resposta, o que é útil para leituras idempotentes mas perigoso num `POST /payments` (risco de cobrança dupla).

## Endpoints

| Endpoint | O quê |
|---|---|
| `POST /orders` | Cria um pedido, chamando o Payments pelo pipeline resiliente |
| `POST /chaos` | Liga e desliga falha e latência em runtime: `{ "fault": true, "injectionRate": 1.0 }` |

## Como rodar e injetar caos

Pré-requisitos: .NET 10 e Docker.

```bash
dotnet new install Aspire.ProjectTemplates   # apenas na primeira vez
dotnet run --project src/AppHost             # Orders + Payments + dashboard

# 1) baseline: o pedido confirma
curl -X POST http://localhost:<porta>/orders -H 'Content-Type: application/json' -d '{"amount":42}'
#   → {"status":"confirmed", ...}

# 2) injeta um outage total do Payments
curl -X POST http://localhost:<porta>/chaos  -H 'Content-Type: application/json' -d '{"fault":true,"injectionRate":1.0}'

# 3) o pedido continua respondendo, agora degradado (o circuito abre, o fallback assume)
curl -X POST http://localhost:<porta>/orders -H 'Content-Type: application/json' -d '{"amount":42}'
#   → {"status":"pending_payment", "payment":null}

# 4) desliga o caos e o pedido volta a confirmar
curl -X POST http://localhost:<porta>/chaos  -H 'Content-Type: application/json' -d '{"fault":false}'
```

## Testes

```bash
dotnet test
```

- Baseline e toggle de caos: confirma, degrada sob caos e recupera ao desligar.
- Experimento: sob outage total do Payments, a disponibilidade do `/orders` se mantém em 99% ou mais, segurada pelo fallback. É um experimento com critério de sucesso (ADR-005), não uma demonstração visual. Na última execução, a disponibilidade ficou em 100%, com todas as respostas degradadas.

## Observabilidade

O OpenTelemetry (via ServiceDefaults) exporta para o dashboard do Aspire, e para qualquer backend OTLP se `OTEL_EXPORTER_OTLP_ENDPOINT` estiver configurado. O stack completo de Grafana, SLO e burn-rate é o lab [observability-from-scratch](https://github.com/thomasmoreira/observability-from-scratch); aqui a prova do SLO é o experimento automatizado, em vez de duplicar aquela stack.

## Decisões de arquitetura

- [ADR-001 — Polly v8 resilience pipelines](docs/adr/ADR-001-polly-pipelines.md)
- [ADR-002 — Simmy (chaos as code)](docs/adr/ADR-002-simmy-chaos.md)
- [ADR-003 — Fallback para degradação graciosa](docs/adr/ADR-003-graceful-degradation.md)
- [ADR-004 — Bulkhead e rate limiter](docs/adr/ADR-004-bulkhead-ratelimiter.md)
- [ADR-005 — Resiliência medida (experimento como gate)](docs/adr/ADR-005-slo-gated.md)
- [ADR-006 — Observabilidade: OTel e dashboard do Aspire](docs/adr/ADR-006-grafana-lgtm.md)
