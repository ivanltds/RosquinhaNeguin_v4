using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartoesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyCards()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { erro = "Sessão inválida." });
        int userId = int.Parse(userIdStr);

        var cards = await db.CartoesCredito
            .Where(c => c.UsuarioId == userId)
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new {
                c.Id,
                c.NumeroMascarado,
                c.Bandeira,
                c.NomeTitular,
                c.Validade
            })
            .ToListAsync();

        return Ok(cards);
    }

    [HttpPost]
    public async Task<IActionResult> AddCard([FromBody] CartaoCreditoDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { erro = "Sessão inválida." });
        int userId = int.Parse(userIdStr);

        if (string.IsNullOrWhiteSpace(dto.Numero) || dto.Numero.Length < 13)
            return BadRequest(new { erro = "Número de cartão inválido." });

        if (string.IsNullOrWhiteSpace(dto.NomeTitular))
            return BadRequest(new { erro = "Nome do titular é obrigatório." });

        if (string.IsNullOrWhiteSpace(dto.Validade) || !dto.Validade.Contains('/'))
            return BadRequest(new { erro = "Validade inválida (use MM/AA)." });

        // Limpa caracteres não numéricos
        var numApenasDigitos = new string(dto.Numero.Where(char.IsDigit).ToArray());
        if (numApenasDigitos.Length < 13 || numApenasDigitos.Length > 19)
            return BadRequest(new { erro = "Número de cartão de crédito inválido." });

        // Determina bandeira baseada no prefixo
        string bandeira = DetectCardBrand(numApenasDigitos);

        // Mascarar o número para salvar apenas os últimos 4 dígitos
        string numeroMascarado = $"**** **** **** {numApenasDigitos.Substring(numApenasDigitos.Length - 4)}";

        // Verifica se já existe um cartão idêntico cadastrado
        var cartaoExistente = await db.CartoesCredito
            .FirstOrDefaultAsync(c => c.UsuarioId == userId && c.NumeroMascarado == numeroMascarado && c.NomeTitular == dto.NomeTitular);

        if (cartaoExistente != null)
        {
            return Ok(new {
                cartaoExistente.Id,
                cartaoExistente.NumeroMascarado,
                cartaoExistente.Bandeira,
                cartaoExistente.NomeTitular,
                cartaoExistente.Validade
            });
        }

        var cartao = new CartaoCredito
        {
            UsuarioId = userId,
            NumeroMascarado = numeroMascarado,
            Bandeira = bandeira,
            NomeTitular = dto.NomeTitular,
            Validade = dto.Validade,
            CriadoEm = DateTime.UtcNow
        };

        db.CartoesCredito.Add(cartao);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyCards), new { id = cartao.Id }, new {
            cartao.Id,
            cartao.NumeroMascarado,
            cartao.Bandeira,
            cartao.NomeTitular,
            cartao.Validade
        });
    }

    private static string DetectCardBrand(string number)
    {
        if (number.StartsWith('4')) return "Visa";
        if (number.StartsWith("51") || number.StartsWith("52") || number.StartsWith("53") || number.StartsWith("54") || number.StartsWith("55") ||
            (number.Length >= 4 && int.TryParse(number.Substring(0, 4), out int prefix) && prefix >= 2221 && prefix <= 2720))
            return "Mastercard";
        if (number.StartsWith("34") || number.StartsWith("37")) return "Amex";
        if (number.StartsWith("6011") || number.StartsWith("622") || number.StartsWith("64") || number.StartsWith("65")) return "Discover";
        if (number.StartsWith("35") || number.StartsWith("36") || number.StartsWith("38")) return "Diners";
        if (number.StartsWith("5067") || number.StartsWith("4576") || number.StartsWith("4011")) return "Elo";
        return "Outra";
    }
}
