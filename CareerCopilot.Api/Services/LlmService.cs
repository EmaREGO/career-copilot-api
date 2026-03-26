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
            var systemPrompt = @"Actúa como Arquitecto de Software Senior. Analiza CV vs Vacante.
                DEVUELVE EXCLUSIVAMENTE UN JSON VÁLIDO. SIN TEXTO ADICIONAL.
                Estructura:
                {
                    ""match_percentage"": 0,
                    ""complexity_score"": 0,
                    ""red_flags"": [],
                    ""strengths"": [],
                    ""missing_skills"": [],
                    ""ats_keywords_to_add"": [],
                    ""cv_improvement_suggestions"": []
                }";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new
                {
                    temperature = 0.1
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error Gemini ({response.StatusCode}): {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var rawText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "{}";
                // Limpieza de Markdown 
                return rawText.Replace("```json", "").Replace("```", "").Trim();
            }
            throw new Exception("Gemini no devolvió resultados.");
        }

        public async Task<string> GenerateCoverLetterAsync(string resumeText, string jobText)
        {
            var systemPrompt = "Eres un experto en reclutamiento. Escribe una carta de presentación persuasiva basada en el CV y la vacante. Devuelve solo el texto de la carta.";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new { temperature = 0.7 }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_apiUrl}?key={_apiKey}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error Gemini: {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";

            return "No se pudo generar la carta.";
        }
    }
}