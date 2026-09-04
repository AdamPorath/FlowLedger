# ADR-0011 — Observabilidade unificada com OpenTelemetry

## Status

Accepted

## Context

Com múltiplos serviços comunicando-se de forma síncrona (HTTP, via Gateway) e assíncrona (RabbitMQ/MassTransit), é necessário correlacionar logs, métricas e traces entre eles para diagnosticar problemas de ponta a ponta, tanto localmente quanto em produção.

## Decision

Toda a observabilidade é centralizada em `FlowLedger.ServiceDefaults` e herdada por todos os serviços via `AddServiceDefaults()`/`ConfigureOpenTelemetry()`: tracing com instrumentação de ASP.NET Core e HttpClient mais as fontes customizadas `MassTransit` e `Npgsql`; métricas com instrumentação de ASP.NET Core, HttpClient e runtime, mais os *meters* `MassTransit` e o meter de cada aplicação; logs via OpenTelemetry Logging (`IncludeFormattedMessage`, `IncludeScopes`). Um exportador **OTLP** é habilitado condicionalmente, apenas quando a variável `OTEL_EXPORTER_OTLP_ENDPOINT` está definida — cenário normal ao rodar via o dashboard do .NET Aspire, que injeta essa variável automaticamente.

## Rationale

- Compartilhar a configuração de telemetria em uma única biblioteca (`ServiceDefaults`) evita divergência de configuração entre serviços e garante que HTTP, banco (Npgsql) e mensageria (MassTransit) apareçam correlacionados no mesmo trace.
- Tornar a exportação OTLP condicional evita exigir um coletor OpenTelemetry configurado para simplesmente rodar os serviços localmente sem o Aspire.
- `Transactions.Api` e `Consolidation.Worker` complementam a telemetria padrão com contadores de negócio próprios (`flowledger.transactions.created`, `flowledger.consolidation.messages_processed`), dando visibilidade direta ao volume de transações/processamento, não apenas a métricas de infraestrutura.

## Alternatives Considered

- **Application Insights nativo (SDK específico da Azure)**: há um comentário no código (`ConfigureOpenTelemetry`) indicando essa opção como possível extensão futura (`Azure.Monitor.OpenTelemetry.AspNetCore`), mas está comentada/desabilitada — não faz parte da implementação atual.
- **Logging/métricas ad-hoc por serviço**, sem padronização central: rejeitada, pois dificultaria correlacionar eventos entre serviços distintos.

## Trade-offs

- Requisições para `/health` e `/alive` são explicitamente filtradas do tracing, reduzindo ruído — mas também significa que problemas nos próprios endpoints de health check não geram traces (mitigado pelo fato de que eles são simples o suficiente para não precisarem de tracing detalhado).
- Depende de um coletor/backend OTLP externo estar disponível para que a telemetria seja, de fato, persistida e consultável fora do dashboard local do Aspire.

## Consequences

- Qualquer novo serviço adicionado à solução herda automaticamente a mesma observabilidade ao chamar `AddServiceDefaults()`, sem configuração adicional.
- Métricas de negócio customizadas seguem o padrão `System.Diagnostics.Metrics` (`Meter`/`Counter<long>`), consistente com a instrumentação nativa do OpenTelemetry para .NET.

## Scope / Limitations

Não há dashboard de produção, alertas ou backend de observabilidade (ex.: Grafana, Azure Monitor) provisionado neste repositório — apenas a instrumentação e a exportação condicional via OTLP.
