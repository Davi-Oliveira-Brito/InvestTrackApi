# InvestTrack — Roadmap de Desenvolvimento

Plataforma de análise de portfólio de investimentos. Simulação de carteira (ações, FIIs, renda fixa), cálculo de rentabilidade e métricas de risco, comparação com benchmarks (CDI/Ibovespa) e simulador de cenários.

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# .NET 10, Clean Architecture, EF Core |
| Banco | PostgreSQL (Supabase) |
| Frontend | Next.js (App Router) |
| Deploy API | Render |
| Deploy Front | Vercel |
| CI/CD | GitHub Actions |

## Princípio geral

Ordem de construção: **Banco → Domínio (C#) → API mínima → Front conectado → incrementar features.**

Regra de ouro: **sempre terminar cada sprint com algo rodando ponta a ponta em produção**, mesmo que incompleto. Evita o cenário "código bonito, nunca integrado".

---

## Sprint 0 — Setup (1-2 dias)

Fundação do projeto, sem lógica de negócio.

- [ ] Criar repositório(s) — mono-repo ou `api` + `web` separados
- [ ] Solution .NET 10 com estrutura em camadas:
  - `InvestTrack.Domain`
  - `InvestTrack.Application`
  - `InvestTrack.Infrastructure`
  - `InvestTrack.Api`
- [ ] Projeto Next.js com App Router, Tailwind, shadcn/ui
- [ ] Criar projeto no Supabase e obter connection string do Postgres
- [ ] Criar Web Service no Render (deploy vazio, só validar pipeline)
- [ ] Conectar repositório do front à Vercel
- [ ] `docker-compose` local com Postgres (dev não depende do Supabase o tempo todo)

**Critério de sucesso:** `dotnet run` sobe a API local, `npm run dev` sobe o front, `GET /health` retorna 200 local e no Render.

---

## Sprint 1 — Ponta a ponta funcionando + Auth

- [ ] Domain: entidade `User`
- [ ] Infrastructure: EF Core configurado, primeira migration, conexão com Supabase
- [ ] Auth: registro e login com JWT (hash de senha com BCrypt)
- [ ] Front: telas de login/registro consumindo a API, token salvo
- [ ] Deploy real: API no Render + front no Vercel funcionando em produção

**Critério de sucesso:** criar uma conta pelo site em produção e o usuário aparecer no banco do Supabase.

> Este é o sprint mais importante do projeto — depois dele, tudo é incremento.

> **Pendência conhecida:** Sprint 1 implementa só access token JWT (2h), sem refresh token. Adiado deliberadamente para reduzir escopo — revisitar em sprint futuro se a expiração de 2h incomodar a UX.

---

## Sprint 2 — CRUD de carteira

- [ ] Domain: entidades `Ativo`, `Posicao` (ticker, quantidade, preço médio, data da compra)
- [ ] Application: casos de uso (adicionar / editar / remover posição, listar carteira)
- [ ] API: endpoints protegidos por JWT
- [ ] Front: tela "Minha Carteira" (formulário + tabela de posições)
- [ ] Testes unitários das validações (ex: quantidade não pode ser negativa)

**Critério de sucesso:** usuário loga, adiciona ativos, vê a lista na tela, dados persistem no Postgres.

---

## Sprint 3 — Cotações e Dashboard

- [ ] Integração com API externa de cotações (ex: BRAPI.dev)
- [ ] Background job (`IHostedService` ou Hangfire) para atualizar preços periodicamente
- [ ] Cálculo de valor atual da carteira e rentabilidade (R$ e %) por ativo e total
- [ ] Front: Dashboard com cards de resumo + gráfico de pizza (alocação por ativo/classe)

**Critério de sucesso:** ao logar, o dashboard mostra o valor atualizado da carteira com base em cotação real.

> Sprint que separa "CRUD bonito" de "sistema com inteligência" — capricha aqui.

---

## Sprint 4 — Métricas e comparação com benchmarks

- [ ] Cálculo de volatilidade, Sharpe Ratio, drawdown máximo (classe de domínio isolada e bem testada)
- [ ] Buscar histórico de CDI e Ibovespa (dados públicos: Banco Central / B3) para comparação
- [ ] Front: gráfico de linha "carteira vs CDI vs Ibovespa"
- [ ] Simulador de cenário: "se eu tivesse investido X em Y na data Z"

**Critério de sucesso:** mostrar um gráfico comparando a carteira fictícia com o Ibovespa e ser entendido de imediato.

---

## Sprint 5 — Polimento

- [ ] CI/CD: GitHub Actions rodando testes + build a cada push, deploy automático
- [ ] Logs estruturados (Serilog)
- [ ] Middleware global de tratamento de erros
- [ ] Rate limiting básico
- [ ] README caprichado: diagrama de arquitetura, como rodar localmente, decisões técnicas
- [ ] Ajustes de UX/UI (loading states, responsividade, dark mode)
- [ ] Vídeo curto de demo (2-3 min)

**Critério de sucesso:** alguém de fora entende o projeto em 3 minutos olhando README + demo, sem perguntar nada.

---

## Backlog (se sobrar tempo)

- [ ] Testes de integração (`WebApplicationFactory`)
- [ ] Cache com Redis nas cotações
- [ ] Notificação por e-mail quando um ativo variar X%
- [ ] Suporte a multi-moeda / ativos internacionais

---

## Log de progresso

> Atualizar a cada sprint concluída: data, o que foi feito, decisões técnicas tomadas, problemas encontrados.

### Sprint 0
- Status: Concluida

### Sprint 1
- Status: não iniciado

### Sprint 2
- Status: não iniciado

### Sprint 3
- Status: não iniciado

### Sprint 4
- Status: não iniciado

### Sprint 5
- Status: não iniciado