# InovaGAB API — Backend C# .NET 8

> **Plataforma de Inovação Corporativa do Grupo Águia Branca**  
> API RESTful com autenticação JWT por perfis, MongoDB e integração com Google Gemini AI.

---

## 📋 Pré-requisitos

| Ferramenta | Versão mínima | Download |
|---|---|---|
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download/dotnet/8.0 |
| MongoDB Community | 7.0 | https://www.mongodb.com/try/download/community |
| Git | Qualquer | https://git-scm.com |

---

## ⚙️ Variáveis de Ambiente / Configuração

Abra o arquivo `appsettings.json` e substitua os valores:

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "InovaGABDb"
  },
  "JwtSettings": {
    "SecretKey": "MINIMA_32_CARACTERES_CHAVE_SECRETA_AQUI",
    "Issuer": "InovaGAB.API",
    "Audience": "InovaGAB.App",
    "ExpiresInHours": 24
  },
  "GeminiSettings": {
    "ApiKey": "SUA_GOOGLE_GEMINI_API_KEY",
    "ApiUrl": "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-lite-latest:generateContent"
  }
}
```

### Como obter a Gemini API Key

1. Acesse [Google AI Studio](https://aistudio.google.com/app/apikey)
2. Clique em **"Create API key"**
3. Copie a chave e cole em `GeminiSettings.ApiKey`

> ⚠️ **NUNCA** commite a API Key no repositório. Considere usar `appsettings.Development.json` ou variáveis de ambiente do sistema.

---

## 🚀 Execução Local

```bash
# 1. Clone o repositório
git clone https://github.com/seu-usuario/InovaGAB-back.git
cd InovaGAB-back

# 2. Restaure os pacotes NuGet
dotnet restore

# 3. Inicie o MongoDB (se não estiver rodando)
# Windows: net start MongoDB
# Linux/Mac: sudo systemctl start mongod

# 4. Execute a API
dotnet run

# A API estará disponível em:
# http://localhost:5000  (Swagger UI na raiz)
# http://localhost:5000/swagger
```

---

## 📱 Consumindo a API no App Android (Emulador)

No emulador Android, o endereço `10.0.2.2` aponta para o `localhost` da máquina host.

### Configuração no Kotlin (app Android)

```kotlin
// Constants.kt
object Constants {
    // Emulador Android → IP da máquina host
    const val BASE_URL = "http://10.0.2.2:5000/api/"
    
    // Dispositivo físico na mesma rede → IP local da máquina
    // const val BASE_URL = "http://192.168.1.XXX:5000/api/"
}
```

### Exemplo de requisição com Retrofit

```kotlin
// Interfaces de retrofit
interface AuthApi {
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>
    
    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>
}

interface IdeaApi {
    @GET("ideas/my")
    suspend fun getMyIdeas(@Header("Authorization") token: String): Response<List<IdeaResponse>>
    
    @POST("ideas")
    suspend fun createIdea(
        @Header("Authorization") token: String,
        @Body request: CreateIdeaRequest
    ): Response<IdeaResponse>
}
```

### Uso do token JWT

Após o login, armazene o token e inclua em toda requisição autenticada:

```kotlin
val authHeader = "Bearer ${token}"
// Use como header: Authorization: Bearer <token>
```

---

## 🔐 Matriz de Acesso (Roles)

| Endpoint | operator | manager | leader |
|---|:---:|:---:|:---:|
| `POST /auth/register` | ✅ | ✅ | ✅ |
| `POST /auth/login` | ✅ | ✅ | ✅ |
| `GET /strategies` | ✅ | ✅ | ✅ |
| `GET /strategies/active` | ✅ | ✅ | ✅ |
| `POST/PUT/DELETE /strategies` | ❌ | ❌ | ✅ |
| `POST /ideas` | ✅ | ❌ | ❌ |
| `GET /ideas/my` | ✅ | ❌ | ❌ |
| `DELETE /ideas/{id}` | ✅ (próprias) | ❌ | ❌ |
| `GET /ideas` | ❌ | ✅ | ✅ |
| `PATCH /ideas/{id}/status` | ❌ | ✅ | ❌ |
| `POST /projects` | ❌ | ✅ | ❌ |
| `PUT/DELETE /projects/{id}` | ❌ | ✅ (próprios) | ❌ |
| `GET /projects` | ❌ | ✅ | ✅ |
| `GET /projects/dashboard` | ❌ | ❌ | ✅ |

---

## 🗂️ Estrutura do Projeto

```
InovaGAB-back/
├── Controllers/
│   ├── AuthController.cs
│   ├── StrategiesController.cs
│   ├── IdeasController.cs
│   └── ProjectsController.cs
├── DTOs/
│   └── Dtos.cs
├── Models/
│   ├── User.cs
│   ├── Strategy.cs
│   ├── Idea.cs
│   └── Project.cs
├── Repositories/
│   ├── UserRepository.cs
│   ├── StrategyRepository.cs
│   ├── IdeaRepository.cs
│   └── ProjectRepository.cs
├── Services/
│   ├── AuthService.cs
│   ├── StrategyService.cs
│   ├── IdeaService.cs
│   ├── ProjectService.cs
│   └── GeminiService.cs
├── Settings/
│   └── AppSettings.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── Program.cs
└── InovaGAB.API.csproj
```

---

## 🧪 Testando com cURL

```bash
# 1. Registrar um operador
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"nome":"João Silva","email":"joao@aguiabranca.com.br","password":"Senha@123","perfil":"operator"}'

# 2. Fazer login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"joao@aguiabranca.com.br","password":"Senha@123"}'

# 3. Criar ideia (use o token do login)
curl -X POST http://localhost:5000/api/ideas \
  -H "Authorization: Bearer SEU_TOKEN_AQUI" \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Rota alternativa SP-RJ","descricao":"A rota pela Via Dutra tem gargalos nas segundas-feiras. Sugiro análise da Fernão Dias como alternativa.","strategyId":"ID_DA_ESTRATEGIA_ATIVA"}'
```

---

## 📦 Pacotes NuGet Utilizados

| Pacote | Versão | Finalidade |
|---|---|---|
| `MongoDB.Driver` | 2.27.0 | Driver NoSQL MongoDB |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.8 | Autenticação JWT |
| `System.IdentityModel.Tokens.Jwt` | 8.0.2 | Geração de tokens |
| `BCrypt.Net-Next` | 4.0.3 | Hash de senhas |
| `Swashbuckle.AspNetCore` | 6.7.3 | Swagger UI |
| `System.Net.Http.Json` | 8.0.1 | HttpClient para Gemini |
