using CareerCopilot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly IScraperService _scraperService;

        public JobController(IScraperService scraperService)
        {
            _scraperService = scraperService;
        }

        [HttpGet("test-scrape")]
        public async Task<IActionResult> TestScrape([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url)) return BadRequest("URL requerida");

            try
            {
                var content = await _scraperService.ScrapeJobDescriptionAsync(url);
                return Ok(new { Url = url, ContentLength = content.Length, RawContent = content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al scrapear: {ex.Message}");
            }
        }
    }
}