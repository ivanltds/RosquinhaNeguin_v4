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

document.addEventListener('DOMContentLoaded', () => {
    initHeroSlider();
    updateNavAuth();
});

// Alias para compatibilidade
const obs = typeof revealObs !== 'undefined' ? revealObs : null;
