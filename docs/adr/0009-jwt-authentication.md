# ADR-0009 — Autenticação e autorização via JWT com segredo compartilhado

## Status

Accepted

## Context

O sistema precisa autenticar comerciantes e garantir que cada um só acesse/crie transações e consulte saldos consolidados associados a si mesmo, através de múltiplos serviços (`Gateway`, `Transactions.Api`, `Consolidation.Api`) que precisam validar essa identidade de forma independente.

## Decision

`Identity.Api` autentica usuário/senha e emite um **JWT** assinado com uma chave simétrica (HMAC), contendo, entre outras claims, `merchantId`. Essa mesma chave de assinatura (`Jwt:SigningKey`) é compartilhada, via configuração, entre `Identity.Api` (emissor) e `Gateway`, `Transactions.Api`, `Consolidation.Api` (validadores) — cada um configurando seu próprio `AddAuthentication().AddJwtBearer(...)` com `ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey` e `ValidateLifetime` habilitados. `Transactions.Api` e `Consolidation.Api` extraem o `merchantId` da claim do usuário autenticado (nunca de parâmetro de URL/corpo) para restringir os dados àquele comerciante.

## Rationale

- Um segredo simétrico compartilhado é a abordagem mais simples para múltiplos validadores internos, sem exigir infraestrutura de chave pública/JWKS.
- Validar o JWT de forma independente em cada serviço interno (e não apenas no Gateway) implementa defesa em profundidade: mesmo que um serviço interno seja acessado diretamente (ex.: erro de configuração de rede), ele ainda exige um token válido.

## Alternatives Considered

- **Confiar cegamente no Gateway** (serviços internos sem validação própria de JWT, assumindo que só o Gateway pode alcançá-los): rejeitada — não foi implementada, e adicionaria uma dependência implícita de topologia de rede como único controle de segurança.
- **Chaves assimétricas (RS256) com JWKS endpoint**: alternativa mais robusta para ambientes com múltiplos emissores/consumidores externos, não adotada — o escopo atual usa um único emissor interno (`Identity.Api`) e chave simétrica compartilhada por configuração.

## Trade-offs

- O mesmo token é validado mais de uma vez por requisição (Gateway + serviço de destino), custo de CPU aceito em troca da defesa em profundidade.
- A chave simétrica precisa ser distribuída de forma consistente e seruga entre 4 serviços — tratada como segredo (`jwt-signing-key`, ver ADR-0013).

## Consequences

- Não há políticas de autorização baseadas em roles/scopes no código — a única regra de autorização de negócio é a extração do `merchantId` da claim, usada para filtrar dados por comerciante.
- Qualquer novo serviço que precise validar o JWT deve replicar a mesma configuração de `TokenValidationParameters` (issuer, audience, chave) usada nos demais.

## Scope / Limitations

Não há refresh token, revogação de token ou expiração configurável documentada no código além do `ValidateLifetime` padrão do middleware de JWT Bearer.
