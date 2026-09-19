# 📱 Guia Definitivo de Integração: InovaGAB API ↔ Android Kotlin

Este guia fornece o passo a passo completo, com código pronto para copiar e colar, para integrar o aplicativo Android Kotlin (`InovaGAB-main`) à API backend C# .NET 8.

---

## 1. Configurações Críticas de Rede no Android

Antes de escrever qualquer código Kotlin, o Android precisa ter permissão para acessar a rede e permitir conexões HTTP (Cleartext).

### 1.1 `AndroidManifest.xml`
Abra `app/src/main/AndroidManifest.xml` e garanta que:
1. A permissão de Internet esteja declarada **fora** da tag `<application>`.
2. O atributo `android:usesCleartextTraffic="true"` esteja dentro da tag `<application>` (necessário pois a API local roda em `http://`, sem SSL).

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">

    <!-- 1. Permissão de Internet obrigatória -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />

    <application
        android:allowBackup="true"
        android:icon="@mipmap/ic_launcher"
        android:label="@string/app_name"
        android:roundIcon="@mipmap/ic_launcher_round"
        android:supportsRtl="true"
        android:theme="@style/Theme.InovaGAB"
        android:usesCleartextTraffic="true"> <!-- 2. Permite tráfego HTTP local -->

        <!-- Suas activities aqui -->

    </application>
</manifest>
```

---

## 2. Dependências do Gradle

No arquivo `app/build.gradle.kts` (ou `app/build.gradle`), adicione as bibliotecas do **Retrofit**, **OkHttp** e **Gson**:

```kotlin
dependencies {
    // Retrofit & Conversor Gson
    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.retrofit2:converter-gson:2.11.0")

    // OkHttp & Interceptor de Logs (essencial para debugar chamadas no Logcat)
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")

    // Coroutines para chamadas assíncronas
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.1")
    implementation("androidx.lifecycle:lifecycle-viewmodel-ktx:2.8.5")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.5")
}
```

---

## 3. Configuração de URL Base (`NetworkConstants.kt`)

No emulador Android oficial, `localhost` ou `127.0.0.1` aponta para o próprio celular virtual. Para acessar a sua máquina de desenvolvimento (onde a API está rodando), utiliza-se o IP especial **`10.0.2.2`**.

Crie o arquivo `data/remote/NetworkConstants.kt`:

```kotlin
package com.inovagab.data.remote

object NetworkConstants {
    // Para Emulador Android Studio:
    const val BASE_URL = "http://10.0.2.2:5000/api/"

    // Para Celular Físico conectado no mesmo Wi-Fi do computador:
    // (Substitua pelo IPv4 do seu computador via 'ipconfig' no terminal)
    // const val BASE_URL = "http://192.168.1.15:5000/api/"
}
```

---

## 4. Modelos de Dados (DTOs / Data Classes)

Crie o pacote `data/model/` e adicione as Data Classes exatas esperadas pela API:

### 4.1 Autenticação (`AuthModels.kt`)
```kotlin
package com.inovagab.data.model

import com.google.gson.annotations.SerializedName

data class RegisterRequest(
    @SerializedName("nome") val nome: String,
    @SerializedName("email") val email: String,
    @SerializedName("password") val password: String,
    @SerializedName("perfil") val perfil: String // "operator", "manager" ou "leader"
)

data class LoginRequest(
    @SerializedName("email") val email: String,
    @SerializedName("password") val password: String
)

data class AuthResponse(
    @SerializedName("token") val token: String,
    @SerializedName("userId") val userId: String,
    @SerializedName("nome") val nome: String,
    @SerializedName("email") val email: String,
    @SerializedName("perfil") val perfil: String,
    @SerializedName("expiresAt") val expiresAt: String
)
```

### 4.2 Estratégias (`StrategyModels.kt`)
```kotlin
package com.inovagab.data.model

import com.google.gson.annotations.SerializedName

data class CreateStrategyRequest(
    @SerializedName("titulo") val titulo: String,
    @SerializedName("descricao") val descricao: String,
    @SerializedName("categoria") val categoria: String,
    @SerializedName("campanha") val campanha: String,
    @SerializedName("status") val status: String = "ativo" // "ativo" | "inativo"
)

data class StrategyResponse(
    @SerializedName("id") val id: String,
    @SerializedName("titulo") val titulo: String,
    @SerializedName("descricao") val descricao: String,
    @SerializedName("categoria") val categoria: String,
    @SerializedName("campanha") val campanha: String,
    @SerializedName("status") val status: String,
    @SerializedName("dataCriacao") val dataCriacao: String
)
```

### 4.3 Ideias (`IdeaModels.kt`)
```kotlin
package com.inovagab.data.model

import com.google.gson.annotations.SerializedName

