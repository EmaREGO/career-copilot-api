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

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["GeminiAI:ApiKey"] ?? throw new Exception("API Key faltante.");
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            Console.WriteLine($"[DEBUG] Intentando análisis con Gemini 1.5 Flash...");

            var systemPrompt = "Analiza CV vs Vacante. Devuelve solo un JSON con match_percentage (int) y cv_improvement_suggestions (array).";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                }
            };

            var fullUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(fullUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] Error de Google: {responseBody}");
                throw new Exception($"Google Error: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var rawText = doc.RootElement.GetProperty("candidates")[0]
                                         .GetProperty("content")
                                         .GetProperty("parts")[0]
                                         .GetProperty("text").GetString() ?? "{}";

            return rawText.Replace("```json", "").Replace("```", "").Trim();
        }

        public async Task<string> GenerateCoverLetterAsync(string resumeText, string jobText)
        {
            var fullUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            var payload = new { contents = new[] { new { parts = new[] { new { text = $"Escribe una carta de presentación para este CV:\n{resumeText}\ny vacante:\n{jobText}" } } } } };
            var response = await _httpClient.PostAsync(fullUrl, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}