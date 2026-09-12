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
        try
        {
            var hoje = DateTime.UtcNow.Date;
            
            var pedidosHojeList = await db.Pedidos
                .Where(p => p.CriadoEm >= hoje)
                .ToListAsync();

            var pedidosHoje = pedidosHojeList.Count;
            var faturamentoHoje = pedidosHojeList
                .Where(p => p.Status != "cancelado" && p.Status != "reembolsado")
                .Sum(p => p.Total);

            var estoqueCritico = await db.Produtos
                .CountAsync(p => p.Estoque < 10);

            var ultimosPedidos = await db.Pedidos
                .OrderByDescending(p => p.CriadoEm)
                .Take(5)
                .Select(p => new {
                    p.Id,
                    p.NomeCliente,
                    p.Telefone,
                    p.Total,
                    p.Status,
                    p.CriadoEm
                })
                .ToListAsync();

            return Ok(new {
                pedidosHoje,
                faturamentoHoje,
                estoqueCritico,
                ultimosPedidos
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ReportsController.GetDashboard Error] {ex.Message}");
            return StatusCode(500, new { erro = "Erro ao carregar dados do dashboard", detalhe = ex.Message });
        }
    }

    [HttpGet("clientes")]
    public async Task<ActionResult> GetClientesReport()
    {
        try
        {
            var pedidos = await db.Pedidos
                .Where(p => p.Status != "cancelado" && p.Status != "reembolsado")
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
        catch (Exception ex)
        {
            Console.WriteLine($"[ReportsController.GetClientesReport Error] {ex.Message}");
            return StatusCode(500, new { erro = "Erro ao carregar relatório de clientes", detalhe = ex.Message });
        }
    }
}

