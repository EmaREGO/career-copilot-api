using CareerCopilot.Api.Data;
using CareerCopilot.Api.Models;
using Hangfire;

namespace CareerCopilot.Api.Services
{
    public class CareerAnalysisJob
    {
        private readonly ApplicationDbContext _db;
        private readonly IScraperService _scraper;
        private readonly ILlmService _llmService;

        public CareerAnalysisJob(ApplicationDbContext db, IScraperService scraper, ILlmService llmService)
        {
            _db = db;
            _scraper = scraper;
            _llmService = llmService;
        }

        public async Task RunAnalysis(int evaluationId, string resumeText, string jobUrl)
        {
            var eval = await _db.Evaluations.FindAsync(evaluationId);
            if (eval == null) return;

            try
            {
                eval.Status = "Processing";
                await _db.SaveChangesAsync();

                // Scraper de la vacante
                var jobText = await _scraper.ScrapeJobDescriptionAsync(jobUrl);

                // Usar directamente el 'resumeText' que llegó por parámetro
                var resultJson = await _llmService.AnalyzeMatchAsync(resumeText, jobText);

                // Guardar resultados
                eval.ResultJson = resultJson;
                eval.Status = "Completed";
                eval.CompletedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Si algo falla, marca la evaluación como fallida
                eval.Status = "Failed";
                // Podría guardar el mensaje de error en ResultJson para saber qué pasó
                eval.ResultJson = $"{{\"error\": \"{ex.Message}\"}}";
                await _db.SaveChangesAsync();

                // Voler a lanzar la excepción para que Hangfire sepa que falló y lo reintente si es necesario
                throw;
            }
        }
    }
}