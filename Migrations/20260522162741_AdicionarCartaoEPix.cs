using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RosquinhaNeguin.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCartaoEPix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CartaoId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetodoPagamento",
                table: "Pedidos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CartoesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    NumeroMascarado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bandeira = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomeTitular = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Validade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartoesCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartoesCredito_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Chave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Configuracoes",
                columns: new[] { "Id", "Chave", "Valor" },
                values: new object[,]
                {
                    { 1, "PixKey", "5511999999999" },
                    { 2, "PixBeneficiario", "Rosquinha do Neguin Ltda" },
                    { 3, "PixCidade", "Sao Paulo" }
                });

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 28,
                column: "Nome",
                value: "Combo Doces");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_CartaoId",
                table: "Pedidos",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CartoesCredito_UsuarioId",
                table: "CartoesCredito",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_CartoesCredito_CartaoId",
                table: "Pedidos",
                column: "CartaoId",
                principalTable: "CartoesCredito",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_CartoesCredito_CartaoId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Usuarios_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropTable(
                name: "CartoesCredito");

            migrationBuilder.DropTable(
                name: "Configuracoes");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_CartaoId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "CartaoId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "MetodoPagamento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Pedidos");

            migrationBuilder.UpdateData(
                table: "Produtos",
                keyColumn: "Id",
                keyValue: 28,
                column: "Nome",
                value: "Combo Doce");
        }
    }
}
