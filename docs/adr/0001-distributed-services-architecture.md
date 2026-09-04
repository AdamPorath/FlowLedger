# ADR-0001 — Arquitetura distribuída em múltiplos serviços

## Status

Accepted

## Context

O sistema precisa registrar transações financeiras e produzir um saldo consolidado por comerciante/dia/moeda. Essas duas responsabilidades têm perfis de carga, consistência e evolução diferentes: a escrita de transações exige baixa latência e forte validação de domínio no momento da requisição; a consolidação é, por natureza, um processo que pode (e deve) ser assíncrono e tolerante a atraso.

## Decision

A solução foi dividida em serviços distintos e independentemente implantáveis: `Gateway`, `Identity.Api`, `Transactions.Api`, `Consolidation.Worker` e `Consolidation.Api`, além de uma biblioteca de contratos compartilhados (`FlowLedger.Contracts`) e uma biblioteca de defaults de observabilidade/resiliência (`FlowLedger.ServiceDefaults`). `Transactions.Api` (escrita) e o par `Consolidation.Worker`/`Consolidation.Api` (leitura consolidada) só se comunicam via eventos de integração assíncronos, nunca por chamada HTTP direta entre si.

## Rationale

- Isolar o caminho crítico de escrita (criação de transação) de qualquer dependência síncrona do processo de consolidação.
- Permitir que o consumidor de consolidação escale e opere de forma independente (inclusive com número fixo de réplicas, ver `Consolidation.Worker`) sem afetar a API de escrita.
- Tornar o contrato entre os dois lados explícito e versionável através de `FlowLedger.Contracts`, ao invés de acoplamento por chamada de API.

## Alternatives Considered

- **Monólito modular único**: mais simples de operar, mas acopla o caminho de escrita ao de consolidação e dificulta escalar os dois lados de forma independente.
- **Consolidação síncrona via chamada HTTP** a partir de `Transactions.Api`: adicionaria latência e uma dependência síncrona direta entre os dois domínios, contrariando o objetivo de consistência eventual assíncrona.

## Trade-offs

- Maior complexidade operacional (mais processos, mais pontos de falha, necessidade de mensageria confiável).
- Consistência eventual entre escrita e leitura consolidada, exigindo que o cliente tolere um pequeno atraso até o saldo refletir uma transação recém-criada.
- Testes de ponta a ponta (E2E) tornam-se necessários para validar a interação entre os serviços, além dos testes unitários/integração por serviço.

## Consequences

- `FlowLedger.ArchitectureTests` (`LayeringRuleTests.cs`) impõe, em tempo de teste, que cada serviço não dependa dos namespaces internos dos demais (com `Consolidation.Api`/`Consolidation.Worker` tratados como um único bounded context, dado o compartilhamento de `DbContext` — ver ADR-0008).
- O `AppHost` do .NET Aspire modela a topologia completa, tanto para execução local (`aspire run`) quanto para publicação em Azure Container Apps (`aspire deploy`).

## Scope / Limitations

Esta decisão não implica que todo o sistema seja "orientado a eventos" de forma homogênea: `Consolidation.Api` é puramente síncrona/de leitura e não participa da mensageria (ver ADR-0003).
