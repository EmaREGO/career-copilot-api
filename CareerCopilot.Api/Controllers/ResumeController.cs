using CareerCopilot.Api.Data;
using CareerCopilot.Api.Models;
using CareerCopilot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeController : ControllerBase
    {
        private readonly IPdfExtractionService _pdfExtractionService;
        private readonly ApplicationDbContext _db;

        // Inyectar el servicio creado
        public ResumeController(IPdfExtractionService pdfExtractionService, ApplicationDbContext db)
        {
            _pdfExtractionService = pdfExtractionService;
            _db = db;
        }

        [HttpPost("create-test-profile")]
        public IActionResult CreateTestProfile()
        {
            // 1. Verificar si ya existe el usuario 1
            var existingUser = _db.Users.FirstOrDefault(u => u.Id == 1);
            if (existingUser != null) return Ok("El perfil de prueba ya existe (ID: 1).");

            // 2. Crear Usuario y Perfil vinculados
            var testUser = new User
            {
                Email = "ema.test@example.com",
                PasswordHash = "hashed_password_here"
            };

            var testProfile = new CandidateProfile
            {
                User = testUser,
                ProfessionalTitle = "Software Engineer",
                TotalExperienceYears = 2,
                Summary = "Perfil de prueba para Career Co-pilot"
            };

            _db.Profiles.Add(testProfile);
            _db.SaveChanges();

            return Ok(new { Message = "Perfil e IA inicializados", ProfileId = testProfile.Id });
        }

        [HttpPost("extract-text")]
        public async Task<IActionResult> ExtractTextFromPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Por favor, sube un archivo PDF.");
            }
            if (file.ContentType != "application/pdf")
            {
                return BadRequest("El archivo debe ser un PDF.");
            }

            //Convertir el archivo subido a un Stram y se pasa al servicio
            using var stream = file.OpenReadStream();
            var extractedText = await _pdfExtractionService.ExtractTextAsync(stream);

            // Devolver el texto extraido (prueba de funcionalidad)
            return Ok(new
            {
                FileName = file.FileName,
                FileSize = file.Length,
                Text = extractedText
            });
        }
    }
}
