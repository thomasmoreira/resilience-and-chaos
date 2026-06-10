# ADR-005 — Resiliência medida (experimento como gate)

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Resiliência sem medida é fé. É preciso provar, com um critério objetivo, que sob caos o sistema
**continua disponível**.

## Decisão
Tratar o caos como um **experimento com critério de sucesso**, automatizado como teste
(`ExperimentTests`): sob outage total do Payments, a **disponibilidade do `/orders` deve ficar
≥ 99%** (o fallback segura). A disponibilidade é o SLI; o limiar (99%) é o gate.

> O **stack completo de SLO + alerta de burn-rate** (Prometheus/Grafana) é o lab
> `observability-from-scratch`. Aqui ele **não é duplicado**: o gate é verificado pelo experimento
> automatizado, determinístico e que roda em qualquer máquina.

## Consequências
- ✅ Resiliência **medida** e reproduzível — o caos é um experimento, não uma demo.
- ✅ Roda em `dotnet test` (local), sem depender de um stack de observabilidade externo.
- ✅ O fallback precisa manter o SLI alto; senão o experimento falha (e isso é informação).
- ⚠️ É um gate de disponibilidade pontual, não monitoramento contínuo — esse é o papel do lab de
  observabilidade (burn-rate multi-janela).
