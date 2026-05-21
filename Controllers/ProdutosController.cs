using Microsoft.AspNetCore.Mvc; // Permite criar endpoints HTTP (API)
using Microsoft.EntityFrameworkCore; // Usado para acessar o banco com EF Core
using RosquinhaNeguin.Data; // Onde está o AppDbContext (conexão com banco)
using RosquinhaNeguin.Models; // Onde estão os modelos (Produto, Pedido, etc)

namespace RosquinhaNeguin.Controllers;

// Define que isso é um controller de API
[ApiController]

// Define a rota base → /api/produtos
[Route("api/produtos")]
public class ProdutosController(AppDbContext db) : ControllerBase
{
    // Lista de categorias permitidas (validação)
    static readonly string[] CategoriasValidas =
        ["churros", "rosquinhas", "salgados", "bebidas", "combos"];

    // ─────────────────────────────────────────────
    // GET /api/produtos
    // Lista todos os produtos ativos (com filtro opcional por categoria)
    // ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? categoria)
    {
        // Começa buscando apenas produtos ativos
        var q = db.Produtos.Where(p => p.Ativo);

        // Se veio categoria na URL, filtra
        if (!string.IsNullOrEmpty(categoria))
            q = q.Where(p => p.Categoria == categoria.ToLower());

        // Retorna lista ordenada por ID
        return Ok(await q.OrderBy(p => p.Id).ToListAsync());
    }

    // ─────────────────────────────────────────────
    // GET /api/produtos/{id}
    // Busca um produto específico pelo ID
    // ─────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await db.Produtos.FindAsync(id);

        // Se não encontrou → 404
        return p is null 
            ? NotFound(new { erro = "Produto não encontrado." }) 
            : Ok(p);
    }

    // ─────────────────────────────────────────────
    // POST /api/produtos
    // Cria um novo produto
    // ─────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(Produto p)
    {
        // Validação: nome obrigatório
        if (string.IsNullOrWhiteSpace(p.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        // Validação: preço deve ser maior que zero
        if (p.Preco <= 0)
            return BadRequest(new { erro = "Preço deve ser maior que zero." });

        // Validação: categoria válida
        var categoriaLower = p.Categoria?.ToLower() ?? "";
        if (!CategoriasValidas.Contains(categoriaLower))
            return BadRequest(new 
            { 
                erro = $"Categoria inválida. Use: {string.Join(", ", CategoriasValidas)}" 
            });

        // Normaliza categoria para minúsculo
        p.Categoria = categoriaLower;

        // Produto começa ativo
        p.Ativo = true;

        // Adiciona no banco
        db.Produtos.Add(p);
        await db.SaveChangesAsync();

        // Retorna 201 Created com localização do novo recurso
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    // ─────────────────────────────────────────────
    // PUT /api/produtos/{id}
    // Atualiza um produto existente
    // ─────────────────────────────────────────────
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Produto p)
    {
        // Valida se o ID da URL bate com o objeto
        if (id != p.Id) 
            return BadRequest(new { erro = "ID não confere." });

        // Validação: nome obrigatório
        if (string.IsNullOrWhiteSpace(p.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        // Validação: preço válido
        if (p.Preco <= 0)
            return BadRequest(new { erro = "Preço deve ser maior que zero." });

        // Normaliza categoria
        p.Categoria = p.Categoria.ToLower();

        // Marca como modificado (EF vai atualizar tudo)
        db.Entry(p).State = EntityState.Modified;

        await db.SaveChangesAsync();

        // Retorna 204 (sem conteúdo)
        return NoContent();
    }

    // ─────────────────────────────────────────────
    // DELETE /api/produtos/{id}
    // "Deleta" produto (soft delete → só desativa)
    // ─────────────────────────────────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Produtos.FindAsync(id);

        // Se não existe
        if (p is null) 
            return NotFound(new { erro = "Produto não encontrado." });

        // Não remove do banco → apenas desativa
        p.Ativo = false;

        await db.SaveChangesAsync();

        return NoContent();
    }

    // ─────────────────────────────────────────────
    // GET /api/produtos/categorias
    // Retorna lista de categorias disponíveis
    // ─────────────────────────────────────────────
    [HttpGet("categorias")]
    public IActionResult GetCategorias() => Ok(CategoriasValidas);
}