data class CreateIdeaRequest(
    @SerializedName("titulo") val titulo: String,
    @SerializedName("descricao") val descricao: String,
    @SerializedName("strategyId") val strategyId: String
)

data class UpdateIdeaStatusRequest(
    @SerializedName("status") val status: String, // "em_analise" | "aprovada" | "rejeitada" | "convertida_em_projeto"
    @SerializedName("comentarioGestor") val comentarioGestor: String? = null,
    @SerializedName("strategyId") val strategyId: String? = null
)

data class IdeaResponse(
    @SerializedName("id") val id: String,
    @SerializedName("titulo") val titulo: String,
    @SerializedName("descricao") val descricao: String,
    @SerializedName("status") val status: String,
    @SerializedName("autorId") val autorId: String,
    @SerializedName("autorNome") val autorNome: String,
    @SerializedName("strategyId") val strategyId: String,
    @SerializedName("comentarioGestor") val comentarioGestor: String,
    @SerializedName("pontuacaoIA") val pontuacaoIA: Int,
    @SerializedName("justificativaIA") val justificativaIA: String,
    @SerializedName("dataCriacao") val dataCriacao: String
)
```

### 4.4 Projetos & Dashboard (`ProjectModels.kt`)
```kotlin
package com.inovagab.data.model

import com.google.gson.annotations.SerializedName

data class CreateProjectRequest(
    @SerializedName("nome") val nome: String,
    @SerializedName("descricao") val descricao: String,
    @SerializedName("ideiaId") val ideiaId: String,
    @SerializedName("strategyId") val strategyId: String,
    @SerializedName("investimento") val investimento: Double,
    @SerializedName("retornoEsperado") val retornoEsperado: Double,
    @SerializedName("prazoMeses") val prazoMeses: Int
)

data class UpdateProjectRequest(
    @SerializedName("nome") val nome: String? = null,
    @SerializedName("descricao") val descricao: String? = null,
    @SerializedName("status") val status: String? = null, // "planejamento" | "em_andamento" | "concluido" | "cancelado"
    @SerializedName("investimento") val investimento: Double? = null,
    @SerializedName("retornoEsperado") val retornoEsperado: Double? = null,
    @SerializedName("prazoMeses") val prazoMeses: Int? = null,
    @SerializedName("progresso") val progresso: Int? = null,
    @SerializedName("strategyId") val strategyId: String? = null
)

data class ProjectResponse(
    @SerializedName("id") val id: String,
    @SerializedName("nome") val nome: String,
    @SerializedName("descricao") val descricao: String,
    @SerializedName("ideiaId") val ideiaId: String,
    @SerializedName("strategyId") val strategyId: String,
    @SerializedName("status") val status: String,
    @SerializedName("gestorId") val gestorId: String,
    @SerializedName("investimento") val investimento: Double,
    @SerializedName("retornoEsperado") val retornoEsperado: Double,
    @SerializedName("roiPercentual") val roiPercentual: Double,
    @SerializedName("prazoMeses") val prazoMeses: Int,
    @SerializedName("progresso") val progresso: Int,
    @SerializedName("dataCriacao") val dataCriacao: String
)

data class DashboardResponse(
    @SerializedName("totalIdeias") val totalIdeias: Int,
    @SerializedName("ideiasPendentes") val ideiasPendentes: Int,
    @SerializedName("ideiasAprovadas") val ideiasAprovadas: Int,
    @SerializedName("ideiasRejeitadas") val ideiasRejeitadas: Int,
    @SerializedName("totalProjetos") val totalProjetos: Int,
    @SerializedName("projetosEmAndamento") val projetosEmAndamento: Int,
    @SerializedName("projetosConcluidos") val projetosConcluidos: Int,
    @SerializedName("investimentoTotal") val investimentoTotal: Double,
    @SerializedName("retornoTotal") val retornoTotal: Double,
    @SerializedName("roiMedioPercentual") val roiMedioPercentual: Double,
    @SerializedName("lucroObtido") val lucroObtido: Double,
    @SerializedName("mediaPrazoMeses") val mediaPrazoMeses: Int,
    @SerializedName("mediaProgresso") val mediaProgresso: Double,
    @SerializedName("totalStrategies") val totalStrategies: Int
)
```

---

## 5. Gerenciador de Sessão (`SessionManager.kt`)

Armazena o token JWT e dados do usuário logado no `SharedPreferences` local do Android:

```kotlin
package com.inovagab.data.local

import android.content.Context
import android.content.SharedPreferences

class SessionManager(context: Context) {
    private val prefs: SharedPreferences =
        context.getSharedPreferences("inovagab_session", Context.MODE_PRIVATE)

    companion object {
        private const val KEY_TOKEN = "jwt_token"
        private const val KEY_USER_ID = "user_id"
        private const val KEY_USER_NAME = "user_name"
        private const val KEY_USER_EMAIL = "user_email"
        private const val KEY_USER_ROLE = "user_role"
    }

