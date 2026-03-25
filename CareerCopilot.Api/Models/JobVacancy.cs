namespace CareerCopilot.Api.Models
{
    public class JobVacancy
    {
        public int Id { get; set; }
        public string SourceUrl { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RawDescription { get; set; } = string.Empty;
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<VacancyRedFlag> RedFlags { get; set; } = new List<VacancyRedFlag>();
        public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}