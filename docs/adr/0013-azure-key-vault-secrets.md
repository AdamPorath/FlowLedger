# ADR-0013 — Azure Key Vault como repositório durável de segredos

## Status

Accepted

## Context

A chave de assinatura JWT, a senha do administrador do Postgres e a connection string do RabbitMQ são segredos que precisam existir em produção sem estarem versionados em código ou expostos em texto plano em pipelines.

## Decision

Em modo de publicação (`isPublishMode`), o `AppHost` provisiona um recurso **Azure Key Vault** (`AddAzureKeyVault("key-vault")`) e registra cópias de três segredos nele: `jwt-signing-key-secret`, `postgres-admin-password-secret` e `rabbitmq-connection-string-secret` (via `keyVault.AddSecret(...)`), a partir dos mesmos parâmetros secretos do Aspire (`AddParameter(..., secret: true)`) que também são injetados diretamente como variáveis de ambiente/segredos dos Container Apps que precisam deles (`transactionsApi`, `consolidationApi`, `identityApi`, `gateway`).

## Rationale

- Manter uma cópia auditável e durável de cada segredo de produção em um cofre dedicado, independente do pipeline de CI/CD que os injeta a cada deploy — útil para rotação, auditoria e recuperação, mesmo que o pipeline de deploy não esteja em execução.
- Reaproveitar os mesmos parâmetros do Aspire tanto para os Container Apps quanto para o Key Vault evita definir o segredo em dois lugares diferentes no código do `AppHost`.

## Alternatives Considered

- **Segredos apenas como variáveis de ambiente/segredos do Container App**, sem Key Vault: mais simples, mas sem um repositório central e auditável independente do ciclo de vida do deploy — não adotada.
- **Container Apps lendo diretamente do Key Vault em runtime** (via referência de secret do Key Vault no próprio Container App): **não implementada neste código** — nenhum recurso de serviço tem `.WithReference(keyVault)`, portanto os contêineres em execução não buscam segredos do Key Vault em tempo real; eles recebem os valores diretamente como variáveis de ambiente providas pelos parâmetros do Aspire no momento do deploy.

## Trade-offs

- Como os serviços não leem do Key Vault em runtime, uma rotação de segredo feita diretamente no Key Vault (fora do pipeline de deploy) **não** se propaga automaticamente para os Container Apps em execução — seria necessário um novo `aspire deploy` com o parâmetro atualizado para que o novo valor chegue aos contêineres.
- `postgres-admin-username` não é replicado para o Key Vault (apenas a senha) — no estado atual do código, o nome de usuário do administrador do Postgres não tem uma cópia auditável no cofre.

## Consequences

- O Key Vault, tal como implementado, funciona como um repositório de auditoria/durabilidade dos segredos usados no último deploy, não como fonte de leitura em tempo de execução dos serviços.
- Os valores reais dos segredos continuam vindo, a cada deploy, dos GitHub Secrets (`AZURE_CLIENT_ID`, `JWT_SIGNING_KEY`, `POSTGRES_ADMIN_USERNAME`, `POSTGRES_ADMIN_PASSWORD`, `RABBITMQ_CONNECTION_STRING`), injetados como variáveis de ambiente no job `deploy` do `cd.yml`.
- Este comportamento é intencional e funcional para o escopo atual do repositório, não um erro de configuração: o Key Vault populado sem leitura em runtime já atende ao objetivo de auditoria/durabilidade dos segredos, sem exigir Managed Identity nos serviços.

## Scope / Limitations

Esta ADR descreve o comportamento exatamente como implementado no `AppHost.cs`; uma integração de leitura em runtime a partir do Key Vault via Managed Identity (comum em outros projetos Aspire) **não está presente neste repositório** e não deve ser documentada nem assumida como já implementada.
