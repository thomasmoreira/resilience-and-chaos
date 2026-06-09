# ADR-002 — Simmy (chaos as code)

**Status:** Aceito · **Data:** 2026-06-09

## Contexto
"Funciona quando tudo está bom" não prova resiliência. É preciso **provocar falha** de forma
controlada e reproduzível.

## Decisão
Usar as **chaos strategies do Polly v8** (Simmy): injeção de **latência, fault e outcome** no
pipeline, por configuração, **toglável em runtime** (sem redeploy).

## Consequências
- ✅ Caos reproduzível e versionado — experimentos, não acidentes.
- ✅ Liga/desliga ao vivo (EnabledGenerator + InjectionRate) durante um teste de carga.
- ✅ O caos vive no mesmo pipeline da resiliência — testa exatamente o que protege.
- ⚠️ Caos só em ambientes não-produtivos; gated por config.
