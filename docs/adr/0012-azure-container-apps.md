# ADR-0012 — Azure Container Apps como plataforma de produção

## Status

Accepted

## Context

A solução precisa de uma plataforma de execução em produção para 5 serviços containerizáveis, com necessidade de escala independente por serviço (em especial, um consumidor de fila com exatamente uma réplica), ingress público restrito a um único serviço, e integração nativa com .NET Aspire para deployment.

## Decision

**Azure Container Apps (ACA)** é a única plataforma de produção modelada no `AppHost`, via `AddAzureContainerAppEnvironment("aca-env")`. Apenas o `Gateway` recebe ingress externo (`WithExternalHttpEndpoints()`); os demais serviços só são acessíveis dentro do ambiente do Container Apps. `Consolidation.Worker` é fixado em exatamente 1 réplica (`MinReplicas = MaxReplicas = 1`); os demais 4 serviços não têm `Scale` customizado no código, usando o comportamento padrão do Container Apps. Probes HTTP explícitos de liveness (`/alive`) e readiness (`/health`), além de `TerminationGracePeriodSeconds = 45`, são configurados para todos os 5 serviços via `PublishAsAzureContainerApp`/`ConfigureHealthProbes`.

## Rationale

- O .NET Aspire tem integração de primeira classe com Azure Container Apps (`aspire deploy`), permitindo derivar a infraestrutura de produção diretamente do modelo do `AppHost`, sem manter Bicep/Terraform separado (`infra/` está, de fato, vazio neste repositório).
- ACA suporta scale-to-zero e escala baseada em HTTP/fila nativamente, adequado a serviços com carga variável (as 4 APIs HTTP), enquanto permite fixar réplicas para o consumidor de fila único (`Consolidation.Worker`), que não deveria escalar horizontalmente sem lógica adicional de particionamento.
- Probes HTTP explícitos garantem que o ACA use exatamente os endpoints `/health`/`/alive` definidos por `ServiceDefaults`, e não uma inferência padrão de porta/protocolo.

## Alternatives Considered

- **Azure Kubernetes Service (AKS)**: ofereceria mais controle, mas exigiria gerenciar manifests/Helm e um plano de controle Kubernetes completo — complexidade não justificada pelo escopo do projeto, e sem integração tão direta com o modelo de publicação do Aspire usado aqui.
- **Azure App Service**: alternativa mais simples para apps web isolados, mas com suporte menos direto a um consumidor de fila de longa duração como `Consolidation.Worker` e a uma topologia de múltiplos serviços coordenados por um único `AppHost`.

## Trade-offs

- Acoplamento ao modelo de publicação do Aspire para Azure (`Azure.Provisioning.AppContainers`) — qualquer customização de infraestrutura de produção precisa ser expressa em C# dentro do `AppHost`, e não em arquivos de infraestrutura declarativos separados.
- RabbitMQ é tratado como dependência externa/gerenciada e não é provisionado como parte deste ambiente (ver ADR-0003) — é modelado apenas como uma connection string externa, transferindo essa responsabilidade para fora do `AppHost`. Essa é uma escolha válida e funcional para o escopo atual (o Aspire não oferece um recurso de hosting gerenciado para RabbitMQ no Azure equivalente ao que oferece para Postgres), não uma limitação acidental.

## Consequences

- O pipeline de CD (`cd.yml`) usa o **Aspire CLI** (`aspire deploy --apphost ... --environment production`) para materializar essa infraestrutura a partir do código do `AppHost`, autenticando no Azure via OIDC (`azure/login`).
- Qualquer alteração de escala, probes ou grace period para um serviço deve ser feita no `AppHost.cs`, dentro do bloco `if (isPublishMode)`.

## Scope / Limitations

Não há definição de região, SKU de Container Apps Environment, ou políticas de rede (VNet integration, private endpoints) no código — esses aspectos, se existirem, são resolvidos por fora do que está documentado neste repositório.
