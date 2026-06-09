# ADR-003 — Fallback para degradação graciosa

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Quando o downstream cai, o serviço pode falhar (5xx) ou **degradar** (resposta parcial/cacheada).

## Decisão
Um **fallback** envolve o pipeline: se o Payments está indisponível (circuito aberto/timeout), o
Orders responde com um resultado degradado (ex: pedido aceito como "pending payment") em vez de 5xx.

## Consequências
- ✅ Disponibilidade preservada — o cliente recebe algo útil.
- ✅ O SLO de disponibilidade aguenta o caos (o fallback conta como sucesso degradado).
- ⚠️ Degradação precisa ser semanticamente segura (idempotência, reconciliação posterior).
