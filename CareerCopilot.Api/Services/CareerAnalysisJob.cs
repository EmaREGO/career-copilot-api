using CareerCopilot.Api.Data;
using Hangfire;

namespace CareerCopilot.Api.Services
{
    public class CareerAnalysisJob
    {
        private readonly ApplicationDbContext _db;
        private readonly IPdfExtractionService _pdfService;
        private readonly IScraperService _scraper;
        private readonly ILlmService _llmService;

        public CareerAnalysisJob(ApplicationDbContext db, IPdfExtractionService pdfService, IScraperService scraper, ILlmService llmService)
        {
            _db = db; 
            _pdfService = pdfService; 
            _scraper = scraper; 
            _llmService = llmService;
        }

        public async Task RunAnalysis(int evaluationId, string pdfPath, string jobUrl)
        {
            var eval = await _db.Evaluations.FindAsync(evaluationId);
            if (eval == null) return;

            eval.Status = "Processing";
            await _db.SaveChangesAsync();

            // Extraer CV (pasar texto directamente si ya se tiene)
            // Para el MVP, leer el archivo localmente
            using var stream = System.IO.File.OpenRead(pdfPath);
            var resumeText = await _pdfService.ExtractTextAsync(stream);

            // Scraper vacante
            var jobText = await _scraper.ScrapeJobDescriptionAsync(jobUrl);

            // IA
            var resultJson = await _llmService.AnalyzeMatchAsync(resumeText, jobText);

            // Guardar
            eval.ResultJson = resultJson;
            eval.Status = "Completed";
            eval.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
