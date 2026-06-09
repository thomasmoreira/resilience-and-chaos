# ADR-004 — Bulkhead + rate limiter

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Uma dependência lenta pode esgotar threads/conexões e derrubar o serviço inteiro (falha em
cascata). Picos de tráfego podem saturar recursos.

## Decisão
- **Bulkhead** (concurrency limiter): limita chamadas simultâneas ao Payments — isolamento de recursos.
- **Rate limiter**: aplica backpressure na entrada — protege o serviço de picos.

## Consequências
- ✅ Uma dependência lenta não afoga o serviço todo (falha isolada, não em cascata).
- ✅ Backpressure explícito em vez de colapso silencioso.
- ⚠️ Limites mal calibrados rejeitam tráfego legítimo; ajustados via carga (k6) + SLO.
