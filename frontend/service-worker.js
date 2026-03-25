const CACHE_NAME = "noorlocator-shell-v5";
const SHELL_ASSETS = [
    "/",
    "/about",
    "/index.html",
    "/about.html",
    "/centers.html",
    "/center-details.html",
    "/login.html",
    "/register.html",
    "/css/style.css",
    "/js/auth.js",
    "/js/api.js",
    "/js/layout.js",
    "/js/app.js",
    "/assets/logo.svg",
    "/assets/favicon.svg",
    "/assets/center-photo-placeholder.svg",
    "/site.webmanifest"
];
const NON_CACHEABLE_PATHS = new Set([
    "/dashboard.html",
    "/manager.html",
    "/admin.html",
    "/logout.html"
]);

self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(SHELL_ASSETS))
            .then(() => self.skipWaiting()));
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys
                .filter(key => key !== CACHE_NAME)
                .map(key => caches.delete(key))))
            .then(() => self.clients.claim()));
});

self.addEventListener("fetch", event => {
    if (event.request.method !== "GET") {
        return;
    }

    const requestUrl = new URL(event.request.url);
    if (requestUrl.pathname.startsWith("/api/") || NON_CACHEABLE_PATHS.has(requestUrl.pathname)) {
        return;
    }

    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            if (cachedResponse) {
                return cachedResponse;
            }

            return fetch(event.request).then(networkResponse => {
                if (!networkResponse || networkResponse.status !== 200 || networkResponse.type !== "basic") {
                    return networkResponse;
                }

                const responseClone = networkResponse.clone();
                caches.open(CACHE_NAME).then(cache => cache.put(event.request, responseClone));
                return networkResponse;
            });
        }));
});
