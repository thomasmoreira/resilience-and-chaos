# ADR-001 — Polly v8 resilience pipelines

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Chamadas a dependências falham (timeouts, 5xx, lentidão). Tratar isso com retry caseiro é
frágil e não compõe.

## Decisão
Usar **Polly v8** (`ResiliencePipeline`): estratégias componíveis — retry, timeout, circuit
breaker, hedging, bulkhead, rate limiter, fallback — montadas num pipeline por dependência.

## Consequências
- ✅ Cada falha tem uma estratégia explícita e testável.
- ✅ Ordem importa e é clara (de fora para dentro: rate limiter → CB → retry → timeout → chamada).
- ✅ Telemetria do Polly integra com OTel.
- ⚠️ Configurar mal (ex: retry dentro do CB) tem efeitos sutis — documentado por estratégia.
