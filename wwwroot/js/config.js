// ════════════════════════════════════════════════
//  CONFIGURAÇÕES DO SITE — edite aqui!
// ════════════════════════════════════════════════

const CONFIG = {
  // 🔴 TROQUE pelo número real do WhatsApp (só números, com código do país)
  whatsapp: '5511999999999',

  // Nome da loja (aparece nas mensagens do WhatsApp)
  nomeLoja: 'Rosquinha do Neguin',

  // URL base da API (deixe '/' para rodar junto com o servidor C#)
  apiBase: '',
};

// ════════════════════════════════════════════════
//  CART (localStorage)
// ════════════════════════════════════════════════

const CART_KEY = 'neguin_cart';

function getCart() {
  try { return JSON.parse(localStorage.getItem(CART_KEY) || '{}'); }
  catch { return {}; }
}

function saveCart(cart) {
  localStorage.setItem(CART_KEY, JSON.stringify(cart));
  updateCartBadge();
  if (typeof updateStickyCart === 'function') updateStickyCart();
}

// Alias para compatibilidade com o HTML antigo
function addItem(id, nome, preco, emoji) {
    // Tenta capturar o evento de clique para o feedback visual
    const event = window.event;
    addToCart(id, nome, preco, emoji, event);
}

async function addToCart(id, nome, preco, emoji, evt = null) {
  const cart = getCart();
  const key = String(id);

  try {
    // Busca estoque atualizado da API
    const produto = await apiFetch('/api/produtos/' + id);
    const qtyNoCarrinho = cart[key] ? cart[key].qty : 0;

    if (produto.estoque <= qtyNoCarrinho) {
      showToast(`📦 Estoque insuficiente! Apenas ${produto.estoque} unidades de ${nome} disponíveis.`, 'warning');
      return;
    }

    if (cart[key]) cart[key].qty++;
    else cart[key] = { id, nome, preco, emoji, qty: 1 };

    saveCart(cart);

    // Feedback visual no botão
    const btn = evt?.currentTarget || (evt?.target?.closest ? evt.target.closest('button') : null);
    if (btn) {
      const originalText = btn.innerHTML;
      btn.innerHTML = '✅';
      btn.classList.add('btn-success');
      setTimeout(() => {
        btn.innerHTML = originalText;
        btn.classList.remove('btn-success');
      }, 800);
    }

    showToast(emoji + ' ' + nome + ' adicionado ao carrinho!', 'success');
  } catch (err) {
    console.error('Erro ao verificar estoque:', err);
    // Fallback: adiciona sem verificar se a API falhar
    if (cart[key]) cart[key].qty++;
    else cart[key] = { id, nome, preco, emoji, qty: 1 };
    saveCart(cart);
  }
}
function clearCart() {
  localStorage.removeItem(CART_KEY);
  updateCartBadge();
}

function cartTotal() {
  return Object.values(getCart()).reduce((s, i) => s + i.preco * i.qty, 0);
}

function cartQty() {
  return Object.values(getCart()).reduce((s, i) => s + i.qty, 0);
}

function updateCartBadge() {
  const qty = cartQty();
  const badges = document.querySelectorAll('.cart-count, #cartCount');
  badges.forEach(el => {
    el.textContent = qty;
    if (qty > 0) {
      el.style.display = 'inline-block';
      el.classList.add('bounce-animation');
      setTimeout(() => el.classList.remove('bounce-animation'), 400);
    } else {
      el.style.display = 'none';
    }
  });
}

// ════════════════════════════════════════════════
//  API HELPERS
// ════════════════════════════════════════════════

async function apiFetch(path, opts = {}) {
  try {
    const res = await fetch(CONFIG.apiBase + path, {
      headers: { 'Content-Type': 'application/json', ...opts.headers },
      credentials: 'include',
      ...opts,
    });
    
    if (!res.ok) {
      const errData = await res.json().catch(() => ({ erro: `Erro HTTP ${res.status}` }));
      const msg = errData.erro || errData.message || `Falha na requisição (${res.status})`;
      console.error(`[API Error ${res.status}] ${path}:`, msg);
      throw new Error(msg);
    }
    
    return res.status === 204 ? null : res.json();
  } catch (err) {
    console.error(`[apiFetch Connection Error] ${path}:`, err);
    throw err;
  }
}

// ════════════════════════════════════════════════
//  TOAST
// ════════════════════════════════════════════════

let _toastTimer;
function showToast(msg, tipo = 'info') {
  let el = document.getElementById('toast');
  if (!el) {
    el = document.createElement('div');
    el.id = 'toast';
    el.className = 'toast';
    document.body.appendChild(el);
  }
  el.textContent = msg;
  el.className = 'toast show tipo-' + tipo;
  clearTimeout(_toastTimer);
  _toastTimer = setTimeout(() => el.classList.remove('show'), 2800);
}

// ════════════════════════════════════════════════
//  MOEDA
// ════════════════════════════════════════════════

function formatBRL(v) {
  return 'R$\u00a0' + Number(v).toFixed(2).replace('.', ',');
}

// ════════════════════════════════════════════════
//  REVEAL ON SCROLL
// ════════════════════════════════════════════════

const revealObs = new IntersectionObserver(entries => {
  entries.forEach((e, i) => {
    if (e.isIntersecting) {
      setTimeout(() => e.target.classList.add('visible'), i * 55);
      revealObs.unobserve(e.target);
    }
  });
}, { threshold: 0.08 });

document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.reveal').forEach(el => revealObs.observe(el));
  updateCartBadge();
});
