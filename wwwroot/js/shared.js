// shared.js — Lógica compartilhada de UI e Autenticação

function toggleMenu() {
    const links = document.getElementById('navLinks');
    links.classList.toggle('active');
}

// Lógica do Slider da Hero
function initHeroSlider() {
    const slides = document.querySelectorAll('.hero-slides .slide');
    if (slides.length < 2) return;

    let current = 0;
    setInterval(() => {
        // Remove active do slide atual (inicia fade out)
        slides[current].classList.remove('active');
        
        // Próximo índice
        current = (current + 1) % slides.length;
        
        // Adiciona active ao novo slide (inicia fade in)
        slides[current].classList.add('active');
    }, 5000); // Troca a cada 5 segundos
}

// Verifica Autenticação para a Nav
function updateNavAuth() {
    const navAuth = document.getElementById('navAuth');
    const userOnlyLinks = document.querySelectorAll('.user-only');
    if (!navAuth) return;

    const userJson = localStorage.getItem('user');
    if (userJson) {
        const user = JSON.parse(userJson);
        
        // Mostra links exclusivos de usuários logados
        userOnlyLinks.forEach(el => el.style.display = 'block');

        // Se for admin, adiciona link do painel
        let adminLink = '';
        if (user.role === 'Admin') {
            adminLink = `<li><a href="/admin.html" style="color: var(--color-dourado); font-weight: 700;">⚙️ Painel Admin</a></li>`;
            // Insere antes do botão de sair se possível
            const navLinks = document.getElementById('navLinks');
            if (navLinks && !navLinks.innerHTML.includes('admin.html')) {
                const li = document.createElement('li');
                li.innerHTML = `<a href="/admin.html" style="color: var(--color-dourado); font-weight: 700;">⚙️ Admin</a>`;
                navLinks.insertBefore(li, navAuth);
            }
        }

        navAuth.innerHTML = `
            <div style="display: flex; align-items: center; gap: 15px;">
                <span style="font-weight: 600; color: var(--color-chocolate); white-space: nowrap;">Olá, ${user.nome.split(' ')[0]}</span>
                <button onclick="logout()" class="btn btn-outline" style="padding: 8px 15px; font-size: 0.8rem;">Sair</button>
            </div>
        `;
    } else {
        userOnlyLinks.forEach(el => el.style.display = 'none');
    }
}

function logout() {
    localStorage.removeItem('user');
    // Se estiver no admin, vai para o login. Se estiver em outro lugar, recarrega para atualizar a nav.
    if (window.location.pathname.includes('admin.html')) {
        window.location.href = '/login.html';
    } else {
        window.location.href = '/index.html';
    }
}

// PWA & Mobile Navigation
let deferredPrompt;

function initPwa() {
    // 1. Registrar Service Worker
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', () => {
            navigator.serviceWorker.register('/sw.js')
                .then(reg => console.log('[PWA] Service Worker registrado com sucesso! Escopo:', reg.scope))
                .catch(err => console.log('[PWA] Falha ao registrar Service Worker:', err));
        });
    }

    // 2. Interceptar prompt de instalação
    window.addEventListener('beforeinstallprompt', (e) => {
        e.preventDefault();
        deferredPrompt = e;
        showInstallButton();
    });

    window.addEventListener('appinstalled', () => {
        console.log('[PWA] Aplicativo instalado no dispositivo com sucesso!');
        hideInstallButton();
    });
}

function showInstallButton() {
    let btn = document.getElementById('pwaInstallBtn');
    if (!btn) {
        btn = document.createElement('button');
        btn.id = 'pwaInstallBtn';
        btn.className = 'pwa-install-btn';
        btn.innerHTML = '📲 <span>Instalar App</span>';
        btn.onclick = installPwa;
        document.body.appendChild(btn);
    }
    btn.style.display = 'flex';
}

function hideInstallButton() {
    const btn = document.getElementById('pwaInstallBtn');
    if (btn) btn.style.display = 'none';
}

async function installPwa() {
    if (!deferredPrompt) return;
    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    console.log('[PWA] Escolha do usuário:', outcome);
    deferredPrompt = null;
    hideInstallButton();
}

// Injeta Bottom Navigation Bar automaticamente em telas mobile
function renderMobileBottomNav() {
    if (document.querySelector('.mobile-bottom-nav')) return;

    const path = window.location.pathname;
    const isHome = path === '/' || path === '/index.html';
    const isCardapio = path.includes('cardapio.html');
    const isPedido = path.includes('pedido.html');
    const isMeusPedidos = path.includes('meus-pedidos.html');
    const isLogin = path.includes('login.html');

    const nav = document.createElement('nav');
    nav.className = 'mobile-bottom-nav';
    nav.innerHTML = `
        <a href="/" class="bottom-nav-item ${isHome ? 'active' : ''}">
            <span class="nav-icon">🏠</span>
            <span>Início</span>
        </a>
        <a href="/cardapio.html" class="bottom-nav-item ${isCardapio ? 'active' : ''}">
            <span class="nav-icon">🍩</span>
            <span>Cardápio</span>
        </a>
        <a href="/pedido.html" class="bottom-nav-item ${isPedido ? 'active' : ''}">
            <span class="nav-icon">🛒</span>
            <span>Carrinho</span>
            <span class="bottom-nav-badge cart-count" style="display: none;">0</span>
        </a>
        <a href="/meus-pedidos.html" class="bottom-nav-item ${isMeusPedidos ? 'active' : ''}">
            <span class="nav-icon">📋</span>
            <span>Pedidos</span>
        </a>
        <a href="/login.html" class="bottom-nav-item ${isLogin ? 'active' : ''}" id="bottomNavProfile">
            <span class="nav-icon">👤</span>
            <span>Conta</span>
        </a>
    `;
    document.body.appendChild(nav);

    // Atualiza contagem do carrinho na bottom bar
    if (typeof updateCartBadge === 'function') {
        updateCartBadge();
    }
}

document.addEventListener('DOMContentLoaded', () => {
    initHeroSlider();
    updateNavAuth();
    initPwa();
    renderMobileBottomNav();
});

// Alias para compatibilidade
const obs = typeof revealObs !== 'undefined' ? revealObs : null;

