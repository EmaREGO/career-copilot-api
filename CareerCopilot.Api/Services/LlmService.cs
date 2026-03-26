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
        private readonly string _model;

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["GeminiAI:ApiKey"] ?? throw new Exception("Falta API Key");

            _model = _configuration["GeminiAI:Model"] ?? "gemini-1.5-flash-latest";
            _baseUrl = "https://generativelanguage.googleapis.com/v1beta";
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            Console.WriteLine($"[DEBUG] Enviando a Gemini ({_model})...");

            var systemPrompt = @"Actúa como Arquitecto de Software y Reclutador IT. 
                Analiza el CV contra la vacante.
                REGLAS: Detecta Red Flags, Complexity Score (1-10), ATS Keywords y Sugerencias.
                DEVUELVE ÚNICAMENTE UN JSON VÁLIDO SIN MARKDOWN.";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2
                }
            };

            var fullUrl = $"{_baseUrl}/models/{_model}:generateContent?key={_apiKey}";

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(fullUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[DEBUG] ERROR DE GOOGLE: {responseBody}");
                throw new Exception($"Google Error {response.StatusCode}");
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
            var fullUrl = $"{_baseUrl}/models/{_model}:generateContent?key={_apiKey}";
            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"Escribe una carta de presentación breve para este CV:\n{resumeText}\ny esta vacante:\n{jobText}" } } }
                },
                generationConfig = new { temperature = 0.7 }
            };

            var response = await _httpClient.PostAsync(fullUrl, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}