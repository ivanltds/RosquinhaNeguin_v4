namespace RosquinhaNeguin.Models;

public class Produto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string Emoji { get; set; } = "";
    public decimal Preco { get; set; }
    public int CategoriaId { get; set; }
    public Categoria? CategoriaObjeto { get; set; }
    public bool Ativo { get; set; } = true;
    public int Estoque { get; set; }
    public string? ImagemUrl { get; set; }
}

public class Categoria
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
}

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string Role { get; set; } = "Gerente"; // Admin | Gerente
}

public class MovimentacaoEstoque
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public decimal Quantidade { get; set; }
    public string Tipo { get; set; } = "Entrada"; // Entrada | Saida
    public DateTime Data { get; set; } = DateTime.UtcNow;
}

public class LogLogin
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public string Ip { get; set; } = "";
    public bool Sucesso { get; set; }
}

public class Pedido
{
    public int Id { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "pendente";
    public string? NomeCliente { get; set; }
    public string? Telefone { get; set; }
    public string? Observacao { get; set; }
    public decimal Total { get; set; }
    public List<ItemPedido> Itens { get; set; } = new();
}

public class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }
    public string NomeProduto { get; set; } = "";
    public string EmojiProduto { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

public record CriarPedidoDto(string? NomeCliente, string? Telefone, string? Observacao, List<ItemPedidoDto> Itens);
public record ItemPedidoDto(int ProdutoId, int Quantidade);
public record AtualizarStatusDto(string Status);
public record LoginDto(string Email, string Senha);
public record RegisterDto(string Nome, string Email, string Senha, string? Role);
