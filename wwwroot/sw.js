// Service Worker - Rosquinha do Neguin PWA
const CACHE_NAME = 'rosquinha-v1';
const ASSETS_TO_CACHE = [
  '/',
  '/index.html',
  '/cardapio.html',
  '/pedido.html',
  '/meus-pedidos.html',
  '/login.html',
  '/css/shared.css',
  '/js/config.js',
  '/js/shared.js',
  '/img/logo-200.png',
  '/img/logo-circle.png',
  '/manifest.json'
];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      console.log('[Service Worker] Pré-carregando arquivos essenciais para mobile');
      return cache.addAll(ASSETS_TO_CACHE);
    })
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))
      );
    })
  );
  self.clients.claim();
});

// Estratégia: Network First com fallback para Cache para dados da API, Cache First para estáticos
self.addEventListener('fetch', (event) => {
  const url = new URL(event.request.url);

  // Se for chamada de API, não fazemos cache estático para manter sempre atualizado
  if (url.pathname.startsWith('/api/')) {
    event.respondWith(fetch(event.request));
    return;
  }

  // Para páginas e assets estáticos: Network first com cache fallback
  event.respondWith(
    fetch(event.request)
      .then((response) => {
        if (response && response.status === 200) {
          const responseClone = response.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, responseClone);
          });
        }
        return response;
      })
      .catch(() => caches.match(event.request))
  );
});
