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
        private readonly string _model;

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["GeminiAI:ApiKey"] ?? throw new Exception("API Key no configurada.");
            _model = _configuration["GeminiAI:Model"] ?? "gemini-2.5-flash";
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            var systemPrompt = @"Actúa como un Consultor de Carrera Senior y Reclutador Estratégico con 20 años de experiencia global. 
            Tu tarea es analizar el CV de un candidato frente a una vacante de CUALQUIER industria (Tech, Finanzas, Salud, Ingeniería, etc.).

            Sigue este protocolo:
            1. IDENTIFICACIÓN: Determina la industria y el nivel de seniority de la vacante.
            2. PERSONA: Adopta la mentalidad de un experto técnico en esa área específica.
            3. COMPLEXITY SCORE (Escala 1-10):
               - 1-3: Perfil operativo o junior. Proyectos guiados o académicos sin métricas de impacto claras.
               - 4-6: Perfil mid-level o profesional independiente. Resultados tangibles, autonomía y manejo de herramientas especializadas.
               - 7-8: Perfil Senior o Líder. Impacto directo en negocio (ahorro/ganancia), mentoría y decisiones técnicas complejas.
               - 9-10: Perfil Staff, Director o Experto Único. Transformación organizacional, patentes o arquitecturas de alto impacto.

            Devuelve EXCLUSIVAMENTE un JSON válido (sin bloques markdown):
            {
                ""match_percentage"": 0,
                ""complexity_score"": 0,
                ""detected_industry"": ""Nombre de la industria"",
                ""red_flags"": [
                    { ""flag"": ""Título"", ""reason"": ""Explicación profesional"", ""severity"": ""High|Medium|Low"" }
                ],
                ""strengths"": [""Puntos fuertes del perfil""],
                ""missing_skills"": [""Habilidades críticas ausentes""],
                ""ats_keywords_to_add"": [""Keywords para optimizar el CV""],
                ""cv_improvement_suggestions"": [
                    { ""section"": ""Sección del CV"", ""suggestion"": ""Acción concreta para mejorar"" }
                ]
            }";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\nCV:\n{resumeText}\n\nVACANTE:\n{jobText}" } } }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error Gemini: {response.StatusCode} - {responseBody}");

            using var doc = JsonDocument.Parse(responseBody);
            var rawText = doc.RootElement.GetProperty("candidates")[0]
                                         .GetProperty("content")
                                         .GetProperty("parts")[0]
                                         .GetProperty("text").GetString() ?? "{}";

            return rawText.Trim();
        }

        public async Task<string> GenerateCoverLetterAsync(string resumeText, string jobText)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"Escribe una carta de presentación persuasiva para este CV:\n{resumeText}\ny vacante:\n{jobText}" } } }
                }
            };

            var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
    }
}