# 🍩 Rosquinha do Neguin — Sistema Completo

Backend **ASP.NET Core 8** + Banco **SQLite** + Frontend **HTML/CSS/JS** em 4 páginas separadas.

---

## 📁 Estrutura do Projeto

```
RosquinhaNeguin/
├── Controllers/
│   ├── ProdutosController.cs     ← API REST de produtos (com validação)
│   └── PedidosController.cs      ← API REST de pedidos + stats
├── Data/
│   └── AppDbContext.cs           ← EF Core + SQLite + seed dos 32 produtos
├── Models/
│   └── Models.cs                 ← Entidades + DTOs
├── Migrations/                   ← Migration inicial (auto-aplicada)
├── wwwroot/
│   ├── index.html                ← Página inicial: Hero + Sobre + Stats
│   ├── cardapio.html             ← Cardápio completo + Combos (busca da API)
│   ├── pedido.html               ← Carrinho + Checkout + Histórico
│   ├── admin.html                ← Painel admin: dashboard, pedidos, produtos
│   ├── 404.html                  ← Página de erro estilizada
│   ├── css/shared.css            ← CSS compartilhado
│   └── js/
│       ├── config.js             ← ⚙️ Configurações (WhatsApp, API) + helpers
│       └── shared.js             ← Alias de compatibilidade
├── Program.cs                    ← Entry point: CORS, Swagger, auto-migrate
├── appsettings.json              ← String de conexão e configurações
├── appsettings.Development.json  ← Config de desenvolvimento (logs detalhados)
└── RosquinhaNeguin.csproj        ← Pacotes NuGet
```

---

## 🚀 Passo a Passo para Executar no seu PC

### ✅ Pré-requisito 1 — Instalar o .NET 8 SDK

1. Acesse: **https://dotnet.microsoft.com/download/dotnet/8.0**
2. Baixe o **.NET 8 SDK** para o seu sistema (Windows / Mac / Linux)
3. Execute o instalador
4. Abra o **Prompt de Comando** e verifique:
   ```
   dotnet --version
   ```
   Deve aparecer algo como `8.0.xxx`

---

### 📂 Passo 1 — Organizar a pasta

Coloque a pasta `RosquinhaNeguin` em algum lugar do seu PC, por exemplo:
```
C:\Projetos\RosquinhaNeguin\
```

---

### 💻 Passo 2 — Abrir o terminal na pasta do projeto

**Windows:** No Explorador de Arquivos, clique na barra de endereço, digite `cmd` e Enter.

**Mac/Linux:**
```bash
cd ~/Projetos/RosquinhaNeguin
```

---

### 📦 Passo 3 — Restaurar os pacotes (só na primeira vez)

```bash
dotnet restore
```

---

### ▶️ Passo 4 — Executar o projeto

```bash
dotnet run
```

O banco `rosquinha.db` é criado automaticamente com todos os 32 produtos já cadastrados!

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
