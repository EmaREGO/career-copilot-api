using UglyToad.PdfPig;

namespace CareerCopilot.Api.Services
{
    // Definimor la Interfaz para inyectarla después
    public interface IPdfExtractionService
    {
        Task<string> ExtractTextAsync(Stream pdfStream);
    }

    // Implementamos el Servicio
    public class PdfExtractionService : IPdfExtractionService
    {
        public async Task<string> ExtractTextAsync(Stream pdfStream)
        {
            // Uso un Task.Run porque PdfPig es síncrono por defecto y no se desea bloquear el hilo
            return await Task.Run(() =>
            {
                var fullText = new System.Text.StringBuilder();

                try
                {
                    // Abre el documento PDF desde el Stream que manda el usuario
                    using (PdfDocument document = PdfDocument.Open(pdfStream))
                    {
                        foreach (var page in document.GetPages())
                        {
                            // Extraer el texto de cada página y se concateja
                            fullText.AppendLine(page.Text);
                        }
                    }
                    return fullText.ToString();
                }
                catch (Exception ex)
                {
                    // En un entorno real, aquí usaría un ILogger
                    throw new Exception($"Error al procesar el PDF: {ex.Message}");
                }
            });
        }
    }
}