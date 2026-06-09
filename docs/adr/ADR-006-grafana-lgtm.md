# ADR-006 — Observabilidade = Grafana LGTM

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
O killer detail ("o SLO aguenta o caos") precisa ser **visto**, não só afirmado.

## Decisão
Reusar a stack **Grafana LGTM** (Tempo/Loki/Prometheus/Grafana) do lab de observabilidade:
métricas de estado do circuito e de fallback, traces dos retries, e o painel de SLO + burn-rate
durante o experimento de caos.

## Consequências
- ✅ O experimento de caos é observável e demonstrável (screenshot/GIF do dashboard).
- ✅ Coerência com o portfólio (mesma stack de observabilidade).
- ⚠️ Mais containers; orquestrados pelo Aspire (ou compose) só para o lab.