    fun saveAuthData(token: String, userId: String, name: String, email: String, role: String) {
        prefs.edit()
            .putString(KEY_TOKEN, token)
            .putString(KEY_USER_ID, userId)
            .putString(KEY_USER_NAME, name)
            .putString(KEY_USER_EMAIL, email)
            .putString(KEY_USER_ROLE, role)
            .apply()
    }

    fun getToken(): String? = prefs.getString(KEY_TOKEN, null)
    fun getUserRole(): String? = prefs.getString(KEY_USER_ROLE, null)
    fun getUserName(): String? = prefs.getString(KEY_USER_NAME, null)
    fun getUserId(): String? = prefs.getString(KEY_USER_ID, null)

    fun clearSession() {
        prefs.edit().clear().apply()
    }

    fun isLoggedIn(): Boolean = !getToken().isNullOrBlank()
}
```

---

## 6. Cliente HTTP com Interceptor JWT Automático (`RetrofitClient.kt`)

Esse componente intercepta **todas** as requisições e adiciona automaticamente o header `Authorization: Bearer <token>` quando o usuário estiver logado.

Crie `data/remote/RetrofitClient.kt`:

```kotlin
package com.inovagab.data.remote

import android.content.Context
import com.inovagab.data.local.SessionManager
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    private var retrofit: Retrofit? = null

    fun getInstance(context: Context): Retrofit {
        if (retrofit == null) {
            val sessionManager = SessionManager(context.applicationContext)

            // Interceptor para adicionar o Token JWT em todas as requisições
            val authInterceptor = Interceptor { chain ->
                val original = chain.request()
                val requestBuilder = original.newBuilder()

                sessionManager.getToken()?.let { token ->
                    requestBuilder.addHeader("Authorization", "Bearer $token")
                }

                chain.proceed(requestBuilder.build())
            }

            // Interceptor para log detalhado no Logcat
            val loggingInterceptor = HttpLoggingInterceptor().apply {
                level = HttpLoggingInterceptor.Level.BODY
            }

            val okHttpClient = OkHttpClient.Builder()
                .addInterceptor(authInterceptor)
                .addInterceptor(loggingInterceptor)
                .connectTimeout(30, TimeUnit.SECONDS)
                .readTimeout(30, TimeUnit.SECONDS)
                .writeTimeout(30, TimeUnit.SECONDS)
                .build()

            retrofit = Retrofit.Builder()
                .baseUrl(NetworkConstants.BASE_URL)
                .client(okHttpClient)
                .addConverterFactory(GsonConverterFactory.create())
                .build()
        }
        return retrofit!!
    }

    fun getApiService(context: Context): InovaGabApi {
        return getInstance(context).create(InovaGabApi::class.java)
    }
}
```

---

## 7. Interface Completa de Endpoints (`InovaGabApi.kt`)

Crie `data/remote/InovaGabApi.kt` com todos os endpoints da API:

```kotlin
package com.inovagab.data.remote

import com.inovagab.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface InovaGabApi {

    // ─── 1. Autenticação ──────────────────────────────────────────────────────
    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>

    // ─── 2. Estratégias ───────────────────────────────────────────────────────
    @GET("strategies")
    suspend fun getStrategies(): Response<List<StrategyResponse>>

    @GET("strategies/active")
    suspend fun getActiveStrategies(): Response<List<StrategyResponse>>

    @GET("strategies/{id}")
    suspend fun getStrategyById(@Path("id") id: String): Response<StrategyResponse>

    @POST("strategies")
    suspend fun createStrategy(@Body request: CreateStrategyRequest): Response<StrategyResponse>

    @PUT("strategies/{id}")
    suspend fun updateStrategy(@Path("id") id: String, @Body request: CreateStrategyRequest): Response<StrategyResponse>

    @DELETE("strategies/{id}")
    suspend fun deleteStrategy(@Path("id") id: String): Response<Unit>

    // ─── 3. Ideias ────────────────────────────────────────────────────────────
    @POST("ideas")
    suspend fun createIdea(@Body request: CreateIdeaRequest): Response<IdeaResponse>

    @GET("ideas/my")
    suspend fun getMyIdeas(): Response<List<IdeaResponse>>

    @GET("ideas")
    suspend fun getAllIdeas(): Response<List<IdeaResponse>>

    @GET("ideas/{id}")
    suspend fun getIdeaById(@Path("id") id: String): Response<IdeaResponse>

    @GET("ideas/status/{status}")
    suspend fun getIdeasByStatus(@Path("status") status: String): Response<List<IdeaResponse>>

    @PATCH("ideas/{id}/status")
    suspend fun updateIdeaStatus(@Path("id") id: String, @Body request: UpdateIdeaStatusRequest): Response<IdeaResponse>

    @DELETE("ideas/{id}")
    suspend fun deleteIdea(@Path("id") id: String): Response<Unit>

    // ─── 4. Projetos & Dashboard ──────────────────────────────────────────────
    @GET("projects/dashboard")
    suspend fun getDashboard(): Response<DashboardResponse>

    @GET("projects")
    suspend fun getAllProjects(): Response<List<ProjectResponse>>

    @GET("projects/my")
    suspend fun getMyProjects(): Response<List<ProjectResponse>>

    @GET("projects/{id}")
    suspend fun getProjectById(@Path("id") id: String): Response<ProjectResponse>

    @GET("projects/status/{status}")
    suspend fun getProjectsByStatus(@Path("status") status: String): Response<List<ProjectResponse>>

    @POST("projects")
    suspend fun createProject(@Body request: CreateProjectRequest): Response<ProjectResponse>

    @PUT("projects/{id}")
    suspend fun updateProject(@Path("id") id: String, @Body request: UpdateProjectRequest): Response<ProjectResponse>

    @DELETE("projects/{id}")
    suspend fun deleteProject(@Path("id") id: String): Response<Unit>
}
```

---

## 8. Exemplos Práticos de Uso no Kotlin

### Exemplo 1: Login e Salvar Sessão
```kotlin
class LoginViewModel(private val api: InovaGabApi, private val session: SessionManager) : ViewModel() {

