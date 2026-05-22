using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RosquinhaNeguin.Migrations
{
    /// <inheritdoc />
    public partial class InicialSQLServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomeCliente = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Emoji = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preco = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedido",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    NomeProduto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmojiProduto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Produtos",
                columns: new[] { "Id", "Ativo", "Categoria", "Descricao", "Emoji", "Nome", "Preco" },
                values: new object[,]
                {
                    { 1, true, "churros", "Crocante por fora, macio por dentro", "🍩", "Churros Tradicional", 8m },
                    { 2, true, "churros", "Recheado com doce de leite cremoso", "🍮", "Churros Doce de Leite", 10m },
                    { 3, true, "churros", "Recheio de chocolate belga", "🍫", "Churros Chocolate", 10m },
                    { 4, true, "churros", "Generoso recheio de Nutella", "🌰", "Churros Nutella", 15m },
                    { 5, true, "churros", "Cremoso e irresistível", "🥛", "Churros Leite Ninho", 15m },
                    { 6, true, "churros", "Geleia artesanal de morango", "🍓", "Churros Morango", 14m },
                    { 7, true, "rosquinhas", "Receita original da casa", "🍩", "Rosquinha Tradicional", 5m },
                    { 8, true, "rosquinhas", "Com cobertura de chocolate", "🍫", "Rosquinha Chocolate", 6m },
                    { 9, true, "rosquinhas", "Recheada e coberta", "🍮", "Rosquinha Doce de Leite", 6m },
                    { 10, true, "rosquinhas", "Premium e indulgente", "🌰", "Rosquinha Nutella", 8m },
                    { 11, true, "rosquinhas", "Leite em pó Ninho cremoso", "🥛", "Rosquinha Ninho", 8m },
                    { 12, true, "rosquinhas", "Fresco e delicado", "🍓", "Rosquinha Morango", 8m },
                    { 13, true, "salgados", "Frango desfiado temperado", "🍗", "Coxinha", 6m },
                    { 14, true, "salgados", "Massa fina e recheio especial", "🥟", "Risole", 6m },
                    { 15, true, "salgados", "Massa amanteigada artesanal", "🥧", "Empada", 7m },
                    { 16, true, "salgados", "Aberta, no estilo árabe", "🫓", "Esfiha", 6m },
                    { 17, true, "salgados", "Quentinho e puxento", "🧀", "Pão de Queijo", 5m },
                    { 18, true, "bebidas", "Grãos selecionados", "☕", "Café", 4m },
                    { 19, true, "bebidas", "Cremoso e reconfortante", "🥛", "Café com Leite", 5m },
                    { 20, true, "bebidas", "Gelado e refrescante", "🥤", "Refrigerante", 6m },
                    { 21, true, "bebidas", "Frutas frescas da estação", "🍊", "Suco Natural", 7m },
                    { 22, true, "bebidas", "Com ou sem gás", "💧", "Água Mineral", 3m },
                    { 23, true, "combos", "3 Churros Tradicionais", "🍩", "Combo 3 Churros", 25m },
                    { 24, true, "combos", "5 Churros Tradicionais", "🍩", "Combo 5 Churros", 40m },
                    { 25, true, "combos", "10 Churros", "👨‍👩‍👧‍👦", "Combo Família", 70m },
                    { 26, true, "combos", "2 Churros + 2 Rosquinhas", "💑", "Combo Casal", 22m },
                    { 27, true, "combos", "2 Rosquinhas + Café", "☕", "Combo Café da Tarde", 12m },
                    { 28, true, "combos", "1 Churros + 1 Donut", "🍬", "Combo Doce", 16m },
                    { 29, true, "combos", "15 Mini Churros", "🎉", "Combo Festa", 60m },
                    { 30, true, "combos", "4 Churros + 4 Rosquinhas", "👯", "Combo Amigos", 35m },
                    { 31, true, "combos", "Salgado + Rosquinha + Refri", "🥙", "Combo Lanche", 18m },
                    { 32, true, "combos", "2 Churros + 2 Salgados + 2 Bebidas", "⭐", "Combo Completo", 30m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_PedidoId",
                table: "ItensPedido",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_ProdutoId",
                table: "ItensPedido",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensPedido");

            migrationBuilder.DropTable(
                name: "Pedidos");

            migrationBuilder.DropTable(
                name: "Produtos");
        }
    }
}
