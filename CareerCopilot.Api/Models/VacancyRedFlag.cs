namespace CareerCopilot.Api.Models
{
    public class VacancyRedFlag
    {
        public int Id { get; set; }
        public int JobVacancyId { get; set; }
        public string FlagCategory { get; set; } = string.Empty; // Ej. "Legacy Tech", "Low Salary"
        public string SnippetContext { get; set; } = string.Empty; // Texto exacto detectado por el LLM
        public string Severity { get; set; } = string.Empty; // "High", "Medium"

        public JobVacancy? JobVacancy { get; set; }
    }
}