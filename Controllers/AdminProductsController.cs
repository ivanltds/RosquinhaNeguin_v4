using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;
using Microsoft.AspNetCore.Authorization;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Produto>>> Get() => 
        await db.Produtos.Include(p => p.CategoriaObjeto).ToListAsync();

    [HttpPost]
    public async Task<ActionResult<Produto>> Post(Produto produto)
    {
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = produto.Id }, produto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Produto produto)
    {
        if (id != produto.Id) return BadRequest();
        db.Entry(produto).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var produto = await db.Produtos.FindAsync(id);
        if (produto == null) return NotFound();
        produto.Ativo = false; // Soft delete
        await db.SaveChangesAsync();
        return NoContent();
    }
}
