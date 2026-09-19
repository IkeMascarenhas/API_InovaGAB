# APRESENTAÇÃO TÉCNICA — InovaGAB API
### Plataforma de Inovação Corporativa | Grupo Águia Branca

---

## SLIDE 1 — VISÃO GERAL DA SOLUÇÃO

**O que é o InovaGAB?**

A InovaGAB é uma plataforma digital end-to-end que conecta os mais de 20 mil colaboradores do Grupo Águia Branca a um ciclo estruturado de inovação corporativa. Ela resolve o problema central da perda de observações operacionais valiosas por falta de um canal digital organizado.

**Os 3 Pilares:**
- 🔵 **CAPTURAR** → Operadores registram ideias e dores do dia a dia
- 🟡 **ESTRUTURAR** → Gestores avaliam, aprovam e transformam ideias em projetos
- 🟢 **ACOMPANHAR** → Líderes monitoram KPIs e resultados financeiros (ROI)

**Stack Backend:**
- Linguagem: C# com .NET 8 Web API
- Banco de dados: MongoDB (NoSQL)
- Autenticação: JWT Bearer com roles (operator / manager / leader)
- IA: Google Gemini API
- Documentação: Swagger UI

---

## SLIDE 2 — ARQUITETURA EM CAMADAS

```
┌─────────────────────────────────────────────┐
│             App Android (Kotlin)            │
│         Base URL: http://10.0.2.2:5000      │
└────────────────────┬────────────────────────┘
                     │ HTTP + JWT
┌────────────────────▼────────────────────────┐
│              CONTROLLERS (API Layer)        │
│  AuthController  │  StrategiesController    │
│  IdeasController │  ProjectsController      │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│              SERVICES (Business Layer)      │
│  AuthService  │  StrategyService            │
│  IdeaService  │  ProjectService             │
│  GeminiService (IA Integration)             │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│           REPOSITORIES (Data Layer)         │
│  UserRepository    │  StrategyRepository    │
│  IdeaRepository    │  ProjectRepository     │
└────────────────────┬────────────────────────┘
                     │
┌────────────────────▼────────────────────────┐
│              MongoDB Atlas / Local          │
│  Collections: users, strategies,            │
│               ideas, projects               │
└─────────────────────────────────────────────┘
```

---

## SLIDE 3 — ESPECIFICAÇÃO DE ENDPOINTS: AUTENTICAÇÃO

### `POST /api/auth/register`
Registra um novo usuário na plataforma.

**Payload:**
```json
{
  "nome": "Maria Oliveira",
  "email": "maria@aguiabranca.com.br",
  "password": "Senha@123",
  "perfil": "operator"
}
```
> Perfis aceitos: `operator` | `manager` | `leader`

