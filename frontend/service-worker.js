const CACHE_NAME = "noorlocator-shell-v10";
const SHELL_ASSETS = [
    "./",
    "index.html",
    "about.html",
    "centers.html",
    "center-details.html",
    "login.html",
    "register.html",
    "css/style.css",
    "js/auth.js",
    "js/api.js",
    "js/layout.js",
    "js/app.js",
    "assets/logo_bkg.png",
    "assets/center-photo-placeholder.svg",
    "site.webmanifest"
];
const NON_CACHEABLE_PATHS = new Set([
    "/js/runtime-config.js",
    "/dashboard.html",
    "/profile.html",
    "/manager.html",
    "/admin.html",
    "/logout.html"
]);
const NETWORK_FIRST_EXTENSIONS = new Set([
    ".html",
    ".js",
    ".css",
    ".json",
    ".webmanifest"
]);

function shouldUseNetworkFirst(request, requestUrl) {
    if (request.mode === "navigate") {
        return true;
    }

    const pathname = (requestUrl.pathname || "").toLowerCase();
    if (!pathname || pathname === "/") {
        return true;
    }

    if (pathname.startsWith("/js/") || pathname.startsWith("/css/") || pathname.startsWith("/locales/")) {
        return true;
    }

    const extensionIndex = pathname.lastIndexOf(".");
    if (extensionIndex < 0) {
        return false;
    }

    return NETWORK_FIRST_EXTENSIONS.has(pathname.slice(extensionIndex));
}

async function fetchAndRefreshCache(request) {
    const networkResponse = await fetch(request);
    if (!networkResponse || networkResponse.status !== 200 || networkResponse.type !== "basic") {
        return networkResponse;
    }

    const responseClone = networkResponse.clone();
    const cache = await caches.open(CACHE_NAME);
    await cache.put(request, responseClone);
    return networkResponse;
}

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

    if (shouldUseNetworkFirst(event.request, requestUrl)) {
        event.respondWith((async () => {
            try {
                return await fetchAndRefreshCache(event.request);
            } catch {
                const cachedResponse = await caches.match(event.request);
                if (cachedResponse) {
                    return cachedResponse;
                }

                throw;
            }
        })());
        return;
    }

    event.respondWith(
        caches.match(event.request).then(cachedResponse => {
            if (cachedResponse) {
                return cachedResponse;
            }

            return fetchAndRefreshCache(event.request);
        }));
});
