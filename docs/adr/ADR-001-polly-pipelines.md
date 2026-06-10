# ADR-001 — Polly v8 resilience pipelines

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Chamadas a dependências falham (timeouts, 5xx, lentidão). Tratar isso com retry caseiro é
frágil e não compõe.

## Decisão
Usar **Polly v8** (`ResiliencePipeline`): estratégias componíveis — bulkhead, retry, circuit
breaker, timeout, fallback (+ rate limiter inbound) — montadas num pipeline por dependência.

## Consequências
- ✅ Cada falha tem uma estratégia explícita e testável.
- ✅ Ordem importa e é clara (de fora para dentro: bulkhead → retry → CB → timeout → chaos → chamada).
- ✅ Telemetria do Polly integra com OTel.
- ✅ **Hedging é deliberadamente NÃO usado**: ele dispara chamadas paralelas e usa a primeira
  resposta — ótimo para leituras idempotentes, perigoso num `POST /payments` (cobrança dupla).
  Saber quando *não* aplicar uma estratégia é parte do design.
- ⚠️ Configurar mal (ex: retry dentro do CB) tem efeitos sutis — documentado por estratégia.
