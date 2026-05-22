using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using Microsoft.AspNetCore.Authorization;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/admin/stock")]
[Authorize(Roles = "Admin")]
public class StockController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetStatus()
    {
        var stock = await db.Produtos
            .Select(p => new { p.Id, p.Nome, p.Estoque, Critico = p.Estoque < 10 })
            .ToListAsync();
        return Ok(stock);
    }

    [HttpPost("adjust")]
    public async Task<ActionResult> Adjust([FromBody] StockAdjustmentDto dto)
    {
        var product = await db.Produtos.FindAsync(dto.ProductId);
        if (product == null) return NotFound();

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            decimal oldStock = product.Estoque;
            if (dto.Type == "Entrada") product.Estoque += (int)dto.Quantity;
            else product.Estoque -= (int)dto.Quantity;

            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque {
                ProdutoId = dto.ProductId,
                Quantidade = dto.Quantity,
                Tipo = dto.Type,
                Data = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { product.Id, product.Estoque });
        }
        catch
        {
            await transaction.RollbackAsync();
            return BadRequest("Erro ao ajustar estoque.");
        }
    }
}

public record StockAdjustmentDto(int ProductId, decimal Quantity, string Type);
