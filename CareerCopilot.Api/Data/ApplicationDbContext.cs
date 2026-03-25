using CareerCopilot.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Tablas que se crearán en SQL Server
        public DbSet<User> Users { get; set; }
        public DbSet<CandidateProfile> Profiles { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<JobVacancy> JobVacancies { get; set; }
        public DbSet<VacancyRedFlag> VacancyRedFlags { get; set; }
        public DbSet<Evaluation> Evaluations { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuracion para porcentajes ej. 95.50%
            modelBuilder.Entity<Evaluation>()
                .Property(e => e.GlobalMatchPercentage)
                .HasColumnType("decimal(5,2)");

            // Configuración para salarios (ej. 150000.00)
            modelBuilder.Entity<JobVacancy>()
                .Property(j => j.SalaryMin)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<JobVacancy>()
                .Property(j => j.SalaryMax)
                .HasColumnType("decimal(18,2)");
        }
    }
}