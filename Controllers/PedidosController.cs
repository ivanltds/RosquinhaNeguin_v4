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
    static readonly string[] StatusValidos = ["pendente", "confirmado", "preparo", "reembolso_solicitado", "reembolsado", "entregue", "cancelado"];

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

    [HttpPost("{id:int}/solicitar-reembolso")]
    public async Task<IActionResult> SolicitarReembolso(int id, [FromBody] SolicitarReembolsoDto? dto)
    {
        var pedido = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null) return NotFound(new { erro = "Pedido não encontrado." });

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { erro = "Usuário não autenticado." });
        int userId = int.Parse(userIdStr);
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        // Apenas o cliente proprietário do pedido ou Admin pode solicitar
        if (pedido.UsuarioId != userId && userRole != "Admin")
        {
            return Forbid();
        }

        if (pedido.Status == "entregue")
        {
            return BadRequest(new { erro = "Não é possível solicitar reembolso para um pedido já entregue." });
        }

        if (pedido.Status == "cancelado" || pedido.Status == "reembolsado")
        {
            return BadRequest(new { erro = "Este pedido já se encontra cancelado/reembolsado." });
        }

        if (pedido.Status == "reembolso_solicitado")
        {
            return BadRequest(new { erro = "A solicitação de reembolso deste pedido já está em análise." });
        }

        // Verificar tempo estimado de preparo
        var prepMinStr = (await db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "TempoPreparoMinutos"))?.Valor ?? "45";
        int prepMin = int.TryParse(prepMinStr, out var m) ? m : 45;

        var elapsedMinutes = (DateTime.UtcNow - pedido.CriadoEm).TotalMinutes;
        if (elapsedMinutes < prepMin && userRole != "Admin")
        {
            return BadRequest(new { erro = $"O pedido ainda está no prazo estimado ({prepMin} min). A solicitação de reembolso por atraso fica disponível após este período." });
        }

        pedido.Status = "reembolso_solicitado";
        var motivoStr = !string.IsNullOrWhiteSpace(dto?.Motivo) ? dto.Motivo.Trim() : "Atraso no tempo de preparo";
        pedido.Observacao = string.IsNullOrWhiteSpace(pedido.Observacao)
            ? $"[Reembolso Solicitado: {motivoStr}]"
            : $"{pedido.Observacao} | [Reembolso Solicitado: {motivoStr}]";

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Solicitação de reembolso registrada com sucesso! A loja foi notificada.", pedido });
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
            // Validar CEP e raio de atendimento (Origem: 06250-250, máx 10km para outros municípios)
            decimal taxaEntregaCalculada = dto.TaxaEntrega ?? 0;
            if (!string.IsNullOrWhiteSpace(dto.Cep))
            {
                var freteResult = await Helpers.FreteCalculator.CalcularFreteAsync(dto.Cep);
                if (!freteResult.Atende)
                {
                    return BadRequest(new { erro = freteResult.Mensagem });
                }
                taxaEntregaCalculada = freteResult.ValorFrete;
            }

            var pedido = new Pedido
            {
                UsuarioId = userId,
                NomeCliente = dto.NomeCliente?.Trim(),
                Telefone = dto.Telefone?.Trim(),
                Observacao = dto.Observacao?.Trim(),
                MetodoPagamento = dto.MetodoPagamento ?? "Dinheiro",
                CartaoId = dto.CartaoId,
                Cep = dto.Cep?.Trim(),
                Logradouro = dto.Logradouro?.Trim(),
                Numero = dto.Numero?.Trim(),
                Complemento = dto.Complemento?.Trim(),
                Bairro = dto.Bairro?.Trim(),
                Cidade = dto.Cidade?.Trim(),
                Estado = dto.Estado?.Trim(),
                TaxaEntrega = taxaEntregaCalculada,
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

            pedido.Subtotal = pedido.Itens.Sum(i => i.Subtotal);

            // Validar e aplicar Cupom de Desconto (se informado)
            if (!string.IsNullOrWhiteSpace(dto.CupomCodigo))
            {
                var codigoNorm = dto.CupomCodigo.Trim().ToUpper();
                var cupom = await db.Cupons.FirstOrDefaultAsync(c => c.Codigo.ToUpper() == codigoNorm && c.Ativo);
                if (cupom != null && pedido.Subtotal >= cupom.ValorMinimoPedido)
                {
                    pedido.CupomCodigo = cupom.Codigo;
                    if (cupom.Tipo.Equals("Porcentagem", StringComparison.OrdinalIgnoreCase))
                    {
                        pedido.Desconto = Math.Round(pedido.Subtotal * (cupom.Valor / 100m), 2);
                    }
                    else
                    {
                        pedido.Desconto = Math.Min(cupom.Valor, pedido.Subtotal);
                    }
                }
            }

            pedido.Total = Math.Max(0, pedido.Subtotal + pedido.TaxaEntrega - pedido.Desconto);
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
                    pedido.Cep,
                    pedido.Logradouro,
                    pedido.Numero,
                    pedido.Complemento,
                    pedido.Bairro,
                    pedido.Cidade,
                    pedido.Estado,
                    pedido.TaxaEntrega,
                    pedido.CupomCodigo,
                    pedido.Desconto,
                    pedido.Subtotal,
                    pedido.Total,
                    pedido.CriadoEm,
                    pedido.Status,
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

        var pedido = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
        if (pedido is null) return NotFound(new { erro = "Pedido não encontrado." });

        var statusAnterior = pedido.Status;
        pedido.Status = dto.Status;

        // Se o admin aprovou o reembolso ou cancelou o pedido, devolve o estoque dos itens
        if ((dto.Status == "reembolsado" || dto.Status == "cancelado") &&
            (statusAnterior != "cancelado" && statusAnterior != "reembolsado"))
        {
            foreach (var item in pedido.Itens)
            {
                var produto = await db.Produtos.FindAsync(item.ProdutoId);
                if (produto != null)
                {
                    produto.Estoque += item.Quantidade;
                    db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        ProdutoId = produto.Id,
                        Quantidade = item.Quantidade,
                        Tipo = "Entrada",
                        Data = DateTime.UtcNow
                    });
                }
            }
        }

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
