# ADR-0014 — Graceful shutdown baseado em mecanismos nativos do .NET e do ACA

## Status

Accepted

## Context

Ao escalar para baixo, reiniciar ou implantar uma nova versão, o Azure Container Apps envia `SIGTERM` aos contêineres antes de eventualmente enviar `SIGKILL`. Se o processo for encerrado abruptamente enquanto uma requisição HTTP está em andamento ou uma mensagem está sendo processada pelo MassTransit, isso pode gerar respostas truncadas ao cliente ou reprocessamento desnecessário de mensagens.

## Decision

Não foi implementado nenhum código customizado de shutdown. O comportamento depende inteiramente de mecanismos nativos: drenagem de conexões pelo Kestrel, `HostOptions.ShutdownTimeout` padrão do .NET Generic Host (30 segundos) e a integração nativa do MassTransit com `IHostApplicationLifetime` (o bus para de aceitar novas mensagens e aguarda o processamento em andamento antes de finalizar). O único ajuste explícito no código é, no modelo de publicação para Azure Container Apps (`AppHost.cs`, função `ConfigureHealthProbes`), a definição de `app.Template.TerminationGracePeriodSeconds = 45` para todos os 5 serviços — um valor maior que o `ShutdownTimeout` padrão do host, para garantir que o ACA aguarde tempo suficiente antes de enviar `SIGKILL`.

## Rationale

- O .NET Generic Host e o MassTransit já implementam o comportamento correto de shutdown gracioso por padrão; não havia necessidade identificada de lógica adicional além de garantir que a plataforma (ACA) desse tempo suficiente para esse processo nativo se completar.
- Definir explicitamente o `TerminationGracePeriodSeconds` evita depender do valor padrão do Container Apps, tornando a relação entre o timeout do host (.NET) e o grace period da plataforma (ACA) explícita e intencional no código.

## Alternatives Considered

- **Implementar um `IHostedService` customizado para lógica de shutdown adicional** (ex.: drenar filas customizadas, aguardar handlers em execução): não foi necessário — o comportamento nativo do Kestrel e do MassTransit já cobre os cenários existentes no sistema (requisições HTTP e consumo de mensagens).
- **Não configurar `TerminationGracePeriodSeconds` explicitamente** (usar o padrão do ACA): rejeitada após análise, pois o valor padrão da plataforma poderia ser menor que o `ShutdownTimeout` do host, arriscando `SIGKILL` prematuro antes do shutdown gracioso terminar.

## Trade-offs

- Depender de comportamento padrão de bibliotecas (.NET Host, MassTransit) significa que qualquer mudança futura de configuração dessas bibliotecas (ex.: reduzir `ShutdownTimeout` para um valor customizado) deve ser revisitada em conjunto com o `TerminationGracePeriodSeconds` do ACA para manter a relação `grace period > shutdown timeout`.
- Não há teste automatizado específico validando o comportamento de graceful shutdown sob carga (ex.: verificar que nenhuma mensagem é perdida durante um `SIGTERM` simulado).

## Consequences

- Qualquer novo serviço adicionado à topologia de produção deve passar pelo mesmo `ConfigureHealthProbes` (ou uma configuração equivalente) para herdar o `TerminationGracePeriodSeconds` explícito, já que esse valor não é um default global do ambiente do Container Apps neste código.

## Scope / Limitations

Esta decisão cobre apenas o comportamento de encerramento do processo em si; não cobre estratégias de deployment sem downtime (ex.: blue-green, canary) no nível do Container Apps Environment, que não estão modeladas neste repositório.
