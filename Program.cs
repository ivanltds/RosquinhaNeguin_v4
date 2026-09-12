using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// ── Proxy / Headers Reversos (Render, Cloudflare, Nginx) ─────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Banco de dados (SQL Server / SQLite Dev) ─────────────────
var connectionString = builder.Configuration.GetConnectionString("Default");
bool isSqlite = connectionString != null && (connectionString.Contains(".db") || connectionString.Contains("Filename="));

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (isSqlite)
        opt.UseSqlite(connectionString);
    else
        opt.UseSqlServer(connectionString);
});

// ── Autenticação e Autorização ──────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RosquinhaNeguin.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/index.html";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect("/login.html");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = 403;
                return Task.CompletedTask;
            }
            context.Response.Redirect("/index.html");
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

// ── Auto-inicializar banco na inicialização ──────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();

    try {
        if (db.Database.IsSqlite())
        {
            db.Database.EnsureCreated();
        }
        else
        {
            db.Database.Migrate();
        }
    } catch (Exception ex) {
        Console.WriteLine($"[Aviso DB] Falha no Migrate: {ex.Message}. Inicializando via EnsureCreated...");
        db.Database.EnsureCreated();
    }


    // Garante usuários, cupons e massa de testes completa automaticamente
    DbSeeder.SeedAsync(db).GetAwaiter().GetResult();

    // Diagnóstico de dados
    var qtdProdutos = db.Produtos.Count();
    var qtdUsuarios = db.Usuarios.Count();
    var qtdPedidos = db.Pedidos.Count();
    var qtdCupons = db.Cupons.Count();
    Console.WriteLine($"[DB Status] Banco Conectado: {db.Database.GetDbConnection().Database}");
    Console.WriteLine($"[DB Status] Produtos: {qtdProdutos}, Usuários: {qtdUsuarios}, Pedidos Teste: {qtdPedidos}, Cupons: {qtdCupons}");
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

app.UseForwardedHeaders();
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
