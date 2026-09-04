# ADR-0008 — PostgreSQL como armazenamento de dados

## Status

Accepted

## Context

`Transactions.Api` e `Consolidation.Worker` precisam persistir, respectivamente, transações financeiras e saldos consolidados, com suporte a transações ACID (necessárias para o outbox/inbox transacional descrito nas ADR-0004 e ADR-0005) e boa integração com EF Core.

## Decision

**PostgreSQL** (via Npgsql/EF Core) é o único banco de dados relacional usado no projeto, com dois bancos lógicos separados no mesmo servidor: `flowledger` (propriedade de `Transactions.Api`) e `consolidation` (propriedade de `Consolidation.Worker`, também lido por `Consolidation.Api` através do mesmo `ConsolidationDbContext`, via referência de projeto). Localmente, o Postgres roda como contêiner Docker orquestrado pelo Aspire (`AddPostgres`); em produção, como Azure PostgreSQL Flexible Server (`AddAzurePostgresFlexibleServer`), com autenticação por usuário/senha.

## Rationale

- EF Core com Npgsql tem suporte de primeira classe no MassTransit para outbox/inbox (`AddEntityFrameworkOutbox(o => o.UsePostgres())`), usado nos dois lados da mensageria.
- Manter bancos lógicos separados por bounded context (`flowledger` vs `consolidation`) preserva o isolamento entre os domínios de escrita e consolidação, mesmo compartilhando o mesmo servidor físico.

## Alternatives Considered

- **Banco único compartilhado entre todos os serviços**: rejeitada, pois misturaria os esquemas de `Transactions.Api` e `Consolidation.Worker`, dificultando evolução independente e violando o isolamento de bounded context reforçado pelos testes de arquitetura.
- **SQL Server**: não escolhido — não há indício no código de uso ou de suporte planejado a esse provedor.

## Trade-offs

- `Consolidation.Api` não possui um `DbContext`/modelo de leitura verdadeiramente independente: ele referencia o projeto `Consolidation.Worker` e reutiliza sua classe `ConsolidationDbContext` diretamente. Isso acopla, em tempo de compilação, o serviço de leitura ao assembly do serviço de escrita da consolidação — uma inconsistência real em relação a uma separação CQRS "pura" com stores independentes, e deve ser lida como tal.
- Dois bancos lógicos no mesmo servidor físico compartilham a mesma superfície de infraestrutura (mesma instância do Postgres Flexible Server em produção), ainda que logicamente isolados.

## Consequences

- Migrações EF Core (pasta `Migrations/`) existem para `Transactions.Api` e `Consolidation.Worker`; `Consolidation.Api` não gera suas próprias migrações, pois não é dona do schema que consome.
- Em Development, cada serviço aplica suas migrações automaticamente na inicialização (`Database.MigrateAsync()`); em produção, o pipeline de CD aplica as migrações explicitamente via `dotnet ef database update` (ver seção de CI/CD do README).

## Scope / Limitations

`Identity.Api` não persiste dados em Postgres nem em nenhum outro banco — usuários existem apenas em memória (ver ADR-0010).
