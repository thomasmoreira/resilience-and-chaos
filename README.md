# resilience-and-chaos

> **Polly v8** (circuit breaker, hedging, bulkhead, rate limiter, fallback) + **Simmy** (injeção de
> caos em runtime) + **degradação graciosa**, com o **SLO/burn-rate** (reusado do
> [observability-from-scratch](https://github.com/thomasmoreira/observability-from-scratch)) provando
> que o sistema **sobrevive** — tudo orquestrado pelo **.NET Aspire**.

[![CI](https://github.com/thomasmoreira/resilience-and-chaos/actions/workflows/ci.yml/badge.svg)](https://github.com/thomasmoreira/resilience-and-chaos/actions/workflows/ci.yml)

---

## A tese

Os outros labs provam que você **constrói** sistemas distribuídos. Este prova que você os
**desenha para falhar bem** — a marca de um arquiteto.

> Funcionar no caminho feliz é fácil. **Sobreviver ao caos é engenharia.**

## Arquitetura

```mermaid
flowchart LR
  Client([Client / k6]) -->|POST /orders| ORD[Orders]
  ORD -->|Polly pipeline + Simmy| PAY[Payments]
  ORD -->|fallback| FB[(degradação graciosa)]
  ORD -. OTLP .-> LGTM[(Grafana: SLO + burn-rate)]
```

O **Orders** chama o **Payments** através de um **pipeline Polly**; o **Simmy** injeta caos em
runtime; quando o Payments cai, o **circuito abre** e o **fallback** assume — e o **SLO** mostra
que o error budget aguenta.

## Componentes

| Peça | Papel |
|---|---|
| **Orders** | Pipeline Polly v8 (retry, timeout, circuit breaker, hedging, bulkhead, rate limiter, fallback) |
| **Payments** | Downstream simples — alvo do caos |
| **Simmy** | Injeção de latência/fault/outage por config, **togláveis ao vivo** (chaos as code) |
| **LGTM + SLO** | Burn-rate mostra que o error budget aguenta o caos (reuso do lab de observabilidade) |
| **k6** | Carga durante os experimentos |

## O killer detail

Sob carga, com o dashboard de SLO aberto: **ativo o caos → o circuito abre → o fallback responde
→ a disponibilidade SLI se mantém e o burn-rate fica abaixo do limiar.** Resiliência **medida**.

## Sinais de arquiteto

- **Cada estratégia Polly com propósito** — retry ≠ circuit breaker ≠ bulkhead ≠ rate limiter.
- **Chaos as code** — caos reproduzível e toglável, não falha manual.
- **Degradação graciosa** — falhar bem (fallback) em vez de falhar feio.
- **SLO-gated** — o caos não pode estourar o error budget (reuso do burn-rate do lab 2).

## Como rodar

**Pré-requisitos:** .NET 10 SDK e Docker.

```bash
dotnet new install Aspire.ProjectTemplates   # uma vez
dotnet run --project src/AppHost             # Orders + Payments + observabilidade + dashboard
```

## Decisões de arquitetura

- [ADR-001 — Polly v8 resilience pipelines](docs/adr/ADR-001-polly-pipelines.md)
- [ADR-002 — Simmy (chaos as code)](docs/adr/ADR-002-simmy-chaos.md)
- [ADR-003 — Fallback para degradação graciosa](docs/adr/ADR-003-graceful-degradation.md)
- [ADR-004 — Bulkhead + rate limiter](docs/adr/ADR-004-bulkhead-ratelimiter.md)
- [ADR-005 — SLO-gated (burn-rate do lab 2)](docs/adr/ADR-005-slo-gated.md)
- [ADR-006 — Observabilidade = Grafana LGTM](docs/adr/ADR-006-grafana-lgtm.md)

---

_Lab de portfólio. Foco: Polly v8, Simmy chaos engineering, degradação graciosa, SLO sob caos e .NET Aspire._
