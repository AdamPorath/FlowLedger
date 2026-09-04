# ADR-0004 — EF Core Transactional Outbox para publicação de eventos

## Status

Accepted

## Context

Quando uma transação financeira é persistida e, em seguida, um evento de integração precisa ser publicado no RabbitMQ, existe uma janela clássica de inconsistência: se o commit no banco for bem-sucedido mas a publicação no broker falhar (ou vice-versa), o evento pode ser perdido ou publicado sem a gravação correspondente.

## Decision

`Transactions.Api` usa o **Transactional Outbox do MassTransit com EF Core** (`AddEntityFrameworkOutbox<TransactionsDbContext>(o => { o.UsePostgres(); o.UseBusOutbox(); })`). Um `DomainEventInterceptor` (`SaveChangesInterceptor`), acionado dentro de `SavingChangesAsync`, identifica entidades `Transaction` com eventos de domínio pendentes, mapeia cada `TransactionCreated` para um `TransactionCreatedIntegrationEvent` via `IntegrationEventMapper`, e publica via `IPublishEndpoint` — publicação que, graças a `UseBusOutbox()`, é gravada como parte da mesma transação de banco, e só entregue ao broker após o commit.

## Rationale

- Garante atomicidade entre "a transação foi persistida" e "o evento será entregue eventualmente" — sem essa garantia, uma falha entre os dois passos poderia deixar o saldo consolidado permanentemente desatualizado ou publicar eventos "fantasma" de gravações que não ocorreram.
- Delega a complexidade de relay do outbox (processo em background que efetivamente envia as mensagens gravadas na tabela de outbox) à biblioteca MassTransit, já testada para esse propósito.

## Alternatives Considered

- **Publicar diretamente no broker dentro do mesmo `SaveChangesAsync`**, sem outbox: mais simples, mas reintroduz a janela de inconsistência descrita no contexto (commit de banco bem-sucedido + falha de publicação, ou o inverso).
- **Change Data Capture (CDC)** a partir do WAL do Postgres: alternativa válida para outbox, não adotada — exigiria infraestrutura adicional de captura de mudanças fora do que o MassTransit já oferece nativamente.

## Trade-offs

- Introduz tabelas adicionais (`OutboxMessage`, `OutboxState`) geridas pelo MassTransit no banco `flowledger`, e uma migração dedicada para criá-las.
- O evento só é entregue ao broker depois do commit — ou seja, existe uma pequena latência adicional entre a criação da transação e a publicação efetiva, inerente ao próprio propósito do outbox.

## Consequences

- O mapeamento `IDomainEvent` → `IIntegrationEvent` é centralizado em `IntegrationEventMapper`, ponto único a atualizar quando um novo tipo de evento de domínio precisar virar evento de integração.
- `DomainEventInterceptor` limpa (`ClearDomainEvents()`) os eventos da entidade após publicá-los, prevenindo republicação em um próximo `SaveChanges` da mesma instância rastreada.

## Scope / Limitations

Esta ADR cobre apenas o lado produtor (`Transactions.Api`). O uso do outbox/inbox no lado consumidor (`Consolidation.Worker`), com propósito de deduplicação, é tratado separadamente na ADR-0005.
