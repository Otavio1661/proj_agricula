// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const url = new URL(event.request.url);
    if (event.request.method !== 'GET') return fetch(event.request);
    // API: network-first, com fallback para a última resposta em cache (dados somente leitura offline)
    if (url.origin === self.location.origin && url.pathname.startsWith('/api/')) {
        const apiCache = await caches.open('agro-api-v1');
        try {
            const resp = await fetch(event.request);
            if (resp.ok) apiCache.put(event.request, resp.clone());
            return resp;
        } catch (e) {
            const hit = await apiCache.match(event.request);
            return hit || new Response(JSON.stringify({ erro: 'offline' }), { status: 503, headers: { 'Content-Type': 'application/json' } });
        }
    }
    // Uploads e CDNs: cache sob demanda
    if (url.pathname.startsWith('/uploads/') || url.hostname.endsWith('unpkg.com') || url.hostname.endsWith('arcgisonline.com') || url.hostname.endsWith('openstreetmap.org')) {
        const c = await caches.open('agro-ext-v1');
        const hit = await c.match(event.request);
        if (hit) return hit;
        try { const r = await fetch(event.request); if (r.ok || r.type === 'opaque') c.put(event.request, r.clone()); return r; } catch (e) { return hit || Response.error(); }
    }
    const shouldServeIndexHtml = event.request.mode === 'navigate' && !manifestUrlList.some(u => u === event.request.url);
    const request = shouldServeIndexHtml ? 'index.html' : event.request;
    const cache = await caches.open(cacheName);
    const cached = await cache.match(request);
    return cached || fetch(event.request);
}
