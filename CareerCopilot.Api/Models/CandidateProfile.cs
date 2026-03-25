namespace CareerCopilot.Api.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ProfessionalTitle { get; set; } = string.Empty;
        public int TotalExperienceYears { get; set; }
        public string Summary { get; set; } = string.Empty;

        // Relaciones
        public User? User { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}