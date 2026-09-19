using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InovaGAB.API.DTOs;
using InovaGAB.API.Models;
using InovaGAB.API.Settings;
using Microsoft.Extensions.Options;

namespace InovaGAB.API.Services;

public interface IGeminiService
{
    Task<GeminiScoreResponse> ScoreIdeaAsync(Idea idea, Strategy strategy);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GeminiScoreResponse> ScoreIdeaAsync(Idea idea, Strategy strategy)
    {
        var prompt = BuildScoringPrompt(idea, strategy);
        var url = _settings.ApiUrl;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            }
        };

        try
        {
            _logger.LogInformation("Chamando Gemini API: {Url}", url);

            HttpResponseMessage response = null!;
            const int maxRetries = 2;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
                httpRequest.Content = JsonContent.Create(requestBody);

                response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                    break;

                var statusCode = (int)response.StatusCode;
                if ((statusCode == 503 || statusCode == 429) && attempt < maxRetries)
                {
                    _logger.LogWarning("Gemini retornou {Status} na tentativa {Attempt}. Aguardando 2s antes de tentar novamente...", statusCode, attempt);
                    await Task.Delay(2000);
                    continue;
                }

                break;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API retornou {Status}. Body: {Body}", (int)response.StatusCode, errorBody);
                return new GeminiScoreResponse(0, $"IA indisponível (HTTP {(int)response.StatusCode}).");
            }

            var rawContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Gemini raw response: {Raw}", rawContent);
            return ParseGeminiResponse(rawContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar Google Gemini API para ideia {IdeaId}", idea.Id);
            return new GeminiScoreResponse(0, "Pontuação não disponível (erro de integração com IA).");
        }
    }

    private static string BuildScoringPrompt(Idea idea, Strategy strategy)
    {
        var jsonExample = "{\n  \"pontuacao\": <número inteiro de 0 a 100>,\n  \"justificativa\": \"<texto em português, máximo 300 caracteres>\"\n}";

        return
            "Você é um especialista em inovação corporativa para o setor de transporte e logística.\n\n" +
            "Avalie a seguinte ideia de inovação submetida por um colaborador e retorne uma pontuação estruturada em formato JSON.\n\n" +
            "=== ESTRATÉGIA CORPORATIVA VIGENTE ===\n" +
            $"Título: {strategy.Titulo}\n" +
            $"Categoria: {strategy.Categoria}\n" +
            $"Campanha: {strategy.Campanha}\n" +
            $"Descrição: {strategy.Descricao}\n\n" +
            "=== IDEIA SUBMETIDA ===\n" +
            $"Título: {idea.Titulo}\n" +
            $"Descrição: {idea.Descricao}\n\n" +
            "=== CRITÉRIOS DE AVALIAÇÃO (peso igual) ===\n" +
            "1. Alinhamento Estratégico (0-20): Quão bem a ideia se alinha à estratégia corporativa vigente?\n" +
            "2. Impacto no Negócio (0-20): Potencial de redução de custos, ganho de eficiência ou aumento de receita?\n" +
            "3. Viabilidade (0-20): Quão realizável é a ideia com os recursos típicos de uma empresa de logística?\n" +
            "4. Inovação (0-20): O quanto a ideia é original e diferenciada?\n" +
            "5. Clareza (0-20): A descrição é clara, objetiva e permite avaliação detalhada?\n\n" +
            "=== RESPOSTA ESPERADA (JSON ESTRITAMENTE NESTE FORMATO) ===\n" +
            jsonExample + "\n\n" +
            "Retorne APENAS o JSON. Não inclua texto antes ou depois.";
    }

    private GeminiScoreResponse ParseGeminiResponse(string rawContent)
    {
        try
        {
            var jsonToParse = ExtractJsonText(rawContent);
            return ParseScoreJson(jsonToParse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar resposta da IA. RawContent: {Raw}", rawContent);
            return new GeminiScoreResponse(0, "Não foi possível processar a resposta da IA.");
        }
    }

    private static string ExtractJsonText(string rawContent)
    {
        using var doc = JsonDocument.Parse(rawContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("candidates", out var candidates)
            && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var content)
                && content.TryGetProperty("parts", out var parts))
            {
                var sb = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textEl))
                    {
                        sb.Append(textEl.GetString());
                    }
                }

                var combinedText = sb.ToString();
                if (!string.IsNullOrWhiteSpace(combinedText))
                {
                    return StripMarkdownAndExtractJson(combinedText);
                }
            }
        }

        if (root.TryGetProperty("pontuacao", out _))
            return rawContent;

        return StripMarkdownAndExtractJson(rawContent);
    }

    private static string StripMarkdownAndExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var startIndex = text.IndexOf('{');
        var endIndex = text.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            return text.Substring(startIndex, endIndex - startIndex + 1);
        }

        return text.Trim();
    }

    private static GeminiScoreResponse ParseScoreJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;

        int pontuacao = 0;
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name.Equals("pontuacao", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Equals("score", StringComparison.OrdinalIgnoreCase))
            {
                pontuacao = prop.Value.ValueKind == JsonValueKind.Number
                    ? (int)Math.Round(prop.Value.GetDouble())
                    : int.TryParse(prop.Value.GetString(), out var val) ? val : 0;
                break;
            }
        }

        string justificativa = string.Empty;
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Name.Equals("justificativa", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Equals("justification", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Equals("reason", StringComparison.OrdinalIgnoreCase))
            {
                justificativa = prop.Value.GetString() ?? string.Empty;
                break;
            }
        }

        pontuacao = Math.Clamp(pontuacao, 0, 100);
        return new GeminiScoreResponse(pontuacao, justificativa);
    }
}
