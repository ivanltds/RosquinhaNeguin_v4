using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using Microsoft.AspNetCore.Authorization;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/pedidos")]
[Authorize]
public class PedidosController(AppDbContext db) : ControllerBase
{
    static readonly string[] StatusValidos = ["pendente", "confirmado", "preparo", "entregue", "cancelado"];

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var q = db.Pedidos.Include(p => p.Itens).AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);
        return Ok(await q.OrderByDescending(p => p.CriadoEm).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await db.Pedidos.Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound(new { erro = "Pedido não encontrado." }) : Ok(p);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        try 
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { erro = "Usuário não identificado na sessão." });

            var userId = int.Parse(userIdStr);
            var orders = await db.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.UsuarioId == userId)
                .OrderByDescending(p => p.CriadoEm)
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            // Log do erro para debug (visível no console do servidor)
            Console.WriteLine($"[ERRO GetMyOrders] {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, new { erro = "Erro interno ao buscar pedidos", detalhe = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CriarPedidoDto dto)
    {
        // ... (resto do método)
        if (dto.Itens == null || dto.Itens.Count == 0)
            return BadRequest(new { erro = "O pedido deve ter pelo menos 1 item." });

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var pedido = new Pedido
            {
                UsuarioId = userId,
                NomeCliente = dto.NomeCliente?.Trim(),
                Telefone = dto.Telefone?.Trim(),
                Observacao = dto.Observacao?.Trim(),
                MetodoPagamento = dto.MetodoPagamento ?? "Dinheiro",
                CartaoId = dto.CartaoId,
                CriadoEm = DateTime.UtcNow,
                Status = "pendente"
            };

            foreach (var itemDto in dto.Itens)
            {
                var produto = await db.Produtos.FindAsync(itemDto.ProdutoId);
                if (produto is null) throw new Exception($"Produto #{itemDto.ProdutoId} não encontrado.");
                if (!produto.Ativo) throw new Exception($"Produto '{produto.Nome}' não está disponível.");
                if (produto.Estoque < itemDto.Quantidade) throw new Exception($"Estoque insuficiente para '{produto.Nome}'.");

                // Baixa de estoque
                produto.Estoque -= itemDto.Quantidade;

                pedido.Itens.Add(new ItemPedido
                {
                    ProdutoId = produto.Id,
                    NomeProduto = produto.Nome,
                    EmojiProduto = produto.Emoji,
                    Quantidade = itemDto.Quantidade,
                    PrecoUnitario = produto.Preco
                });

                // Registrar movimentação
                db.MovimentacoesEstoque.Add(new MovimentacaoEstoque {
                    ProdutoId = produto.Id,
                    Quantidade = itemDto.Quantidade,
                    Tipo = "Saida",
                    Data = DateTime.UtcNow
                });
            }

            pedido.Total = pedido.Itens.Sum(i => i.Subtotal);
            db.Pedidos.Add(pedido);
            
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (pedido.MetodoPagamento == "Pix")
            {
                var pixKey = (await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixKey"))?.Valor ?? "5511999999999";
                var pixBeneficiario = (await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixBeneficiario"))?.Valor ?? "Rosquinha do Neguin Ltda";
                var pixCidade = (await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "PixCidade"))?.Valor ?? "Sao Paulo";

                string pixCopiaECola = Helpers.PixGenerator.GeneratePayload(pixKey, pixBeneficiario, pixCidade, pedido.Total, $"PED{pedido.Id}");

                return CreatedAtAction(nameof(Get), new { id = pedido.Id }, new
                {
                    pedido.Id,
                    pedido.UsuarioId,
                    pedido.NomeCliente,
                    pedido.Telefone,
                    pedido.Observacao,
                    pedido.CriadoEm,
                    pedido.Status,
                    pedido.Total,
                    pedido.MetodoPagamento,
                    pedido.CartaoId,
                    pedido.Itens,
                    pixCopiaECola
                });
            }

            return CreatedAtAction(nameof(Get), new { id = pedido.Id }, pedido);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPost("{id:int}/simular-pagamento")]
    public async Task<IActionResult> SimularPagamento(int id)
    {
        var pedido = await db.Pedidos.FindAsync(id);
        if (pedido is null) return NotFound(new { erro = "Pedido não encontrado." });

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { erro = "Usuário não autenticado." });
        int userId = int.Parse(userIdStr);

        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (pedido.UsuarioId != userId && userRole != "Admin")
        {
            return Forbid();
        }

        if (pedido.Status != "pendente")
        {
            return BadRequest(new { erro = "Apenas pedidos pendentes podem ser pagos." });
        }

        pedido.Status = "confirmado";
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Pagamento simulado com sucesso!", status = pedido.Status });
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, AtualizarStatusDto dto)
    {
        if (!StatusValidos.Contains(dto.Status))
            return BadRequest(new { erro = $"Status inválido." });

        var pedido = await db.Pedidos.FindAsync(id);
        if (pedido is null) return NotFound(new { erro = "Pedido não encontrado." });

        pedido.Status = dto.Status;
        await db.SaveChangesAsync();
        return Ok(pedido);
    }

    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStats()
    {
        var porStatus = await db.Pedidos
            .GroupBy(p => p.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();

        return Ok(new { porStatus });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var pedido = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null) return NotFound(new { erro = "Pedido não encontrado." });

        db.ItensPedido.RemoveRange(pedido.Itens);
        db.Pedidos.Remove(pedido);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
