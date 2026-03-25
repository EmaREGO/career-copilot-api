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

            // Leer valores directamente del archivo de configuracion
            _apiKey = _configuration["GeminiAI:ApiKey"] ?? throw new Exception("API Key no configurada.");
            _apiUrl = _configuration["GeminiAI:BaseUrl"] ?? throw new Exception("Base URL no configurada.");
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            // EL PROMPT MAESTRO
            var systemPrompt = @"Actúa como un Arquitecto de Software Senior y un Reclutador Técnico experto en sistemas ATS (Applicant Tracking Systems).
                Tu objetivo es analizar un CV contra una descripción de vacante, siendo extremadamente analítico y crítico.

                REGLAS DE EVALUACIÓN:
                1. Red Flags: Detecta salarios por debajo del mercado, tecnologías obsoletas (ej. jQuery, VB6 en roles modernos), mala cultura laboral implícita o requisitos absurdos.
                2. Complexity Score (1-10): Evalúa la complejidad real de los proyectos. Premia arquitecturas, despliegues e impacto medible sobre simples mantenimientos.
                3. ATS Keywords: Identifica palabras clave exactas de la vacante que falten en el CV.
                4. Sugerencias de Mejora: Da instrucciones claras (ej. 'Cambia la frase X por Y').

                DEVUELVE ÚNICAMENTE UN JSON VÁLIDO CON ESTA ESTRUCTURA EXACTA, SIN TEXTO ADICIONAL NI MARKDOWN:
                {
                    ""match_percentage"": 85,
                    ""complexity_score"": 8,
                    ""red_flags"": [
                        { ""flag"": ""Bajo salario"", ""reason"": ""$8,000 para un Senior es irreal."", ""severity"": ""High"" }
                    ],
                    ""strengths"": [""Dominio avanzado de C# y SQL Server""],
                    ""missing_skills"": [""Docker"", ""RPA""],
                    ""ats_keywords_to_add"": [""Automatización de procesos"", ""Microservicios""],
                    ""cv_improvement_suggestions"": [
                        { ""section"": ""Experiencia"", ""suggestion"": ""En el proyecto AXOMA, especifica qué métrica mejoraste al usar WebSockets (ej. 'Reducción de latencia en un 40%')."" }
                    ]
                }";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\n--- CV DEL CANDIDATO ---\n{resumeText}\n\n--- DESCRIPCIÓN DE LA VACANTE ---\n{jobText}" } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2 // Baja temperatura para respuestas más analíticas y menos creativas
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

                // PROGRAMACIÓN DEFENSIVA: Limpiar markdown si Gemini lo inyecta por error
                rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

                return rawText;
            }

            throw new Exception("Gemini no devolvió resultados válidos.");
        }

        public async Task<string> GenerateCoverLetterAsync(string resumeText, string jobText)
        {
            var systemPrompt = @"Eres un Copywriter experto y Career Coach. Tu objetivo es escribir una Carta de Presentación (Cover Letter) altamente persuasiva, humana y directa.
    
            REGLAS ESTRICTAS:
            1. CERO CLICHÉS: Prohibido usar frases robóticas como 'Por la presente me dirijo a usted para postularme a la vacante...'. Empieza con un gancho fuerte sobre por qué el candidato conecta con el problema que la empresa quiere resolver.
            2. Tono: Profesional, apasionado, humilde pero seguro de sí mismo.
            3. Estructura: Máximo 3 o 4 párrafos cortos. 
            4. Contenido: Usa 1 o 2 logros específicos del CV que hagan un 'match' perfecto con los requisitos de la vacante.
            5. Cierre: Termina con un Call to Action (CTA) seguro invitando a una entrevista o llamada técnica.
    
            Devuelve ÚNICAMENTE el texto de la carta de presentación. No agregues comentarios tuyos ni formato JSON.";

            var payload = new
            {
                contents = new[] {
                    new { parts = new[] { new { text = $"{systemPrompt}\n\n--- CV DEL CANDIDATO ---\n{resumeText}\n\n--- DESCRIPCIÓN DE LA VACANTE ---\n{jobText}" } } }
                },
                generationConfig = new
                {
                    temperature = 0.7 // Temperatura más alta = Más creatividad y fluidez humana
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