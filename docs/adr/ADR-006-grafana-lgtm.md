# ADR-006 — Observabilidade: OTel + dashboard do Aspire

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
O comportamento sob caos (retries, circuito abrindo, fallback) precisa ser observável. A questão
é *quanto* de observabilidade montar aqui sem duplicar o lab que já existe para isso.

## Decisão
Instrumentar com **OpenTelemetry** (via ServiceDefaults) e exportar para o **dashboard do Aspire**
— e para qualquer backend OTLP via `OTEL_EXPORTER_OTLP_ENDPOINT`. O **stack LGTM completo**
(Tempo/Loki/Prometheus/Grafana + SLO + burn-rate) **não é replicado aqui**: ele é o lab
[`observability-from-scratch`](https://github.com/thomasmoreira/observability-from-scratch). A
prova do SLO neste lab é o **experimento automatizado** (ADR-005).

## Consequências
- ✅ Traces (retries, chamada ao Payments) e métricas visíveis no dashboard do Aspire, sem stack extra.
- ✅ Sem duplicação — cada lab tem um foco; quem quer o burn-rate em Grafana vai ao lab de observabilidade.
- ✅ Apontar para um backend OTLP real (Tempo/Prometheus) é só uma variável de ambiente.
- ⚠️ Não há, *neste repo*, um painel de SLO/burn-rate pronto — é uma escolha de escopo, não um esquecimento.
