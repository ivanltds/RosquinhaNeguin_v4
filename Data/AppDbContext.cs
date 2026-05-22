using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Models;

namespace RosquinhaNeguin.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<LogLogin> LogsLogin => Set<LogLogin>();
    public DbSet<CartaoCredito> CartoesCredito => Set<CartaoCredito>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Precisão decimal
        mb.Entity<Produto>().Property(p => p.Preco).HasPrecision(18, 2);
        mb.Entity<Pedido>().Property(p => p.Total).HasPrecision(18, 2);
        mb.Entity<ItemPedido>().Property(p => p.PrecoUnitario).HasPrecision(18, 2);
        mb.Entity<MovimentacaoEstoque>().Property(m => m.Quantidade).HasPrecision(18, 2);

        // Índices
        mb.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Pedido>().HasIndex(p => p.Status);
        mb.Entity<Produto>().HasIndex(p => p.Nome);

        // Relacionamentos
        mb.Entity<Produto>()
            .HasOne(p => p.CategoriaObjeto)
            .WithMany()
            .HasForeignKey(p => p.CategoriaId);

        // Seed de Categorias
        mb.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nome = "churros" },
            new Categoria { Id = 2, Nome = "rosquinhas" },
            new Categoria { Id = 3, Nome = "salgados" },
            new Categoria { Id = 4, Nome = "bebidas" },
            new Categoria { Id = 5, Nome = "combos" }
        );

        // Seed de Produtos (Atualizado com CategoriaId e Estoque)
        // Nota: Mantendo IDs originais para compatibilidade
        mb.Entity<Produto>().HasData(
            new Produto { Id=1,  Nome="Churros Tradicional",    Descricao="Crocante por fora, macio por dentro",   Emoji="🍩", Preco=8,  CategoriaId=1, Estoque=50 },
            new Produto { Id=2,  Nome="Churros Doce de Leite",  Descricao="Recheado com doce de leite cremoso",    Emoji="🍮", Preco=10, CategoriaId=1, Estoque=50 },
            new Produto { Id=3,  Nome="Churros Chocolate",      Descricao="Recheio de chocolate belga",            Emoji="🍫", Preco=10, CategoriaId=1, Estoque=50 },
            new Produto { Id=4,  Nome="Churros Nutella",        Descricao="Generoso recheio de Nutella",           Emoji="🌰", Preco=15, CategoriaId=1, Estoque=50 },
            new Produto { Id=5,  Nome="Churros Leite Ninho",    Descricao="Cremoso e irresistível",                Emoji="🥛", Preco=15, CategoriaId=1, Estoque=50 },
            new Produto { Id=6,  Nome="Churros Morango",        Descricao="Geleia artesanal de morango",           Emoji="🍓", Preco=14, CategoriaId=1, Estoque=50 },
            new Produto { Id=7,  Nome="Rosquinha Tradicional",  Descricao="Receita original da casa",              Emoji="🍩", Preco=5,  CategoriaId=2, Estoque=50 },
            new Produto { Id=8,  Nome="Rosquinha Chocolate",    Descricao="Com cobertura de chocolate",            Emoji="🍫", Preco=6,  CategoriaId=2, Estoque=50 },
            new Produto { Id=9,  Nome="Rosquinha Doce de Leite",Descricao="Recheada e coberta",                   Emoji="🍮", Preco=6,  CategoriaId=2, Estoque=50 },
            new Produto { Id=10, Nome="Rosquinha Nutella",      Descricao="Premium e indulgente",                  Emoji="🌰", Preco=8,  CategoriaId=2, Estoque=50 },
            new Produto { Id=11, Nome="Rosquinha Ninho",        Descricao="Leite em pó Ninho cremoso",             Emoji="🥛", Preco=8,  CategoriaId=2, Estoque=50 },
            new Produto { Id=12, Nome="Rosquinha Morango",      Descricao="Fresco e delicado",                     Emoji="🍓", Preco=8,  CategoriaId=2, Estoque=50 },
            new Produto { Id=13, Nome="Coxinha",                Descricao="Frango desfiado temperado",             Emoji="🍗", Preco=6,  CategoriaId=3, Estoque=50 },
            new Produto { Id=14, Nome="Risole",                 Descricao="Massa fina e recheio especial",         Emoji="🥟", Preco=6,  CategoriaId=3, Estoque=50 },
            new Produto { Id=15, Nome="Empada",                 Descricao="Massa amanteigada artesanal",           Emoji="🥧", Preco=7,  CategoriaId=3, Estoque=50 },
            new Produto { Id=16, Nome="Esfiha",                 Descricao="Aberta, no estilo árabe",               Emoji="🫓", Preco=6,  CategoriaId=3, Estoque=50 },
            new Produto { Id=17, Nome="Pão de Queijo",          Descricao="Quentinho e puxento",                   Emoji="🧀", Preco=5,  CategoriaId=3, Estoque=50 },
            new Produto { Id=18, Nome="Café",                   Descricao="Grãos selecionados",                    Emoji="☕", Preco=4,  CategoriaId=4, Estoque=50 },
            new Produto { Id=19, Nome="Café com Leite",         Descricao="Cremoso e reconfortante",               Emoji="🥛", Preco=5,  CategoriaId=4, Estoque=50 },
            new Produto { Id=20, Nome="Refrigerante",           Descricao="Gelado e refrescante",                  Emoji="🥤", Preco=6,  CategoriaId=4, Estoque=50 },
            new Produto { Id=21, Nome="Suco Natural",           Descricao="Frutas frescas da estação",             Emoji="🍊", Preco=7,  CategoriaId=4, Estoque=50 },
            new Produto { Id=22, Nome="Água Mineral",           Descricao="Com ou sem gás",                        Emoji="💧", Preco=3,  CategoriaId=4, Estoque=50 },
            new Produto { Id=23, Nome="Combo 3 Churros",        Descricao="3 Churros Tradicionais",                Emoji="🍩", Preco=25, CategoriaId=5, Estoque=50 },
            new Produto { Id=24, Nome="Combo 5 Churros",        Descricao="5 Churros Tradicionais",                Emoji="🍩", Preco=40, CategoriaId=5, Estoque=50 },
            new Produto { Id=25, Nome="Combo Família",          Descricao="10 Churros",                            Emoji="👨‍👩‍👧‍👦", Preco=70, CategoriaId=5, Estoque=50 },
            new Produto { Id=26, Nome="Combo Casal",            Descricao="2 Churros + 2 Rosquinhas",              Emoji="💑", Preco=22, CategoriaId=5, Estoque=50 },
            new Produto { Id=27, Nome="Combo Café da Tarde",    Descricao="2 Rosquinhas + Café",                   Emoji="☕", Preco=12, CategoriaId=5, Estoque=50 },
            new Produto { Id=28, Nome="Combo Doces",            Descricao="1 Churros + 1 Donut",                   Emoji="🍬", Preco=16, CategoriaId=5, Estoque=50 },
            new Produto { Id=29, Nome="Combo Festa",            Descricao="15 Mini Churros",                       Emoji="🎉", Preco=60, CategoriaId=5, Estoque=50 },
            new Produto { Id=30, Nome="Combo Amigos",           Descricao="4 Churros + 4 Rosquinhas",              Emoji="👯", Preco=35, CategoriaId=5, Estoque=50 },
            new Produto { Id=31, Nome="Combo Lanche",           Descricao="Salgado + Rosquinha + Refri",           Emoji="🥙", Preco=18, CategoriaId=5, Estoque=50 },
            new Produto { Id=32, Nome="Combo Completo",         Descricao="2 Churros + 2 Salgados + 2 Bebidas",   Emoji="⭐", Preco=30, CategoriaId=5, Estoque=50 }
        );

        // Relacionamentos e índices para CartaoCredito
        mb.Entity<CartaoCredito>()
            .HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<Pedido>()
            .HasOne(p => p.Cartao)
            .WithMany()
            .HasForeignKey(p => p.CartaoId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed de Configurações
        mb.Entity<Configuracao>().HasData(
            new Configuracao { Id = 1, Chave = "PixKey", Valor = "5511999999999" },
            new Configuracao { Id = 2, Chave = "PixBeneficiario", Valor = "Rosquinha do Neguin Ltda" },
            new Configuracao { Id = 3, Chave = "PixCidade", Valor = "Sao Paulo" }
        );
    }
}
