using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using BC = BCrypt.Net.BCrypt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDto dto)
    {
        try 
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            Console.WriteLine($"[Login Debug] Recebido Email: '{dto.Email}', Senha: '{dto.Senha}' (Length: {dto.Senha?.Length})");
            
            if (user == null || !BC.Verify(dto.Senha, user.SenhaHash))
            {
                if (user != null) {
                    Console.WriteLine($"[Login Debug] Falha de autenticação para o usuário: {dto.Email}. Senha incorreta.");
                    await db.LogsLogin.AddAsync(new LogLogin { 
                        UsuarioId = user.Id, 
                        Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        Sucesso = false 
                    });
                    await db.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine($"[Login Debug] Falha de autenticação: Usuário {dto.Email} não encontrado.");
                }
                return Unauthorized(new { erro = "E-mail ou senha incorretos." });
            }

            // Criar Claims e Identidade para o Cookie
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Nome),
                new(ClaimTypes.Role, user.Role),
                new(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            await db.LogsLogin.AddAsync(new LogLogin { 
                UsuarioId = user.Id, 
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Sucesso = true 
            });
            await db.SaveChangesAsync();

            return Ok(new { user.Id, user.Nome, user.Role });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = "Erro no servidor ao realizar login.", detalhe = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto dto)
    {
        try 
        {
            if (await db.Usuarios.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { erro = "E-mail já cadastrado no sistema." });

            var user = new Usuario {
                Nome = dto.Nome,
                Email = dto.Email,
                SenhaHash = BC.HashPassword(dto.Senha),
                Role = dto.Role ?? "Cliente"
            };

            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            return Ok(new { user.Id, user.Nome, user.Role });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { erro = "Erro ao cadastrar usuário.", detalhe = ex.Message });
        }
    }

    [HttpPost("register-initial-admin")]
    public async Task<ActionResult> RegisterInitialAdmin()
    {
        if (await db.Usuarios.AnyAsync()) return BadRequest("Sistema já possui usuários.");

        var admin = new Usuario {
            Nome = "Administrador",
            Email = "admin@rosquinha.com",
            SenhaHash = BC.HashPassword("admin123"),
            Role = "Admin"
        };

        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        return Ok("Admin inicial criado: admin@rosquinha.com / admin123");
    }
}
