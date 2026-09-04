# ADR-0002 — Gateway como único ponto de entrada público

## Status

Accepted

## Context

Com múltiplos serviços internos (`Identity.Api`, `Transactions.Api`, `Consolidation.Api`), é preciso decidir se cada um expõe ingress público próprio ou se existe um único ponto de entrada externo responsável por roteamento e proteções transversais (autenticação, rate limiting, cabeçalhos de segurança).

## Decision

`FlowLedger.Gateway`, implementado com YARP (Reverse Proxy), é o único serviço com endpoint externo (`WithExternalHttpEndpoints()` no `AppHost`). Ele expõe três rotas — `/api/v1/auth/**`, `/api/v1/transactions/**`, `/api/v1/consolidation/**` — cada uma mapeada para o cluster correspondente (`identity-cluster`, `transactions-cluster`, `consolidation-cluster`), sem rota "catch-all". `Identity.Api`, `Transactions.Api` e `Consolidation.Api` permanecem acessíveis apenas dentro do ambiente do Container Apps.

## Rationale

- Concentrar rate limiting, validação de JWT, cabeçalhos de segurança e HSTS/HTTPS redirect em um único lugar, evitando duplicar essa configuração em cada serviço.
- Reduzir a superfície de ataque exposta publicamente: apenas rotas explicitamente mapeadas chegam aos serviços internos.
- Permitir que os serviços internos usem descoberta de serviço (`https+http://transactions-api`, etc.) e permaneçam isolados da rede pública.

## Alternatives Considered

- **Ingress direto por serviço**: eliminaria o Gateway como ponto único, mas exigiria duplicar autenticação, rate limiting e cabeçalhos de segurança em cada API, e exporia mais superfície publicamente.
- **API Management gerenciado (Azure APIM)**: alternativa de nuvem gerenciada, não adotada — o roteamento e a segurança de borda estão implementados em código (YARP) dentro do próprio `AppHost`.

## Trade-offs

- O Gateway se torna um ponto único de falha para todo o tráfego externo (mitigado, em produção, pela escala/replicação padrão do Container Apps, já que não há `Scale` customizado para ele).
- Cada requisição autenticada tem o JWT validado duas vezes (Gateway + serviço de destino — ver ADR-0009), custo aceito em troca de defesa em profundidade.

## Consequences

- Qualquer nova rota de negócio precisa ser explicitamente adicionada ao `ReverseProxy` do `Gateway`; não há fallback automático.
- Os valores reservados do YARP `AuthorizationPolicy: "Anonymous"` (rota de login) e `AuthorizationPolicy: "default"` (demais rotas) definem a exigência de autenticação por rota sem necessidade de políticas nomeadas customizadas.

## Scope / Limitations

Não há WAF, CDN ou proteção de camada de rede adicional documentada neste repositório — as proteções descritas aqui são as implementadas em código no próprio `Gateway`.