**Resposta 201:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "64f8a2b3c1e2d3f4a5b6c7d8",
  "nome": "Maria Oliveira",
  "email": "maria@aguiabranca.com.br",
  "perfil": "operator",
  "expiresAt": "2025-09-20T17:00:00Z"
}
```

---

### `POST /api/auth/login`
Autentica o usuário e retorna o token JWT.

**Payload:**
```json
{
  "email": "maria@aguiabranca.com.br",
  "password": "Senha@123"
}
```

**Resposta 200:** Mesmo formato de `/register`

---

## SLIDE 4 — ESPECIFICAÇÃO DE ENDPOINTS: ESTRATÉGIAS

> 🔐 **Autorização:** Header `Authorization: Bearer {token}`

### `GET /api/strategies` — Listar todas [operator, manager, leader]
**Resposta 200:**
```json
[
  {
    "id": "64f8a2b3c1e2d3f4a5b6c7d8",
    "titulo": "Eficiência de Rotas 2025",
    "descricao": "Reduzir custo operacional com otimização de rotas.",
    "categoria": "Redução de Custos",
    "campanha": "Inova Águia 2025",
    "status": "ativo",
    "dataCriacao": "2025-01-15T10:00:00Z"
  }
]
```

### `GET /api/strategies/active` — Listar ativas [operator, manager, leader]
Retorna apenas estratégias com `status: "ativo"`. Usado pelo app para popular dropdowns ao criar uma ideia.

### `GET /api/strategies/{id}` — Obter por ID [operator, manager, leader]

### `POST /api/strategies` — Criar [leader only]
**Payload:**
```json
{
  "titulo": "Sustentabilidade e Combustível Verde",
  "descricao": "Reduzir 15% no consumo de diesel até Dec/2025.",
  "categoria": "Sustentabilidade",
  "campanha": "Frota Verde 2025",
  "status": "ativo"
}
```

### `PUT /api/strategies/{id}` — Atualizar [leader only]
Todos os campos são opcionais. Apenas os enviados são atualizados.

### `DELETE /api/strategies/{id}` — Remover [leader only]
**Resposta 204 No Content**

---

## SLIDE 5 — ESPECIFICAÇÃO DE ENDPOINTS: IDEIAS

### `POST /api/ideas` — Cadastrar ideia [operator only]
**Payload:**
```json
{
  "titulo": "Rota alternativa SP-Campinas às sextas",
  "descricao": "A Rodovia Anhanguera fica congestionada toda sexta após 16h. A Rota dos Bandeirantes reduz em 40min o trajeto.",
  "strategyId": "64f8a2b3c1e2d3f4a5b6c7d8"
}
```
**Resposta 201:**
```json
{
  "id": "64f8b3c4d5e6f7a8b9c0d1e2",
  "titulo": "Rota alternativa SP-Campinas às sextas",
  "descricao": "A Rodovia Anhanguera fica congestionada...",
  "status": "pendente",
  "autorId": "64f8a2b3c1e2d3f4a5b6c7d8",
  "autorNome": "Maria Oliveira",
  "strategyId": "64f8a2b3c1e2d3f4a5b6c7d8",
  "comentarioGestor": "",
  "pontuacaoIA": 78,
  "justificativaIA": "Ideia bem alinhada à estratégia de redução de custos. Alto potencial de impacto e viabilidade comprovada em rotas similares.",
  "dataCriacao": "2025-09-19T14:30:00Z"
}
```

### `GET /api/ideas/my` — Minhas ideias [operator only]

### `GET /api/ideas/{id}` — Detalhe [operator (próprias) | manager | leader]

### `DELETE /api/ideas/{id}` — Excluir [operator — somente status 'pendente']

### `GET /api/ideas` — Listar todas [manager, leader]
### `GET /api/ideas/status/{status}` — Filtrar por status [manager, leader]
> Status: `pendente` | `em_analise` | `aprovada` | `rejeitada` | `convertida_em_projeto`

### `PATCH /api/ideas/{id}/status` — Atualizar status [manager only]
**Payload:**
```json
{
  "status": "aprovada",
  "comentarioGestor": "Excelente observação. Encaminhada para criação de projeto.",
  "strategyId": null
}
```

---

## SLIDE 6 — ESPECIFICAÇÃO DE ENDPOINTS: PROJETOS

### `POST /api/projects` — Criar projeto [manager only]
> ⚠️ A ideia referenciada DEVE estar no status `aprovada`.

**Payload:**
```json
{
  "nome": "Projeto Rota Alternativa SP-Campinas",
  "descricao": "Implementar rota via Bandeirantes às sextas-feiras para frota de longa distância.",
  "ideiaId": "64f8b3c4d5e6f7a8b9c0d1e2",
  "strategyId": "64f8a2b3c1e2d3f4a5b6c7d8",
  "investimento": 45000.00,
  "retornoEsperado": 180000.00,
  "prazoMeses": 3
}
```

**Resposta 201:**
```json
{
  "id": "64f9c4d5e6f7a8b9c0d1e2f3",
  "nome": "Projeto Rota Alternativa SP-Campinas",
  "descricao": "...",
  "ideiaId": "64f8b3c4d5e6f7a8b9c0d1e2",
  "strategyId": "64f8a2b3c1e2d3f4a5b6c7d8",
  "status": "planejamento",
  "gestorId": "64f7a1b2c3d4e5f6a7b8c9d0",
  "investimento": 45000.00,
  "retornoEsperado": 180000.00,
  "roiPercentual": 300.0,
  "prazoMeses": 3,
  "progresso": 0,
  "dataCriacao": "2025-09-19T15:00:00Z"
}
```

### `GET /api/projects` — Listar todos [manager, leader]
### `GET /api/projects/my` — Projetos do gestor [manager only]
### `GET /api/projects/{id}` — Detalhe [manager, leader]
### `GET /api/projects/status/{status}` — Filtrar [manager, leader]
> Status: `planejamento` | `em_andamento` | `concluido` | `cancelado`

### `PUT /api/projects/{id}` — Atualizar [manager — somente responsável]
**Payload (todos opcionais):**
```json
{
  "status": "em_andamento",
  "progresso": 35,
  "investimento": 48000.00,
  "retornoEsperado": 192000.00
}
```

### `DELETE /api/projects/{id}` — Remover [manager — somente responsável]

---

## SLIDE 7 — DASHBOARD EXECUTIVO (Líderes)

### `GET /api/projects/dashboard` [leader only]

**Resposta 200:**
```json
{
  "totalIdeias": 142,
  "ideiasPendentes": 38,
  "ideiasAprovadas": 67,
  "ideiasRejeitadas": 21,
  "totalProjetos": 18,
  "projetosEmAndamento": 9,
  "projetosConcluidos": 5,
  "investimentoTotal": 870000.00,
  "retornoTotal": 3480000.00,
  "roiMedioPercentual": 300.00,
  "lucroObtido": 2610000.00,
  "mediaPrazoMeses": 6,
  "mediaProgresso": 42.5,
  "totalStrategies": 4
}
```

**Métricas disponíveis para o Líder:**
| Campo | Descrição |
|---|---|
| `roiMedioPercentual` | Retorno sobre Investimento médio da carteira |
| `lucroObtido` | Retorno Total − Investimento Total |
| `investimentoTotal` | Soma de todos os investimentos em projetos |
| `mediaProgresso` | Progresso médio (%) de todos os projetos |
| `mediaPrazoMeses` | Prazo médio dos projetos |

---

## SLIDE 8 — INTEGRAÇÃO COM IA: GOOGLE GEMINI

### Qual problema a IA resolve?

**Contexto do problema:** Com mais de 20 mil colaboradores submetendo ideias, os gestores enfrentam sobrecarga na triagem manual. Uma ideia mal descrita ou desalinhada com a estratégia pode consumir horas de análise desnecessária.

**Solução implementada:** O sistema utiliza o **Google Gemini 1.5 Flash** para automatizar a pontuação e priorização das ideias recém-cadastradas, antes mesmo de o gestor abrir a tela de revisão.

---

### Como Funciona Tecnicamente

**1. Trigger (quando acontece):**
Imediatamente após o operador cadastrar uma nova ideia via `POST /api/ideas`, o sistema dispara a avaliação da IA em background (padrão fire-and-forget assíncrono), sem bloquear a resposta ao usuário.

**2. O Prompt de IA (engenharia de prompt):**
O sistema envia um prompt cuidadosamente estruturado ao Gemini contendo:
- O contexto da empresa (setor de transporte e logística)
- A Estratégia Corporativa vigente (título, categoria, campanha, descrição)
- A Ideia submetida (título e descrição)
- 5 critérios objetivos de avaliação (20 pontos cada)

**3. Critérios de Pontuação (0-100):**

| Critério | Peso | O que avalia |
|---|---|---|
| Alinhamento Estratégico | 20 pts | Alinhamento com a estratégia corporativa ativa |
| Impacto no Negócio | 20 pts | Potencial de redução de custo / ganho de eficiência |
| Viabilidade | 20 pts | Realizabilidade com recursos de logística |
| Inovação | 20 pts | Originalidade e diferenciação da proposta |
| Clareza | 20 pts | Qualidade e objetividade da descrição |

**4. Resposta da IA (JSON estruturado):**
```json
{
  "pontuacao": 78,
  "justificativa": "Ideia bem alinhada à estratégia de redução de custos. Alto potencial de impacto operacional e viabilidade comprovada em rotas similares de outras empresas do setor."
}
```

**5. Armazenamento:**
Os campos `pontuacaoIA` e `justificativaIA` são persistidos diretamente no documento da Ideia no MongoDB.

---

### Benefício de Negócio

| Antes (sem IA) | Depois (com IA) |
|---|---|
| Gestor analisa todas as ideias manualmente | Gestor vê pontuação + justificativa ao abrir a lista |
| Média de 15 min por ideia para triagem | Triagem inicial em < 30 segundos |
| Critérios subjetivos e inconsistentes | Critérios padronizados e auditáveis |
| Boas ideias podem ser ignoradas por volume | Ranking automático prioriza as mais relevantes |

> **Resultado:** Os gestores focam seu tempo nas ideias com maior pontuação, aumentando a taxa de conversão de ideias em projetos e maximizando o impacto estratégico da plataforma.

---

## SLIDE 9 — MODELOS DE DADOS

### Strategy
```json
{
  "id": "ObjectId",
  "titulo": "string",
  "descricao": "string",
  "categoria": "string",
  "campanha": "string",
  "status": "ativo|inativo",
  "dataCriacao": "DateTime"
}
```

### Idea
```json
{
  "id": "ObjectId",
  "titulo": "string",
  "descricao": "string",
  "status": "pendente|em_analise|aprovada|rejeitada|convertida_em_projeto",
  "autorId": "ObjectId",
  "autorNome": "string",
  "strategyId": "ObjectId",
  "comentarioGestor": "string",
  "pontuacaoIA": "int (0-100)",  ← IA Gemini
  "justificativaIA": "string",   ← IA Gemini
  "dataCriacao": "DateTime"
}
```

### Project
```json
{
  "id": "ObjectId",
  "nome": "string",
  "descricao": "string",
  "ideiaId": "ObjectId",
  "strategyId": "ObjectId",
  "status": "planejamento|em_andamento|concluido|cancelado",
  "gestorId": "ObjectId",
  "investimento": "double",
  "retornoEsperado": "double",
  "roiPercentual": "double (calculado)",
  "prazoMeses": "int",
  "progresso": "int (0-100)",
  "dataCriacao": "DateTime"
}
```

---

## SLIDE 10 — REGRAS DE NEGÓCIO IMPLEMENTADAS

✅ Operadores só visualizam suas **próprias** ideias  
✅ Ideias só podem ser excluídas se estiverem no status `pendente`  
✅ Projetos só podem ser criados a partir de ideias no status `aprovada`  
✅ Ao criar um projeto, a ideia é automaticamente marcada como `convertida_em_projeto`  
✅ Uma ideia só pode originar **um único** projeto (restrição verificada no backend)  
✅ Estratégias `inativas` não podem ser selecionadas ao criar ideias/projetos  
✅ Gestores só podem editar/excluir **seus próprios** projetos  
✅ ROI é sempre calculado no backend: `((retorno - investimento) / investimento) × 100`  
✅ Senhas armazenadas com **BCrypt** (salt + hash — nunca em texto puro)  
✅ Tokens JWT expiram em **24 horas**  
