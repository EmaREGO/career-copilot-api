using System.IO;
using System.Text.Json;
using CareerCopilot.Api.Data;
using CareerCopilot.Api.Models;
using CareerCopilot.Api.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvaluationController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPdfExtractionService _pdfService; // <--- Inyectado correctamente

        public EvaluationController(ApplicationDbContext db, IPdfExtractionService pdfService)
        {
            _db = db;
            _pdfService = pdfService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromForm] IFormFile file, [FromQuery] string jobUrl)
        {
            if (file == null || string.IsNullOrEmpty(jobUrl))
                return BadRequest("Requeridos archivo y URL.");

            // Extraer el texto usando el campo inyectado '_pdfService'
            using var stream = file.OpenReadStream();
            string resumeText = await _pdfService.ExtractTextAsync(stream);

            var eval = new Evaluation
            {
                VacancyUrl = jobUrl,
                Status = "Pending",
                CandidateProfileId = 1,
                ResultJson = "{}"
            };

            _db.Evaluations.Add(eval);
            await _db.SaveChangesAsync();

            // Encolar el trabajo en Hangfire
            BackgroundJob.Enqueue<CareerAnalysisJob>(x => x.RunAnalysis(eval.Id, resumeText, jobUrl));

            return Ok(new { Message = "Análisis iniciado.", EvaluationId = eval.Id });
        }


        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStatus(int id)
        {
            var eval = await _db.Evaluations.FindAsync(id);
            if (eval == null) return NotFound();

            JsonElement analysisResult = JsonDocument.Parse(
                !string.IsNullOrEmpty(eval.ResultJson) && eval.ResultJson != "{}"
                ? eval.ResultJson
                : "{}"
            ).RootElement;

            return Ok(new
            {
                eval.Id,
                eval.Status,
                eval.CreatedAt,
                eval.CompletedAt,
                eval.GlobalMatchPercentage,
                Analysis = analysisResult
            });
        }
    }
}