using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using Microsoft.AspNetCore.Authorization;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard()
    {
        var hoje = DateTime.UtcNow.Date;
        
        var pedidosHoje = await db.Pedidos
            .Where(p => p.CriadoEm >= hoje)
            .CountAsync();

        var faturamentoHoje = await db.Pedidos
            .Where(p => p.CriadoEm >= hoje && p.Status != "cancelado")
            .SumAsync(p => p.Total);

        var estoqueCritico = await db.Produtos
            .CountAsync(p => p.Estoque < 10);

        var ultimosPedidos = await db.Pedidos
            .OrderByDescending(p => p.CriadoEm)
            .Take(5)
            .ToListAsync();

        return Ok(new {
            pedidosHoje,
            faturamentoHoje,
            estoqueCritico,
            ultimosPedidos
        });
    }

    [HttpGet("clientes")]
    public async Task<ActionResult> GetClientesReport()
    {
        var pedidos = await db.Pedidos
            .Where(p => p.Status != "cancelado")
            .ToListAsync();

        var clientes = pedidos
            .GroupBy(p => string.IsNullOrWhiteSpace(p.NomeCliente) ? (p.Telefone ?? "Cliente Anônimo") : p.NomeCliente.Trim())
            .Select(g => new ClienteReportDto(
                Nome: g.Key,
                Telefone: g.FirstOrDefault()?.Telefone ?? "-",
                TotalPedidos: g.Count(),
                TotalGasto: g.Sum(p => p.Total),
                UltimoPedido: g.Max(p => p.CriadoEm)
            ))
            .OrderByDescending(c => c.TotalPedidos)
            .ThenByDescending(c => c.TotalGasto)
            .ToList();

        return Ok(clientes);
    }
}

