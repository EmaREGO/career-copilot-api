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
        private readonly string _baseUrl;

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["GeminiAI:ApiKey"] ?? throw new Exception("API Key faltante.");
            _baseUrl = _configuration["GeminiAI:BaseUrl"] ?? "https://generativelanguage.googleapis.com";
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            Console.WriteLine($"[DEBUG] CV Longitud: {resumeText?.Length ?? 0}");
            Console.WriteLine($"[DEBUG] Vacante Longitud: {jobText?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(jobText) || jobText.Length < 50)
            {
                throw new Exception("El Scraper no obtuvo suficiente texto de la vacante.");
            }

            var systemPrompt = "Actúa como reclutador IT. Analiza CV vs Vacante. Devuelve SOLO un JSON válido con: match_percentage (int), complexity_score (int), red_flags (array), strengths (array), missing_skills (array), ats_keywords_to_add (array), cv_improvement_suggestions (array de objetos {section, suggestion}).";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new { temperature = 0.2 }
            };

            var fullUrl = $"{_baseUrl.TrimEnd('/')}/v1/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(fullUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] Error Gemini {response.StatusCode}: {responseBody}");
                throw new Exception($"Error de Gemini API: {response.StatusCode}");
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
            var fullUrl = $"{_baseUrl.TrimEnd('/')}/v1/models/gemini-1.5-flash:generateContent?key={_apiKey}";
            var payload = new { contents = new[] { new { parts = new[] { new { text = $"Escribe una carta de presentación para este CV:\n{resumeText}\ny esta vacante:\n{jobText}" } } } } };

            var response = await _httpClient.PostAsync(fullUrl, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}