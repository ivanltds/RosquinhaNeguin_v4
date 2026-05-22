using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Data;
using RosquinhaNeguin.Models;

namespace RosquinhaNeguin.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? categoria)
    {
        var q = db.Produtos.Include(p => p.CategoriaObjeto).Where(p => p.Ativo);

        if (!string.IsNullOrEmpty(categoria))
        {
            var catId = await db.Categorias
                .Where(c => c.Nome == categoria.ToLower())
                .Select(c => c.Id)
                .FirstOrDefaultAsync();

            if (catId > 0)
                q = q.Where(p => p.CategoriaId == catId);
        }

        var produtos = await q.OrderBy(p => p.Id).ToListAsync();
        
        // Mapeia para um objeto anônimo para manter a propriedade "categoria" como string para o front-end antigo
        return Ok(produtos.Select(p => new {
            p.Id,
            p.Nome,
            p.Descricao,
            p.Emoji,
            p.Preco,
            p.Estoque,
            p.Ativo,
            p.ImagemUrl,
            Categoria = p.CategoriaObjeto?.Nome ?? "sem categoria"
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await db.Produtos.Include(p => p.CategoriaObjeto).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound(new { erro = "Produto não encontrado." }) : Ok(new {
            p.Id,
            p.Nome,
            p.Descricao,
            p.Emoji,
            p.Preco,
            p.Estoque,
            p.Ativo,
            p.ImagemUrl,
            Categoria = p.CategoriaObjeto?.Nome ?? "sem categoria"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Produto p)
    {
        if (string.IsNullOrWhiteSpace(p.Nome)) return BadRequest(new { erro = "Nome é obrigatório." });
        if (p.Preco <= 0) return BadRequest(new { erro = "Preço deve ser maior que zero." });

        p.Ativo = true;
        db.Produtos.Add(p);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Produto p)
    {
        if (id != p.Id) return BadRequest(new { erro = "ID não confere." });
        db.Entry(p).State = EntityState.Modified;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Produtos.FindAsync(id);
        if (p is null) return NotFound(new { erro = "Produto não encontrado." });
        p.Ativo = false;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias() => Ok(await db.Categorias.Select(c => c.Nome).ToListAsync());
}
