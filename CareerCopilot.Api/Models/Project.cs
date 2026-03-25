namespace CareerCopilot.Api.Models
{ // Para guardar el complexity score, evitar evaluar solo por anios
    public class Project
    {
        public int Id { get; set; }
        public int CandidateProfileId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TechStack { get; set; } = string.Empty; // Formato JSON recomendado
        public int CalculatedComplexityScore { get; set; }

        public CandidateProfile? CandidateProfile { get; set; }
    }
}