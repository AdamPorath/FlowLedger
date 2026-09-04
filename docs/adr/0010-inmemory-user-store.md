# ADR-0010 — InMemoryUserStore no contexto do desafio técnico

## Status

Accepted

## Context

O sistema precisa de um conceito mínimo de usuário/comerciante para autenticação, sem que a gestão de identidade seja o foco do desafio técnico proposto.

## Decision

`Identity.Api` mantém um `InMemoryUserStore` (dicionário em memória, chaveado por username, case-insensitive), sem qualquer persistência em banco de dados. Em ambiente de Development, um único usuário de teste (`merchant-test` / `Passw0rd!`, com senha tratada por `PasswordHasher<User>`) é semeado na inicialização. Não existe endpoint de cadastro/gestão de usuários.

## Rationale

- Permite que o restante do sistema (autenticação, autorização por `merchantId`, o fluxo completo de criação de transação e consolidação) seja implementado e testado de ponta a ponta sem a complexidade adicional de um serviço de identidade completo.
- Mantém o foco do desafio nas decisões arquiteturais de mensageria, consistência, CQRS e observabilidade, que são o objeto central deste repositório.

## Alternatives Considered

- **Persistência real de usuários (banco de dados + hashing + endpoint de cadastro)**: representaria uma solução mais completa para produção, mas não foi implementada — deliberadamente fora do escopo deste desafio.
- **Provedor de identidade externo (Azure AD B2C, Auth0, etc.)**: alternativa válida para um cenário de produção real, não adotada aqui.

## Trade-offs

- Usuários são perdidos a cada reinício do processo `Identity.Api` — não há continuidade de identidade entre execuções.
- O único usuário de teste é semeado apenas em Development; não há indicação no código de comportamento equivalente em produção, o que significa que, tal como está, o serviço de identidade em produção não teria nenhum usuário disponível sem alteração de código.

## Consequences

- Qualquer teste ou uso manual do sistema depende do usuário de teste semeado em Development (ver seção "Como executar e validar manualmente" do README).
- Esta decisão deve ser revisitada antes de qualquer uso além do contexto de desafio/demonstração técnica.

## Scope / Limitations

Esta ADR documenta uma limitação deliberada e conhecida, não uma recomendação de arquitetura para produção real. Não deve ser interpretada como "pronta para produção".
