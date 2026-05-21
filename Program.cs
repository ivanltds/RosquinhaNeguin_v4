using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Banco de dados (Configurável) ──────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (string.IsNullOrEmpty(connectionString) || connectionString.Contains(".db") || connectionString.Contains("Data Source="))
    {
        opt.UseSqlite(connectionString ?? "Data Source=rosquinha.db");
    }
    else
    {
        opt.UseSqlServer(connectionString);
    }
});
    
    
// ── Controllers + JSON camelCase ─────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;

        o.JsonSerializerOptions.ReferenceHandler =
            ReferenceHandler.IgnoreCycles;
    });
// ── Swagger (documentação da API) ────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Rosquinha do Neguin API",
        Version = "v1",
        Description = "API do sistema de pedidos da Rosquinha do Neguin 🍩"
    });
});

// ── CORS ─────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Garantir que o banco existe e está atualizado ───────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Para SQLite em desenvolvimento, EnsureCreated é mais simples que migrações
    db.Database.EnsureCreated();
}

// ── Swagger UI (só em desenvolvimento) ───────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rosquinha API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
