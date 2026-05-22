using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using Microsoft.AspNetCore.Authorization;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController(AppDbContext db) : ControllerBase
{
    [HttpGet("pix-public")]
    public async Task<IActionResult> GetPixPublic()
    {
        var pixKey = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixKey");
        var pixBeneficiario = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixBeneficiario");
        var pixCidade = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixCidade");

        return Ok(new
        {
            pixKey = pixKey?.Valor ?? "5511999999999",
            pixBeneficiario = pixBeneficiario?.Valor ?? "Rosquinha do Neguin Ltda",
            pixCidade = pixCidade?.Valor ?? "Sao Paulo"
        });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminConfigs()
    {
        var configs = await db.Configuracoes.ToListAsync();
        return Ok(configs);
    }

    [HttpPost("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SaveConfigs([FromBody] List<SalvarConfiguracaoDto> dtos)
    {
        if (dtos == null || dtos.Count == 0) return BadRequest("Nenhum dado informado.");

        foreach (var dto in dtos)
        {
            var config = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == dto.Chave);
            if (config != null)
            {
                config.Valor = dto.Valor;
            }
            else
            {
                db.Configuracoes.Add(new Configuracao { Chave = dto.Chave, Valor = dto.Valor });
            }
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Configurações atualizadas com sucesso!" });
    }
}
