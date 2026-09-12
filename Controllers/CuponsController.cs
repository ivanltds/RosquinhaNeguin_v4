using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CuponsController(AppDbContext db) : ControllerBase
{
    // GET: api/cupons
    [HttpGet]
    public async Task<IActionResult> GetCupons()
    {
        var cupons = await db.Cupons.Where(c => c.Ativo).ToListAsync();
        return Ok(cupons);
    }

    // POST: api/cupons/validar
    [HttpPost("validar")]
    public async Task<IActionResult> ValidarCupom([FromBody] ValidarCupomDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo))
        {
            return BadRequest(new ResultadoCupomDto(false, "Código do cupom não informado.", 0, "", "", 0));
        }

        var codigoNorm = dto.Codigo.Trim().ToUpper();
        var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Codigo.ToUpper() == codigoNorm && c.Ativo);

        if (cupom == null)
        {
            return Ok(new ResultadoCupomDto(false, "Cupom inválido ou expirado.", 0, dto.Codigo, "", 0));
        }

        if (dto.Subtotal < cupom.ValorMinimoPedido)
        {
            return Ok(new ResultadoCupomDto(
                false, 
                $"Valor mínimo do pedido para este cupom é R$ {cupom.ValorMinimoPedido:F2}.", 
                0, 
                cupom.Codigo, 
                cupom.Tipo, 
                cupom.Valor
            ));
        }

        decimal descontoCalculado = 0;
        if (cupom.Tipo.Equals("Porcentagem", StringComparison.OrdinalIgnoreCase))
        {
            descontoCalculado = Math.Round(dto.Subtotal * (cupom.Valor / 100m), 2);
        }
        else
        {
            descontoCalculado = Math.Min(cupom.Valor, dto.Subtotal);
        }

        return Ok(new ResultadoCupomDto(
            true, 
            "Cupom aplicado com sucesso!", 
            descontoCalculado, 
            cupom.Codigo, 
            cupom.Tipo, 
            cupom.Valor
        ));
    }
}
