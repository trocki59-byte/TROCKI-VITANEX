// TROÇKİ VİTANEX — çevrimdışı çalışma için önbellek
const CACHE = 'trocki-vitanex-v3';
const IMGCACHE = 'trocki-vitanex-img';
const FILES = ['./', './index.html', './manifest.webmanifest', './icon-192.png', './icon-512.png', './apple-touch-icon.png'];

self.addEventListener('install', e => {
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(FILES)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', e => {
  e.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(k => k !== CACHE && k !== IMGCACHE).map(k => caches.delete(k))))
    .then(() => self.clients.claim()));
});

// Önce internetten dene (güncel sürüm), olmazsa önbellekten aç
self.addEventListener('fetch', e => {
  if (e.request.method !== 'GET') return;
  // Bitki fotoğrafları: önce önbellek (çevrimdışı da görünür)
  if (e.request.url.startsWith('https://upload.wikimedia.org/')) {
    e.respondWith(caches.open(IMGCACHE).then(c => c.match(e.request).then(hit => hit || fetch(e.request).then(r => { c.put(e.request, r.clone()); return r; }))));
    return;
  }
  e.respondWith(
    fetch(e.request).then(r => {
      const copy = r.clone();
      caches.open(CACHE).then(c => c.put(e.request, copy));
      return r;
    }).catch(() => caches.match(e.request).then(r => r || caches.match('./index.html')))
  );
});
