# ADR-0006 — Separação CQRS por pasta/handler dentro de cada serviço

## Status

Accepted

## Context

`Transactions.Api`, `Consolidation.Api` e `Identity.Api` possuem operações de escrita e/ou leitura com necessidades de validação, modelagem e evolução distintas. É preciso decidir como organizar essa separação de responsabilidades dentro de cada serviço.

## Decision

Cada serviço com lógica de aplicação separa fisicamente comandos e queries em pastas dedicadas — `Application/Commands/<Nome>` e `Application/Queries/<Nome>` — cada uma com um *handler* simples (classe comum injetada via DI), sem uso de uma biblioteca de mediator (ex.: MediatR). Exemplos: `CreateTransactionHandler`/`GetTransactionHandler` em `Transactions.Api`, `GetDailyBalanceHandler` em `Consolidation.Api`, `LoginHandler` em `Identity.Api`. Os Minimal API endpoints injetam o handler diretamente e chamam seu método (`HandleAsync`/`Handle`).

## Rationale

- Mantém a separação de intenção entre "mudar estado" (Command) e "ler estado" (Query) visível na estrutura de pastas, sem a sobrecarga de uma infraestrutura de mediator para um número pequeno de handlers por serviço.
- Cada handler permanece uma classe simples e testável isoladamente (como evidenciado pelos testes unitários de `Consolidation.Worker`).

## Alternatives Considered

- **MediatR ou biblioteca de mediator equivalente**: adicionaria uma camada de indireção (dispatch por `IRequest`/`IRequestHandler`) não estritamente necessária dado o número reduzido de comandos/queries por serviço; não foi adotada.
- **Handlers dentro dos próprios arquivos de endpoint** (sem pasta `Application`): rejeitada por misturar a definição de rota HTTP com a lógica de aplicação, dificultando testes unitários independentes de ASP.NET Core.

## Trade-offs

- Sem uma infraestrutura de mediator, cross-cutting concerns (logging, validação, transações) que se aplicariam a todos os handlers precisam ser implementados manualmente em cada um, caso necessário — no estado atual do código, cada handler implementa apenas o que precisa (ex.: métricas próprias em `CreateTransactionHandler`).
- Em nível de serviço, a separação entre "escrita" (`Transactions.Api`) e "leitura consolidada" (`Consolidation.Api`) também constitui uma forma de CQRS distribuído — os dois lados nunca compartilham o mesmo modelo de escrita.

## Consequences

- Adicionar um novo caso de uso segue o padrão existente: nova pasta em `Application/Commands` ou `Application/Queries`, com seu próprio handler, sem necessidade de registrar nada em um pipeline de mediator.
- `FlowLedger.ArchitectureTests` garante que `Application` não dependa do namespace `Endpoints`, preservando a direção de dependência (Endpoints → Application, nunca o inverso).

## Scope / Limitations

`Consolidation.Api` não possui lado de comando (não escreve dados) — apenas a query `GetDailyBalance` existe nesse serviço, refletindo sua natureza puramente de leitura sobre dados populados por `Consolidation.Worker`.
