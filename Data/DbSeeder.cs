using Microsoft.EntityFrameworkCore;
using RosquinhaNeguin.Models;

namespace RosquinhaNeguin.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. Garantir Usuários (Admins e Clientes de Teste)
        var defaultPasswordHash = BCrypt.Net.BCrypt.HashPassword("1234");

        var usuariosParaGarantir = new[]
        {
            new { Email = "admin@admin.com", Nome = "Administrador", Role = "Admin" },
            new { Email = "admin@admin.local", Nome = "Administrador Local", Role = "Admin" },
            new { Email = "ivan.ltds@gmail.com", Nome = "Ivan LTDS", Role = "Admin" },
            new { Email = "ivanltds@gmail.com", Nome = "Ivan LTDS", Role = "Admin" },
            new { Email = "maria.oliveira@gmail.com", Nome = "Maria Oliveira", Role = "Cliente" },
            new { Email = "carlos.souza@gmail.com", Nome = "Carlos Souza", Role = "Cliente" },
            new { Email = "cliente.teste@gmail.com", Nome = "Cliente Teste", Role = "Cliente" }
        };

        foreach (var u in usuariosParaGarantir)
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Email.ToLower() == u.Email.ToLower());
            if (user == null)
            {
                db.Usuarios.Add(new Usuario
                {
                    Nome = u.Nome,
                    Email = u.Email,
                    SenhaHash = defaultPasswordHash,
                    Role = u.Role
                });
            }
            else
            {
                if (u.Role == "Admin") user.Role = "Admin";
            }
        }
        await db.SaveChangesAsync();

        // 2. Garantir Cupons de Desconto Padrão
        var cuponsPadrão = new[]
        {
            new Cupom { Codigo = "NEGUIN10", Tipo = "Porcentagem", Valor = 10, ValorMinimoPedido = 0, Ativo = true },
            new Cupom { Codigo = "BEMVINDO", Tipo = "Fixo", Valor = 5, ValorMinimoPedido = 20, Ativo = true },
            new Cupom { Codigo = "FESTA50", Tipo = "Fixo", Valor = 15, ValorMinimoPedido = 50, Ativo = true },
            new Cupom { Codigo = "DESCONTO20", Tipo = "Porcentagem", Valor = 20, ValorMinimoPedido = 30, Ativo = true }
        };

        foreach (var c in cuponsPadrão)
        {
            if (!await db.Cupons.AnyAsync(x => x.Codigo.ToUpper() == c.Codigo.ToUpper()))
            {
                db.Cupons.Add(c);
            }
        }
        await db.SaveChangesAsync();

        // 3. Garantir Massa de Testes de Pedidos Reais (se houver menos de 3 pedidos)
        if (await db.Pedidos.CountAsync() < 3)
        {
            var maria = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "maria.oliveira@gmail.com");
            var carlos = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "carlos.souza@gmail.com");
            var clienteTeste = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == "cliente.teste@gmail.com");

            var churrosTrad = await db.Produtos.FirstOrDefaultAsync(p => p.Id == 1);
            var rosquinhaChoc = await db.Produtos.FirstOrDefaultAsync(p => p.Id == 8);
            var comboFamilia = await db.Produtos.FirstOrDefaultAsync(p => p.Id == 25);
            var cafe = await db.Produtos.FirstOrDefaultAsync(p => p.Id == 18);
            var coxinha = await db.Produtos.FirstOrDefaultAsync(p => p.Id == 13);
            var refri = await db.Produtos.FirstOrDefaultAsync(p => p.Id == 20);

            // Pedido 1: Maria Oliveira - Entregue - PIX
            if (churrosTrad != null && rosquinhaChoc != null)
            {
                var p1 = new Pedido
                {
                    UsuarioId = maria?.Id,
                    NomeCliente = "Maria Oliveira",
                    Telefone = "(11) 98765-4321",
                    Observacao = "Entregar na portaria | Pedido Exemplo 1",
                    Cep = "01001-000",
                    Logradouro = "Praça da Sé",
                    Numero = "100",
                    Complemento = "Apto 12",
                    Bairro = "Sé",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    TaxaEntrega = 5.00m,
                    CupomCodigo = "NEGUIN10",
                    Subtotal = 28.00m,
                    Desconto = 2.80m,
                    Total = 30.20m,
                    MetodoPagamento = "Pix",
                    Status = "entregue",
                    CriadoEm = DateTime.UtcNow.AddDays(-2)
                };
                p1.Itens.Add(new ItemPedido { ProdutoId = churrosTrad.Id, NomeProduto = churrosTrad.Nome, EmojiProduto = churrosTrad.Emoji, Quantidade = 2, PrecoUnitario = churrosTrad.Preco });
                p1.Itens.Add(new ItemPedido { ProdutoId = rosquinhaChoc.Id, NomeProduto = rosquinhaChoc.Nome, EmojiProduto = rosquinhaChoc.Emoji, Quantidade = 2, PrecoUnitario = rosquinhaChoc.Preco });
                db.Pedidos.Add(p1);
            }

            // Pedido 2: Carlos Souza - Preparo - Cartão
            if (comboFamilia != null && cafe != null)
            {
                var p2 = new Pedido
                {
                    UsuarioId = carlos?.Id,
                    NomeCliente = "Carlos Souza",
                    Telefone = "(11) 97777-8888",
                    Observacao = "Sem açúcar no café | Pedido Exemplo 2",
                    Cep = "01310-100",
                    Logradouro = "Av Paulista",
                    Numero = "1000",
                    Complemento = "Conj 50",
                    Bairro = "Bela Vista",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    TaxaEntrega = 5.00m,
                    CupomCodigo = "FESTA50",
                    Subtotal = 78.00m,
                    Desconto = 15.00m,
                    Total = 68.00m,
                    MetodoPagamento = "Cartão",
                    Status = "preparo",
                    CriadoEm = DateTime.UtcNow.AddDays(-1)
                };
                p2.Itens.Add(new ItemPedido { ProdutoId = comboFamilia.Id, NomeProduto = comboFamilia.Nome, EmojiProduto = comboFamilia.Emoji, Quantidade = 1, PrecoUnitario = comboFamilia.Preco });
                p2.Itens.Add(new ItemPedido { ProdutoId = cafe.Id, NomeProduto = cafe.Nome, EmojiProduto = cafe.Emoji, Quantidade = 2, PrecoUnitario = cafe.Preco });
                db.Pedidos.Add(p2);
            }

            // Pedido 3: Maria Oliveira - Confirmado - Dinheiro
            if (coxinha != null && refri != null)
            {
                var p3 = new Pedido
                {
                    UsuarioId = maria?.Id,
                    NomeCliente = "Maria Oliveira",
                    Telefone = "(11) 98765-4321",
                    Observacao = "Precisa de troco para R$ 50,00",
                    Cep = "01001-000",
                    Logradouro = "Praça da Sé",
                    Numero = "100",
                    Complemento = "Apto 12",
                    Bairro = "Sé",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    TaxaEntrega = 5.00m,
                    CupomCodigo = "BEMVINDO",
                    Subtotal = 30.00m,
                    Desconto = 5.00m,
                    Total = 30.00m,
                    MetodoPagamento = "Dinheiro",
                    Status = "confirmado",
                    CriadoEm = DateTime.UtcNow.AddHours(-3)
                };
                p3.Itens.Add(new ItemPedido { ProdutoId = coxinha.Id, NomeProduto = coxinha.Nome, EmojiProduto = coxinha.Emoji, Quantidade = 3, PrecoUnitario = coxinha.Preco });
                p3.Itens.Add(new ItemPedido { ProdutoId = refri.Id, NomeProduto = refri.Nome, EmojiProduto = refri.Emoji, Quantidade = 2, PrecoUnitario = refri.Preco });
                db.Pedidos.Add(p3);
            }

            // Pedido 4: Cliente Teste - Pendente - Pix
            if (churrosTrad != null)
            {
                var p4 = new Pedido
                {
                    UsuarioId = clienteTeste?.Id,
                    NomeCliente = "Cliente Teste",
                    Telefone = "(11) 91111-2222",
                    Observacao = "Pedido em processamento Pix",
                    Cep = "05426-100",
                    Logradouro = "Av Brigadeiro Faria Lima",
                    Numero = "1500",
                    Bairro = "Pinheiros",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    TaxaEntrega = 5.00m,
                    Subtotal = 16.00m,
                    Desconto = 0.00m,
                    Total = 21.00m,
                    MetodoPagamento = "Pix",
                    Status = "pendente",
                    CriadoEm = DateTime.UtcNow.AddMinutes(-30)
                };
                p4.Itens.Add(new ItemPedido { ProdutoId = churrosTrad.Id, NomeProduto = churrosTrad.Nome, EmojiProduto = churrosTrad.Emoji, Quantidade = 2, PrecoUnitario = churrosTrad.Preco });
                db.Pedidos.Add(p4);
            }

            await db.SaveChangesAsync();
        }
    }
}