    fun performLogin(email: String, pass: String, onSuccess: () -> Unit, onError: (String) -> Unit) {
        viewModelScope.launch {
            try {
                val response = api.login(LoginRequest(email, pass))
                if (response.isSuccessful && response.body() != null) {
                    val auth = response.body()!!
                    // Salva sessão localmente
                    session.saveAuthData(
                        token = auth.token,
                        userId = auth.userId,
                        name = auth.nome,
                        email = auth.email,
                        role = auth.perfil
                    )
                    onSuccess()
                } else {
                    onError("Credenciais inválidas ou erro no servidor (${response.code()})")
                }
            } catch (e: Exception) {
                onError("Falha na conexão: ${e.localizedMessage}")
            }
        }
    }
}
```

### Exemplo 2: Operador Criando Ideia e Carregando a Pontuação da IA
```kotlin
fun submitIdea(titulo: String, descricao: String, strategyId: String) {
    viewModelScope.launch {
        try {
            val response = api.createIdea(CreateIdeaRequest(titulo, descricao, strategyId))
            if (response.isSuccessful && response.body() != null) {
                val idea = response.body()!!
                Log.d("InovaGAB", "Ideia criada com ID: ${idea.id}")

                // A pontuação da IA roda em background no servidor.
                // Após 2 segundos, atualizamos para pegar a pontuação e justificativa do Gemini:
                kotlinx.coroutines.delay(2000)
                val updated = api.getIdeaById(idea.id)
                if (updated.isSuccessful) {
                    Log.d("InovaGAB", "Pontuação IA: ${updated.body()?.pontuacaoIA}")
                    Log.d("InovaGAB", "Justificativa: ${updated.body()?.justificativaIA}")
                }
            }
        } catch (e: Exception) {
            Log.e("InovaGAB", "Erro ao criar ideia", e)
        }
    }
}
```

---

## 9. Checklist "À Prova de Falhas" (Troubleshooting)

| Sintoma / Erro | Causa Mais Provável | Como Resolver |
|---|---|---|
| `CLEARTEXT communication to 10.0.2.2 not permitted by network security policy` | O Android bloqueia conexões HTTP sem SSL por padrão. | Adicione `android:usesCleartextTraffic="true"` na tag `<application>` do `AndroidManifest.xml`. |
| `ConnectException: Failed to connect to /10.0.2.2:5000` | A API backend não está rodando no computador ou a porta mudou. | Certifique-se de que a API está rodando no terminal com `dotnet run` e escutando em `http://0.0.0.0:5000` ou `localhost:5000`. |
| `HTTP 401 Unauthorized` | Token JWT ausente ou expirado (validade de 24h). | Verifique se o `SessionManager` salvou o token e se o `AuthInterceptor` está adicionando o cabeçalho `Authorization: Bearer <token>`. |
| `HTTP 403 Forbidden` | Perfil do usuário logado não tem permissão para a rota. | Consulte a matriz de perfis: Operador só cria ideias; Gestor aprova ideias e cria projetos; Líder cria estratégias e vê dashboard. |
| `HTTP 404 Not Found` | URL base incorreta. | Garanta que a `BASE_URL` termine com `/api/` (ex: `http://10.0.2.2:5000/api/`). |
| Celular Físico não conecta | Celular não alcança `10.0.2.2`. | `10.0.2.2` só funciona no emulador. No celular físico via Wi-Fi, use o IPv4 da sua máquina (ex: `http://192.168.1.XX:5000/api/`). |
