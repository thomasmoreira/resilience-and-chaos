# ADR-005 — SLO-gated (burn-rate do lab 2)

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
Resiliência sem medida é fé. É preciso provar que, sob caos, o sistema **continua dentro do SLO**.

## Decisão
Reusar o **SLO + alerta de burn-rate** do `observability-from-scratch`: a disponibilidade do
Orders é o SLI; o experimento de caos não pode fazer o **burn-rate** estourar o limiar.

## Consequências
- ✅ Resiliência **medida**: o caos é um experimento com critério de sucesso (o budget aguenta).
- ✅ Liga os dois labs — observabilidade vira o oráculo da resiliência.
- ⚠️ O fallback precisa manter o SLI alto; senão o experimento "falha" (e isso é informação).
