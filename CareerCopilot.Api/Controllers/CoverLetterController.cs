using CareerCopilot.Api.Data;
using CareerCopilot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoverLetterController : ControllerBase
    {
        private readonly IPdfExtractionService _pdfService;
        private readonly IScraperService _scraperService;
        private readonly ILlmService _llmService;
        private readonly ApplicationDbContext _db;

        public CoverLetterController(IPdfExtractionService pdfService, IScraperService scraperService, ILlmService llmService)
        {
            _pdfService = pdfService;
            _scraperService = scraperService;
            _llmService = llmService;
        }

        [HttpPost("generate/{evaluationId}")]
        public async Task<IActionResult> GenerateLetter(int evaluationId, IFormFile file, [FromQuery] string jobUrl)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido.");

            if (string.IsNullOrWhiteSpace(jobUrl))
                return BadRequest("jobUrl es requerido.");

            var eval = await _db.Evaluations.FindAsync(evaluationId);
            if (eval == null)
                return NotFound("Evaluación no encontrada.");

            try
            {
                using var stream = file.OpenReadStream();
                var resumeText = await _pdfService.ExtractTextAsync(stream);
                var jobText = await _scraperService.ScrapeJobDescriptionAsync(jobUrl);

                var coverLetter = await _llmService.GenerateCoverLetterAsync(resumeText, jobText);

                eval.CoverLetter = coverLetter;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    Message = "Carta generada y guardada con éxito",
                    CoverLetter = coverLetter
                });
            }
            catch
            {
                return StatusCode(500, "Ocurrió un error al generar la carta.");
            }
        }
    }
}