# ADR-0005 — Processamento at-least-once com idempotência via Inbox Pattern

## Status

Accepted

## Context

RabbitMQ, combinado com a semântica padrão do MassTransit, entrega mensagens com garantia **at-least-once**: falhas de rede, timeouts de acknowledgment ou reinícios do consumidor podem levar à reentrega da mesma mensagem. Sem tratamento explícito, isso somaria o valor de uma mesma transação mais de uma vez no saldo consolidado.

## Decision

`Consolidation.Worker` habilita o **Inbox Pattern do MassTransit** no *receive endpoint* da fila `consolidation-transaction-created`, via `e.UseEntityFrameworkOutbox<ConsolidationDbContext>(context)`. Isso cria e mantém uma tabela `InboxState`, com chave única por `MessageId` + `ConsumerId`: quando uma mensagem com o mesmo `MessageId` chega novamente, o MassTransit reconhece a duplicata e não executa `TransactionCreatedConsumer.Consume` novamente. O `MessageId` é definido no produtor como o `EventId` do próprio evento de integração (`context.MessageId = integrationEvent.EventId`), garantindo um identificador estável por evento de domínio.

## Rationale

- Delegar a deduplicação a uma tabela transacional gerida pelo MassTransit evita implementar manualmente uma lógica de "já processei este evento?" no consumidor, reduzindo risco de condições de corrida.
- Como o `InboxState` é atualizado na mesma transação de banco que o `ConsolidatedBalance`, a marcação "processado" e a atualização de saldo são atômicas — não existe uma janela em que a mensagem é marcada como processada mas o saldo não foi atualizado (ou vice-versa).

## Alternatives Considered

- **Consumo idempotente por design** (ex.: upsert baseado apenas nos dados de negócio, sem tabela de deduplicação): não se aplica aqui, pois a operação de negócio (somar crédito/débito) não é naturalmente idempotente — reprocessar a mesma mensagem sempre soma o valor novamente, exigindo um mecanismo de deduplicação por identidade da mensagem.
- **Deduplicação manual em uma tabela própria**: teria efeito equivalente ao Inbox do MassTransit, mas duplicaria uma solução já oferecida pela biblioteca em uso.

## Trade-offs

- Acopla a garantia de idempotência à infraestrutura de tabelas do MassTransit (`InboxState`, `OutboxState`, `OutboxMessage`) no banco `consolidation`.
- A deduplicação depende inteiramente da unicidade do `MessageId` gerado pelo produtor — se o produtor gerar um novo `MessageId` para o que deveria ser o mesmo evento de negócio, a deduplicação não ocorre.

## Consequences

- Validado por teste de integração real: `Publish_SameEventTwice_InboxDeduplicatesAndBalanceIsNotDoubled` (`tests/FlowLedger.Consolidation.IntegrationTests/TransactionCreatedFlowTests.cs`), que publica a mesma mensagem duas vezes contra um Postgres real (Testcontainers) e confirma que o saldo não dobra.
- O retry exponencial configurado no *receive endpoint* (`UseMessageRetry`) trabalha em conjunto com o inbox: mesmo que o MassTransit reentregue a mensagem após uma falha transitória, o inbox previne dupla contabilização caso a mensagem já tenha sido processada com sucesso antes da falha ser sinalizada ao broker.

## Scope / Limitations

Esta garantia de idempotência cobre apenas o consumo de `TransactionCreatedIntegrationEvent` por `Consolidation.Worker`. Não há mecanismo equivalente de deduplicação do lado da API de criação de transações (`POST /api/v1/transactions` não é idempotente por chave de idempotência de cliente).
