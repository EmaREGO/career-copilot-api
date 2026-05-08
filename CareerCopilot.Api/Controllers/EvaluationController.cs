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
        private readonly IPdfExtractionService _pdfService; 

        public EvaluationController(ApplicationDbContext db, IPdfExtractionService pdfService)
        {
            _db = db;
            _pdfService = pdfService;
        }


        [HttpPost("analyze")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Analyze([FromForm] AnalyzeRequest request) 
        {
            // Ahora accedemos a los datos a través de 'request'
            if (request.File == null || string.IsNullOrEmpty(request.JobUrl))
                return BadRequest("Requeridos archivo y URL.");

            using var stream = request.File.OpenReadStream();
            string resumeText = await _pdfService.ExtractTextAsync(stream);

            var eval = new Evaluation
            {
                VacancyUrl = request.JobUrl,
                Status = "Pending",
                CandidateProfileId = 1,
                ResultJson = "{}"
            };

            _db.Evaluations.Add(eval);
            await _db.SaveChangesAsync();

            BackgroundJob.Enqueue<CareerAnalysisJob>(x => x.RunAnalysis(eval.Id, resumeText, request.JobUrl)); 

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

public class AnalyzeRequest
{
    public required IFormFile File { get; set; }
    public required string JobUrl { get; set; }
}