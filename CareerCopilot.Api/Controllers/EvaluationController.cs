// EvaluationController.cs
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

        // Inyectar el DbContext a través del constructor
        public EvaluationController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("analyze")]
        public IActionResult Analyze([FromForm] IFormFile file, [FromQuery] string jobUrl)
        {
            if (file == null || string.IsNullOrEmpty(jobUrl))
                return BadRequest("Archivo y URL de vacante son requeridos.");

            // Validar que exista al menos un perfil para evitar error de FK
            //var profileExists = _db.Profiles.Any(p => p.Id == 1);
            //if (!profileExists)
           // {
            //    return BadRequest("No existe un perfil con ID 1 en la base de datos. Por favor, crea uno primero.");
           // }

            // Preparar ruta temporal
            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");

            using (var stream = System.IO.File.Create(filePath))
            {
                file.CopyTo(stream);
            }

            var eval = new Evaluation
            {
                VacancyUrl = jobUrl,
                Status = "Pending",
                CandidateProfileId = 1
            };

            _db.Evaluations.Add(eval);
            _db.SaveChanges();

            // Encolar
            BackgroundJob.Enqueue<CareerAnalysisJob>(x => x.RunAnalysis(eval.Id, filePath, jobUrl));

            return Ok(new
            {
                Message = "Análisis iniciado. Monitorea en /hangfire",
                EvaluationId = eval.Id
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStatus(int id)
        {
            var eval = await _db.Evaluations.FindAsync(id);
            if (eval == null) return NotFound();

            // Si el análisis terminó, deserializamos el ResultJson para que se vea bien en Swagger
            object? analysisResult = null;
            if (!string.IsNullOrEmpty(eval.ResultJson))
            {
                analysisResult = JsonSerializer.Deserialize<object>(eval.ResultJson);
            }

            return Ok(new
            {
                eval.Id,
                eval.Status,
                eval.CreatedAt,
                eval.CompletedAt,
                Analysis = analysisResult
            });
        }
    }
}