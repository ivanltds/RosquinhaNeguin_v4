using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// ── Banco de dados (SQL Server) ──────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// ── Autenticação e Autorização ──────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/index.html";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

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
    p.SetIsOriginAllowed(_ => true)
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials()));

var app = builder.Build();

// ── Auto-migrar banco na inicialização (com retry para Docker) ──
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    // Tenta aplicar migrações até 5 vezes (SQL Server no Docker demora a subir)
    int retries = 5;
    while (retries > 0)
    {
        try {
            db.Database.Migrate();

            // Correção Manual: Adiciona a coluna UsuarioId se ela não existir
            // Útil quando não podemos rodar 'dotnet ef migrations add' no ambiente atual
            try {
                db.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Pedidos]') AND name = 'UsuarioId')
                    BEGIN
                        ALTER TABLE [Pedidos] ADD [UsuarioId] int NULL;
                    END
                ");
            } catch (Exception ex) {
                Console.WriteLine($"[Aviso] Verificação de coluna UsuarioId: {ex.Message}");
            }

            // Garante que o usuário Admin existe e tem a senha correta (1234)
            var admin = db.Usuarios.FirstOrDefault(u => u.Email == "admin@admin.local");
            if (admin == null)
            {
                db.Usuarios.Add(new RosquinhaNeguin.Models.Usuario
                {
                    Nome = "Administrador",
                    Email = "admin@admin.local",
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("1234"),
                    Role = "Admin"
                });
                db.SaveChanges();
            }
            else 
            {
                // Força atualização da senha para '1234' para facilitar o acesso do usuário
                admin.SenhaHash = BCrypt.Net.BCrypt.HashPassword("1234");
                db.SaveChanges();
            }

            // Diagnóstico de dados
            var qtdProdutos = db.Produtos.Count();
            var qtdUsuarios = db.Usuarios.Count();
            Console.WriteLine($"[DB Status] Banco Conectado: {db.Database.GetDbConnection().Database}");
            Console.WriteLine($"[DB Status] Produtos: {qtdProdutos}, Usuários: {qtdUsuarios}");

            break; 
        } catch (Exception) {
            retries--;
            if (retries == 0) throw;
            Console.WriteLine($"Aguardando SQL Server... ({retries} tentativas restantes)");
            Thread.Sleep(10000); // espera 10s
        }
    }
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

// Configuração para suportar arquivos do Unity WebGL
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".data"] = "application/octet-stream";
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".symbols.json"] = "application/octet-stream";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
