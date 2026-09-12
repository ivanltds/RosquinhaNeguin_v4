using Microsoft.AspNetCore.Mvc;
using RosquinhaNeguin.Helpers;
using RosquinhaNeguin.Models;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FreteController : ControllerBase
{
    // GET: api/frete/calcular/{cep}
    [HttpGet("calcular/{cep}")]
    public async Task<IActionResult> CalcularPorGet(string cep)
    {
        var resultado = await FreteCalculator.CalcularFreteAsync(cep);
        return Ok(resultado);
    }

    // POST: api/frete/calcular
    [HttpPost("calcular")]
    public async Task<IActionResult> CalcularPorPost([FromBody] CalcularFreteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Cep))
        {
            return BadRequest(new ResultadoFreteDto(false, "Informe o CEP de destino.", 0, 0, "", "", "", ""));
        }

        var resultado = await FreteCalculator.CalcularFreteAsync(dto.Cep);
        return Ok(resultado);
    }
}
