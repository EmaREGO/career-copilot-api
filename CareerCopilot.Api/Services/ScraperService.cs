using Microsoft.Playwright;

namespace CareerCopilot.Api.Services
{
    public interface IScraperService
    {
        Task<string> ScrapeJobDescriptionAsync(string url);
    }

    public class ScraperService : IScraperService
    {
        public async Task<string> ScrapeJobDescriptionAsync(string url)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" } // <--- AGREGA ESTO
            });

            // Aniadir un contexto con un User-Agent de un chrome real
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
            });


            var page = await context.NewPageAsync();

            // Navegar a la URL de la vacante
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });

            var cleanText = await page.EvaluateAsync<string>(@"() => {
                // 1. Eliminar etiquetas ruidosas (menús, footers, scripts, estilos, botones)
                const selectorsToRemove = ['nav', 'footer', 'header', 'aside', 'script', 'style', 'noscript', 'button', 'svg'];
                selectorsToRemove.forEach(selector => {
                    document.querySelectorAll(selector).forEach(el => el.remove());
                });
        
                // 2. Intentar buscar el contenedor principal (específico para LinkedIn, Indeed o estándar)
                // Si no encuentra los específicos, cae al 'main' o finalmente al 'body' ya limpio.
                const mainContent = document.querySelector('.jobs-description') 
                                 || document.querySelector('#job-details') 
                                 || document.querySelector('main') 
                                 || document.body;
        
                // 3. Devolver solo el texto limpio y sin espacios dobles
                return mainContent.innerText.replace(/\s+/g, ' ').trim();
            }");


            return cleanText;

            
        }
    }
}
