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
    [HttpGet("public")]
    public async Task<IActionResult> GetPublicConfig()
    {
        var configs = await db.Configuracoes.ToDictionaryAsync(c => c.Chave, c => c.Valor);
        
        var tempoPreparoStr = configs.GetValueOrDefault("TempoPreparoMinutos", "45");
        int.TryParse(tempoPreparoStr, out int tempoPreparo);
        if (tempoPreparo <= 0) tempoPreparo = 45;

        return Ok(new
        {
            tempoPreparoMinutos = tempoPreparo,
            lojaCep = configs.GetValueOrDefault("LojaCep", "06250250"),
            lojaLogradouro = configs.GetValueOrDefault("LojaLogradouro", "Rua Professor Sud Menucci"),
            lojaNumero = configs.GetValueOrDefault("LojaNumero", "123"),
            lojaBairro = configs.GetValueOrDefault("LojaBairro", "Jardim Elvira"),
            lojaCidade = configs.GetValueOrDefault("LojaCidade", "Osasco"),
            lojaUf = configs.GetValueOrDefault("LojaUf", "SP"),
            lojaEnderecoCompleto = $"{configs.GetValueOrDefault("LojaLogradouro", "Rua Professor Sud Menucci")}, {configs.GetValueOrDefault("LojaNumero", "123")} - {configs.GetValueOrDefault("LojaBairro", "Jardim Elvira")}, {configs.GetValueOrDefault("LojaCidade", "Osasco")}/{configs.GetValueOrDefault("LojaUf", "SP")} (CEP: {configs.GetValueOrDefault("LojaCep", "06250-250")})",
            pixKey = configs.GetValueOrDefault("PixKey", "11999999999"),
            pixBeneficiario = configs.GetValueOrDefault("PixBeneficiario", "Rosquinha do Neguin Ltda"),
            pixCidade = configs.GetValueOrDefault("PixCidade", "Osasco")
        });
    }

    [HttpGet("pix-public")]
    public async Task<IActionResult> GetPixPublic()
    {
        var pixKey = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixKey");
        var pixBeneficiario = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixBeneficiario");
        var pixCidade = await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixCidade");

        return Ok(new
        {
            pixKey = pixKey?.Valor ?? "11999999999",
            pixBeneficiario = pixBeneficiario?.Valor ?? "Rosquinha do Neguin Ltda",
            pixCidade = pixCidade?.Valor ?? "Osasco"
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
