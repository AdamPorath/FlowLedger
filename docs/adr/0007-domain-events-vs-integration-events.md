# ADR-0007 — Separação entre Domain Events e Integration Events

## Status

Accepted

## Context

Quando uma `Transaction` é criada, esse fato precisa, ao mesmo tempo, (a) ser expresso dentro do próprio domínio de `Transactions.Api` de forma independente de infraestrutura, e (b) ser comunicado a outros serviços (`Consolidation.Worker`) através de um contrato estável de mensageria. Modelar os dois com o mesmo tipo acoplaria o domínio interno ao formato de mensagem publicado externamente.

## Decision

Foram criados dois tipos distintos para o mesmo fato de negócio:

- **Domain Event**: `TransactionCreated` (record, em `Transactions.Api/Domain/Events`, implementa `IDomainEvent`), levantado dentro do próprio agregado `Transaction` ao ser criado (`Transaction.Create`), e acumulado em uma lista interna (`_domainEvents`) até ser processado.
- **Integration Event**: `TransactionCreatedIntegrationEvent` (record, em `FlowLedger.Contracts/IntegrationEvents/Transactions`, implementa `IIntegrationEvent`/herda `IntegrationEvent`), publicado no RabbitMQ para consumo por outros serviços.

A ponte entre os dois é feita por `IntegrationEventMapper.Map(IDomainEvent)`, chamado a partir de `DomainEventInterceptor` (um `SaveChangesInterceptor` do EF Core) no momento do `SaveChangesAsync` — ou seja, o evento de domínio só é traduzido e publicado como evento de integração se a transação de banco for de fato persistida.

## Rationale

- O Domain Event pertence ao domínio (`Transactions.Api.Domain`) e não pode depender de infraestrutura de mensageria (garantido em teste por `LayeringRuleTests.Domain_Should_Not_DependOn_InfrastructureConcerns`), preservando a possibilidade de testar o agregado `Transaction` sem qualquer dependência externa.
- O Integration Event pertence a um contrato compartilhado (`FlowLedger.Contracts`), podendo evoluir de forma independente do modelo de domínio interno de `Transactions.Api` — inclusive tendo seu próprio enum `TransactionType`, distinto do enum de domínio (`Domain.Enums.TransactionType`), com conversão explícita em `IntegrationEventMapper.MapTransactionType`.

## Alternatives Considered

- **Publicar o próprio Domain Event diretamente no barramento**: rejeitada, pois acoplaria o formato de mensagem externo (que outros serviços consomem) à representação interna do domínio, dificultando evolução independente de cada lado.
- **Gerar o Integration Event diretamente no handler de aplicação** (`CreateTransactionHandler`), sem passar pelo domínio: rejeitada, pois moveria a responsabilidade de "o que aconteceu" para fora do agregado, quebrando o encapsulamento de `Transaction`.

## Trade-offs

- Exige manter dois tipos e um mapeamento explícito entre eles para cada novo evento, em vez de reutilizar uma única classe.
- A tradução ocorre em um `SaveChangesInterceptor`, um ponto de extensão do EF Core que pode não ser óbvio para quem não conhece o código — mitigado por este documento e pelos nomes descritivos das classes (`DomainEventInterceptor`, `IntegrationEventMapper`).

## Consequences

- Novos eventos de domínio que precisem virar eventos de integração devem ser adicionados ao `switch` de `IntegrationEventMapper.Map`.
- `Transaction.ClearDomainEvents()` é chamado após a publicação, evitando republicação do mesmo evento em uma próxima chamada de `SaveChanges` sobre a mesma instância rastreada pelo EF Core.

## Scope / Limitations

Atualmente existe apenas um par Domain Event/Integration Event implementado (`TransactionCreated`/`TransactionCreatedIntegrationEvent`); não há outros fatos de domínio publicados como eventos de integração neste repositório.
