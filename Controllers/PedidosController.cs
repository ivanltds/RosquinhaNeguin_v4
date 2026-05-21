using Microsoft.AspNetCore.Mvc; // Permite criar endpoints HTTP
using Microsoft.EntityFrameworkCore; // Usado para consultas no banco (Include, ToListAsync, etc)
using RosquinhaNeguin.Data; // Contexto do banco (AppDbContext)
using RosquinhaNeguin.Models; // Models: Pedido, ItemPedido, DTOs

namespace RosquinhaNeguin.Controllers;

// Define que é uma API
[ApiController]

// Rota base → /api/pedidos
[Route("api/pedidos")]
public class PedidosController(AppDbContext db) : ControllerBase
{
    // Lista de status válidos (controle de fluxo do pedido)
    static readonly string[] StatusValidos =
        ["pendente", "confirmado", "preparo", "entregue", "cancelado"];

    // ─────────────────────────────────────────────
    // GET /api/pedidos
    // Lista todos os pedidos (com filtro opcional por status)
    // ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        // Inclui os itens do pedido (JOIN automático)
        var q = db.Pedidos.Include(p => p.Itens).AsQueryable();

        // Filtra por status se informado
        if (!string.IsNullOrEmpty(status))
            q = q.Where(p => p.Status == status);

        // Ordena do mais recente para o mais antigo
        return Ok(await q.OrderByDescending(p => p.CriadoEm).ToListAsync());
    }

    // ─────────────────────────────────────────────
    // GET /api/pedidos/{id}
    // Retorna um pedido específico
    // ─────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await db.Pedidos
            .Include(x => x.Itens) // traz os itens junto
            .FirstOrDefaultAsync(x => x.Id == id);

        // Se não encontrou → 404
        return p is null 
            ? NotFound(new { erro = "Pedido não encontrado." }) 
            : Ok(p);
    }

    // ─────────────────────────────────────────────
    // POST /api/pedidos
    // Cria um novo pedido
    // ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(CriarPedidoDto dto)
    {
        // Validação: precisa ter pelo menos 1 item
        if (dto.Itens == null || dto.Itens.Count == 0)
            return BadRequest(new { erro = "O pedido deve ter pelo menos 1 item." });

        // Cria objeto pedido
        var pedido = new Pedido
        {
            NomeCliente = dto.NomeCliente?.Trim(),
            Telefone = dto.Telefone?.Trim(),
            Observacao = dto.Observacao?.Trim(),
            CriadoEm = DateTime.UtcNow,
            Status = "pendente"
        };

        // Percorre cada item enviado
        foreach (var item in dto.Itens)
        {
            // Validação de quantidade
            if (item.Quantidade <= 0)
                return BadRequest(new { erro = "Quantidade deve ser maior que zero." });

            // Busca produto no banco
            var produto = await db.Produtos.FindAsync(item.ProdutoId);

            // Produto não existe
            if (produto is null)
                return BadRequest(new { erro = $"Produto #{item.ProdutoId} não encontrado." });

            // Produto desativado
            if (!produto.Ativo)
                return BadRequest(new { erro = $"Produto '{produto.Nome}' não está disponível." });

            // Adiciona item ao pedido
            pedido.Itens.Add(new ItemPedido
            {
                ProdutoId = produto.Id,
                NomeProduto = produto.Nome,
                EmojiProduto = produto.Emoji,
                Quantidade = item.Quantidade,
                PrecoUnitario = produto.Preco
            });
        }

        // Calcula total do pedido
        pedido.Total = pedido.Itens.Sum(i => i.Subtotal);

        // Salva no banco
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();

        // Retorna 201 Created
        return CreatedAtAction(nameof(Get), new { id = pedido.Id }, pedido);
    }

    // ─────────────────────────────────────────────
    // PATCH /api/pedidos/{id}/status
    // Atualiza o status do pedido
    // ─────────────────────────────────────────────
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, AtualizarStatusDto dto)
    {
        // Valida status
        if (!StatusValidos.Contains(dto.Status))
            return BadRequest(new 
            { 
                erro = $"Status inválido. Use: {string.Join(", ", StatusValidos)}" 
            });

        // Busca pedido
        var pedido = await db.Pedidos.FindAsync(id);

        if (pedido is null)
            return NotFound(new { erro = "Pedido não encontrado." });

        // Atualiza status
        pedido.Status = dto.Status;

        await db.SaveChangesAsync();

        return Ok(pedido);
    }

    // ─────────────────────────────────────────────
    // DELETE /api/pedidos/{id}
    // Remove pedido e seus itens
    // ─────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pedido = await db.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido is null)
            return NotFound(new { erro = "Pedido não encontrado." });

        // Remove itens primeiro (boa prática)
        db.ItensPedido.RemoveRange(pedido.Itens);

        // Remove pedido
        db.Pedidos.Remove(pedido);

        await db.SaveChangesAsync();

        return NoContent();
    }

    // ─────────────────────────────────────────────
    // GET /api/pedidos/stats
    // Estatísticas para painel admin
    // ─────────────────────────────────────────────
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var hoje = DateTime.UtcNow.Date;

        // Carrega todos pedidos com itens
        var pedidos = await db.Pedidos
            .Include(p => p.Itens)
            .ToListAsync();

        // Calcula métricas
        var stats = new
        {
            total = pedidos.Count,

            hoje = pedidos.Count(p => p.CriadoEm.Date == hoje),

            faturamentoTotal = pedidos
                .Where(p => p.Status != "cancelado")
                .Sum(p => p.Total),

            faturamentoHoje = pedidos
                .Where(p => p.CriadoEm.Date == hoje && p.Status != "cancelado")
                .Sum(p => p.Total),

            porStatus = StatusValidos.Select(s => new 
            { 
                status = s, 
                count = pedidos.Count(p => p.Status == s) 
            }),
        };

        return Ok(stats);
    }
}