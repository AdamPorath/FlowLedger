# ADR-0003 — Comunicação assíncrona via RabbitMQ e MassTransit

## Status

Accepted

## Context

`Transactions.Api` (escrita) precisa notificar `Consolidation.Worker` (leitura consolidada) sobre novas transações, sem acoplar os dois serviços por uma chamada síncrona e sem risco de perda de eventos em caso de indisponibilidade momentânea de um dos lados.

## Decision

A comunicação entre os dois domínios é feita exclusivamente por eventos de integração publicados em **RabbitMQ**, usando **MassTransit** como biblioteca de abstração sobre o broker. `Transactions.Api` publica `TransactionCreatedIntegrationEvent` (definido em `FlowLedger.Contracts`) através de um `IPublishEndpoint`; `Consolidation.Worker` consome essa mesma mensagem através de um `IConsumer<TransactionCreatedIntegrationEvent>` (`TransactionCreatedConsumer`), assinando a fila `consolidation-transaction-created`.

## Rationale

- MassTransit fornece, prontas para uso, as abstrações de outbox transacional (ADR-0004) e inbox para deduplicação (ADR-0005), que seriam custosas de implementar manualmente de forma correta.
- RabbitMQ é um broker maduro, com garantias de entrega at-least-once bem conhecidas, adequado ao volume e ao caso de uso do desafio.
- O contrato de mensagem vive em uma biblioteca compartilhada (`FlowLedger.Contracts`), desacoplado dos modelos de domínio internos de cada serviço.

## Alternatives Considered

- **Chamada HTTP síncrona** de `Transactions.Api` para `Consolidation.Worker`/`Consolidation.Api`: rejeitada por acoplar disponibilidade e latência dos dois serviços, e por não oferecer garantias nativas de entrega/retentativa.
- **Broker gerenciado na nuvem (ex.: Azure Service Bus)**: não adotado — o `AppHost` modela RabbitMQ tanto localmente (`AddRabbitMQ`) quanto em produção (como connection string externa, ver ADR-0012), sem trocar de tecnologia de mensageria entre ambientes.

## Trade-offs

- Consistência eventual entre os dois domínios (ver ADR-0001), exigindo que o consumidor seja idempotente (ADR-0005).
- Introduz uma dependência de infraestrutura adicional (RabbitMQ) que precisa estar saudável para o pipeline de consolidação funcionar — mitigado por health checks (`BusHealthCheck` do MassTransit) e por retry exponencial no consumo (`UseMessageRetry(r => r.Exponential(retryLimit: 5, minInterval: 1s, maxInterval: 30s, intervalDelta: 5s))`).

## Consequences

- Qualquer novo evento de integração deve ser adicionado a `FlowLedger.Contracts` para ser compartilhado entre produtor e consumidor.
- A saúde do RabbitMQ é monitorada automaticamente via `BusHealthCheck`, sem configuração adicional, sempre que `AddMassTransit` está presente em um serviço.

## Scope / Limitations

Em produção, o RabbitMQ é tratado como uma **dependência externa/gerenciada**: não é provisionado pelo `AppHost` — é modelado apenas como uma connection string externa esperada (ver ADR-0012). Isso é uma decisão intencional e funcional no estado atual do código, não uma lacuna a corrigir; está fora do escopo desta decisão de comunicação assíncrona em si, que trata do uso de RabbitMQ/MassTransit como tecnologia, independente de quem o provisiona.
