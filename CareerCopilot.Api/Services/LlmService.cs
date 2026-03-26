using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CareerCopilot.Api.Services
{
    public interface ILlmService
    {
        Task<string> AnalyzeMatchAsync(string resumeText, string jobText);
        Task<string> GenerateCoverLetterAsync(string resumeText, string jobText);
    }

    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _apiKey = _configuration["GeminiAI:ApiKey"] ?? throw new Exception("API Key no configurada.");
            _apiUrl = _configuration["GeminiAI:BaseUrl"] ?? throw new Exception("Base URL no configurada.");
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            // PROMPT AJUSTADO
            var systemPrompt = @"Actúa como un Arquitecto de Software Senior y Reclutador experto en ATS. 
                Analiza el CV contra la vacante de forma crítica.
                
                REGLAS:
                1. Identifica Red Flags, Complexity Score (1-10), Strengths, Missing Skills y ATS Keywords.
                2. Sugiere mejoras específicas.
                
                DEVUELVE ÚNICAMENTE UN JSON COMPACTO EN UNA SOLA LÍNEA, SIN SALTOS DE LÍNEA REALES NI MARKDOWN:
                {
                    ""match_percentage"": 85,
                    ""complexity_score"": 8,
                    ""red_flags"": [{ ""flag"": """", ""reason"": """", ""severity"": """" }],
                    ""strengths"": [],
                    ""missing_skills"": [],
                    ""ats_keywords_to_add"": [],
                    ""cv_improvement_suggestions"": [{ ""section"": """", ""suggestion"": """" }]
                }";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0.2
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error de Gemini API ({response.StatusCode}): {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var rawText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";

                // LIMPIEZA SEGURA
                rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

                return rawText;
            }

            throw new Exception("Gemini no devolvió resultados válidos.");
        }

        public async Task<string> GenerateCoverLetterAsync(string resumeText, string jobText)
        {
            var systemPrompt = @"Eres un Copywriter experto. Escribe una Carta de Presentación persuasiva, humana y directa. Máximo 3 párrafos. Cero clichés. Devuelve solo el texto plano.";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new 
                {
                    response_mime_type = "application/json",
                    temperature = 0.7               
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error de Gemini API: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
            }

            return "No se pudo generar la carta.";
        }
    }
}