namespace RosquinhaNeguin.Models;

// ── Produto (item do cardápio) ───────────────────────────────
public class Produto
{
    public int Id { get; set; } // ID único no banco

    public string Nome { get; set; } = ""; // Nome do produto (ex: "Churros Nutella")

    public string Descricao { get; set; } = ""; // Descrição do produto

    public string Emoji { get; set; } = ""; // Emoji pra exibir no front (🍩, 🍫 etc)

    public decimal Preco { get; set; } // Preço do produto

    public string Categoria { get; set; } = ""; 
    // Categoria do produto:
    // churros | rosquinhas | salgados | bebidas | combos

    public bool Ativo { get; set; } = true; 
    // Se o produto está disponível no sistema (soft delete)
}


// ── Pedido (compra do cliente) ───────────────────────────────
public class Pedido
{
    public int Id { get; set; } // ID do pedido

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow; 
    // Data de criação (UTC padrão)

    public string Status { get; set; } = "pendente"; 
    // Status do pedido:
    // pendente | confirmado | preparo | entregue | cancelado

    public string? NomeCliente { get; set; } // Nome do cliente (opcional)

    public string? Telefone { get; set; } // Telefone do cliente (opcional)

    public string? Observacao { get; set; } // Observações (ex: "sem açúcar")

    public decimal Total { get; set; } 
    // Valor total do pedido (calculado)

    public List<ItemPedido> Itens { get; set; } = new(); 
    // Lista de itens do pedido (relacionamento 1:N)
}


// ── Item do Pedido ───────────────────────────────────────────
public class ItemPedido
{
    public int Id { get; set; } // ID do item

    public int PedidoId { get; set; } 
    // FK → Pedido

    public Pedido? Pedido { get; set; } 
    // Navegação para o pedido

    public int ProdutoId { get; set; } 
    // FK → Produto

    public Produto? Produto { get; set; } 
    // Navegação para o produto

    public string NomeProduto { get; set; } = ""; 
    // Nome copiado no momento da compra (evita mudar histórico)

    public string EmojiProduto { get; set; } = ""; 
    // Emoji copiado do produto

    public int Quantidade { get; set; } 
    // Quantidade comprada

    public decimal PrecoUnitario { get; set; } 
    // Preço no momento da compra

    public decimal Subtotal => Quantidade * PrecoUnitario; 
    // Subtotal calculado automaticamente (não salva no banco)
}


// ── DTO: Criar Pedido ────────────────────────────────────────
public record CriarPedidoDto(
    string? NomeCliente, // Nome do cliente
    string? Telefone,    // Telefone
    string? Observacao,  // Observações
    List<ItemPedidoDto> Itens // Lista de itens
);


// ── DTO: Item do Pedido (entrada) ────────────────────────────
public record ItemPedidoDto(
    int ProdutoId, // ID do produto
    int Quantidade // Quantidade desejada
);


// ── DTO: Atualizar Status ────────────────────────────────────
public record AtualizarStatusDto(
    string Status // Novo status do pedido
);