namespace CareerCopilot.Api.Models
{ // El corazon de la logica asincrona
    public class Evaluation
    {
        public int Id { get; set; }
        public int CandidateProfileId { get; set; }
        public int? JobVacancyId { get; set; } // Puede ser null inicialmente hasta que el LLM lo procese
        public string VacancyUrl { get; set; } = string.Empty;

        // Control de los Background Jobs (Hangfire)
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

        // Resultados del LLM
        public decimal? GlobalMatchPercentage { get; set; }
        public string ResultJson { get; set; } = string.Empty; // Todo el análisis detallado

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        public CandidateProfile? CandidateProfile { get; set; }
        public JobVacancy? JobVacancy { get; set; }
        public string? CoverLetter { get; set; }
    }
}