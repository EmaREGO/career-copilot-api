using Microsoft.EntityFrameworkCore;
using CareerCopilot.Api.Data;
using Hangfire;
using Hangfire.PostgreSql;
using CareerCopilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? builder.Configuration["DATABASE_URL"];
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configuracion Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

// Add Worker (Procesa las tareas)
builder.Services.AddHangfireServer();
// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IScraperService, ScraperService>();
builder.Services.AddScoped<IPdfExtractionService, PdfExtractionService>();
builder.Services.AddHttpClient<ILlmService, LlmService>();
//builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddScoped<CareerAnalysisJob>();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}
// CONFIGURACIÓN GLOBAL DE CORS 
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Career Copilot API v1");
    c.RoutePrefix = string.Empty; 
});



app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseHangfireDashboard();

app.Run();
