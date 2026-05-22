# 🍩 Rosquinha do Neguin — Sistema Completo

Backend **ASP.NET Core 8** + Banco **SQL Server** + Frontend **HTML/CSS/JS**.

---

## 🔑 Acesso Administrativo (Novo!)

O sistema agora possui controle de acesso para a área administrativa.
- **URL:** `http://localhost:5000/admin.html`
- **Usuário:** `admin@admin.local`
- **Senha:** `1234`

> **Nota:** Este usuário é criado automaticamente via Migration/Seed.

---

## 🐳 Executando com Docker (Recomendado)

O projeto está pronto para rodar em containers, facilitando a configuração do SQL Server.

1. **Certifique-se de ter o Docker Desktop instalado.**
2. **Abra o terminal na pasta raiz do projeto.**
3. **Suba os serviços:**
   ```bash
   docker-compose up -d --build
   ```
4. **O que acontece agora?**
   - Um container com **SQL Server 2022** será iniciado.
   - O backend será compilado e iniciado na porta **5000**.
   - As migrações do banco de dados serão aplicadas automaticamente no início.
5. **Acesse:** `http://localhost:5000`

---

## 🚀 Execução Manual (Local)

### ✅ Pré-requisitos
1. **.NET 8 SDK** instalado.
```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --install-dir "$HOME/.dotnet"
```
2. **SQL Server** local ou via Docker (porta 1433).
3. Ferramenta EF Core: 
```bash
dotnet tool install --global dotnet-ef
```

### 📂 Passo 1 — Configurar o Banco de Dados
Se você estiver usando um SQL Server diferente do Docker, ajuste a `ConnectionStrings` no arquivo `appsettings.json`.

### 💻 Passo 2 — Aplicar Migrations
Abra o terminal na pasta do projeto e execute:

   ```bash
   docker-compose up -d --build
   ```
**Atualizaer o banco:**
```bash
dotnet ef database update
```
*Isso criará as tabelas e fará o seed inicial de produtos e do usuário admin.*

### ▶️ Passo 3 — Rodar o Projeto
```bash
dotnet run

se tiver com a porta ocupada rodar 

dotnet run -- --urls "http://localhost:5005"

```

---

## 📁 Estrutura do Projeto
... (manter estrutura anterior, mas atualizar caminhos se necessário) ...

---

### 🌐 Passo 5 — Acessar o site

| Página | URL |
|--------|-----|
| 🏠 Início | http://localhost:5000 |
| 🍩 Cardápio | http://localhost:5000/cardapio.html |
| 🛒 Meu Pedido | http://localhost:5000/pedido.html |
| ⚙️ Admin | http://localhost:5000/admin.html |
| 📖 API Docs | http://localhost:5000/swagger |

---

### 🛑 Parar o servidor

Pressione **Ctrl + C** no terminal.

---

## ⚙️ Personalizações

### 📱 Trocar o número do WhatsApp

Abra `/wwwroot/js/config.js` e edite:
```js
const CONFIG = {
  whatsapp: '5511999999999',  // ← só números, com código do país (55 = Brasil)
  nomeLoja: 'Rosquinha do Neguin',
};
```

Exemplo: `5511987654321` (55 + DDD + número)

### 🗄️ Resetar o banco de dados

1. Pare o servidor (Ctrl + C)
2. Delete o arquivo `rosquinha.db`
3. Execute `dotnet run` — banco recriado com os produtos do seed

---

## 🔌 API Endpoints

Documentação interativa: **http://localhost:5000/swagger**

### Produtos
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/produtos` | Lista todos |
| GET | `/api/produtos?categoria=churros` | Filtra por categoria |
| POST | `/api/produtos` | Cria produto |
| PUT | `/api/produtos/{id}` | Edita produto |
| DELETE | `/api/produtos/{id}` | Desativa produto |

**Categorias:** `churros` · `rosquinhas` · `salgados` · `bebidas` · `combos`

### Pedidos
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/pedidos` | Lista todos |
| GET | `/api/pedidos?status=pendente` | Filtra por status |
| POST | `/api/pedidos` | Cria pedido |
| PATCH | `/api/pedidos/{id}/status` | Atualiza status |
| DELETE | `/api/pedidos/{id}` | Exclui pedido |
| GET | `/api/pedidos/stats` | Estatísticas do painel |

**Status:** `pendente` · `confirmado` · `preparo` · `entregue` · `cancelado`

---

## ❓ Problemas Comuns

**"dotnet não é reconhecido"**
→ Reinicie o PC após instalar o .NET SDK.

**"Porta 5000 já está em uso"**
→ Execute: `dotnet run --urls http://localhost:5001`

**"Erro na migration"**
→ Delete `rosquinha.db` e execute `dotnet run` novamente.

**WhatsApp não abre**
→ Verifique o número em `config.js` — deve ter código do país (55 para Brasil), sem traços ou espaços.

---

© 2026 Rosquinha do Neguin · Todos os direitos reservados
