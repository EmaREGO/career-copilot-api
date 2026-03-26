// LlmService.cs
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
        private readonly string _apiKey;
        private readonly string _modelUrl;

        // Modelos de fallback en orden de preferencia
        private static readonly string[] FallbackModels = new[]
        {
            "gemini-2.0-flash",
            "gemini-1.5-flash",
            "gemini-1.5-pro"
        };

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiAI:ApiKey"]
                ?? throw new InvalidOperationException("Falta GeminiAI:ApiKey en configuración.");

            var model = configuration["GeminiAI:Model"] ?? FallbackModels[0];
            _modelUrl = BuildModelUrl(model);

            Console.WriteLine($"[LlmService] Inicializado con modelo: {model}");
            Console.WriteLine($"[LlmService] URL base: {_modelUrl.Split('?')[0]}");
        }

        private static string BuildModelUrl(string model)
        {
            return $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        }

        public async Task<string> AnalyzeMatchAsync(string resumeText, string jobText)
        {
            Console.WriteLine("[LlmService] Iniciando AnalyzeMatchAsync...");

            var systemPrompt = @"Actúa como un Arquitecto de Software Senior y un Reclutador Técnico experto en sistemas ATS.
                Tu objetivo es analizar un CV contra una descripción de vacante de forma crítica y objetiva.

                REGLAS DE EVALUACIÓN:
                1. Red Flags: Detecta salarios bajos, tecnologías obsoletas, cultura laboral negativa o requisitos absurdos.
                2. Complexity Score (1-10): Evalúa la complejidad real de los proyectos. Premia arquitecturas, despliegues e impacto medible.
                3. ATS Keywords: Identifica palabras clave exactas de la vacante que falten en el CV.
                4. Sugerencias de Mejora: Da instrucciones claras y accionables.

                DEVUELVE ÚNICAMENTE UN JSON VÁLIDO CON ESTA ESTRUCTURA EXACTA, SIN TEXTO ADICIONAL NI MARKDOWN:
                {
                    ""match_percentage"": 85,
                    ""complexity_score"": 8,
                    ""red_flags"": [
                        { ""flag"": ""Bajo salario"", ""reason"": ""$8,000 MXN para un Senior es irreal."", ""severity"": ""High"" }
                    ],
                    ""strengths"": [""Dominio avanzado de C# y SQL Server""],
                    ""missing_skills"": [""Docker"", ""RPA""],
                    ""ats_keywords_to_add"": [""Automatización de procesos"", ""Microservicios""],
                    ""cv_improvement_suggestions"": [
                        { ""section"": ""Experiencia"", ""suggestion"": ""Especifica métricas de impacto."" }
                    ]
                }";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $"{systemPrompt}\n\n--- CV DEL CANDIDATO ---\n{resumeText}\n\n--- DESCRIPCIÓN DE LA VACANTE ---\n{jobText}"
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.2
                }
            };

            return await SendRequestAsync(_modelUrl, payload);
        }

        public async Task<string> GenerateCoverLetterAsync(string resumeText, string jobText)
        {
            Console.WriteLine("[LlmService] Iniciando GenerateCoverLetterAsync...");

            var systemPrompt = @"Eres un Copywriter experto y Career Coach. Escribe una Carta de Presentación persuasiva, humana y directa.

                REGLAS ESTRICTAS:
                1. CERO CLICHÉS: Nada de 'Por la presente me dirijo...'. Empieza con un gancho fuerte.
                2. Tono: Profesional, apasionado, seguro de sí mismo.
                3. Estructura: Máximo 3-4 párrafos cortos.
                4. Usa 1-2 logros específicos del CV que hagan match con la vacante.
                5. Cierra con un Call to Action invitando a una entrevista o llamada técnica.

                Devuelve ÚNICAMENTE el texto de la carta. Sin JSON ni comentarios.";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = $"{systemPrompt}\n\n--- CV DEL CANDIDATO ---\n{resumeText}\n\n--- DESCRIPCIÓN DE LA VACANTE ---\n{jobText}"
                            }
                        }
                    }
                },
                generationConfig = new { temperature = 0.7 }
            };

            return await SendRequestAsync(_modelUrl, payload);
        }

        private async Task<string> SendRequestAsync(string modelUrl, object payload)
        {
            var fullUrl = $"{modelUrl}?key={_apiKey}";
            var jsonPayload = JsonSerializer.Serialize(payload);

            Console.WriteLine($"[LlmService] POST → {modelUrl.Split('?')[0]}");

            using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[LlmService] Error de red: {ex.Message}");
                throw new Exception($"Error de conexión con Gemini: {ex.Message}", ex);
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[LlmService] Error HTTP {(int)response.StatusCode}: {responseBody}");
                throw new Exception(
                    $"Gemini API Error {(int)response.StatusCode} ({response.StatusCode}): " +
                    $"{ExtractGeminiErrorMessage(responseBody)}"
                );
            }

            Console.WriteLine($"[LlmService] Respuesta OK ({(int)response.StatusCode})");

            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.GetArrayLength() == 0)
            {
                Console.WriteLine($"[LlmService] Respuesta sin candidates: {responseBody}");
                throw new Exception("Gemini no devolvió candidatos en la respuesta.");
            }

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            return text.Replace("```json", "").Replace("```", "").Trim();
        }

        private static string ExtractGeminiErrorMessage(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? responseBody;
                }
            }
            catch { /* si el body no es JSON válido, devolvemos el body crudo */ }

            return responseBody;
        }
    }
}