window.NoorLocatorConfig = (() => {
    const apiBaseUrlStorageKey = "noorlocator.api.baseUrl";

    function normalizeBaseUrl(value) {
        if (!value) {
            return "";
        }

        return String(value).trim().replace(/\/+$/, "");
    }

    function readStoredBaseUrl() {
        try {
            return normalizeBaseUrl(localStorage.getItem(apiBaseUrlStorageKey));
        } catch {
            return "";
        }
    }

    function rememberApiBaseUrl(value) {
        const normalized = normalizeBaseUrl(value);
        if (!normalized) {
            return;
        }

        try {
            localStorage.setItem(apiBaseUrlStorageKey, normalized);
        } catch {
            // Ignore storage failures and continue using the resolved base URL in memory.
        }
    }

    function getConfiguredRuntimeBaseUrl() {
        return normalizeBaseUrl(window.NoorLocatorRuntimeConfig?.apiBaseUrl);
    }

    function getSameOriginCandidate() {
        if (!window.location || !/^https?:$/i.test(window.location.protocol)) {
            return "";
        }

        return normalizeBaseUrl(window.location.origin);
    }

    function getApiBaseCandidates() {
        const runtimeConfigured = getConfiguredRuntimeBaseUrl();
        const configured = normalizeBaseUrl(document.body?.dataset.apiBaseUrl);
        const sameOrigin = getSameOriginCandidate();
        const stored = readStoredBaseUrl();

        return [
            runtimeConfigured,
            configured,
            sameOrigin,
            stored
        ].filter((value, index, values) => value && values.indexOf(value) === index);
    }

    function buildApiUrl(baseUrl, path) {
        return `${normalizeBaseUrl(baseUrl)}${path}`;
    }

    function shouldTryNextBase(response, contentType) {
        return response.status === 404 && !contentType.includes("application/json");
    }

    async function fetchApi(path, options = {}) {
        const candidates = getApiBaseCandidates();
        let lastError = null;

        for (const baseUrl of candidates) {
            try {
                const response = await fetch(buildApiUrl(baseUrl, path), options);
                const contentType = response.headers.get("content-type") || "";

                if (response.ok || !shouldTryNextBase(response, contentType)) {
                    rememberApiBaseUrl(baseUrl);
                    return { response, baseUrl, contentType };
                }
            } catch (error) {
                lastError = error;
            }
        }

        throw lastError ?? new Error("NoorLocator could not reach the API.");
    }

    async function resolveApiBaseUrl() {
        const fetchResult = await fetchApi("/api/health/ping", {
            cache: "no-store"
        });

        rememberApiBaseUrl(fetchResult.baseUrl);
        return fetchResult.baseUrl;
    }

    return {
        buildApiUrl,
        fetchApi,
        getApiBaseCandidates,
        rememberApiBaseUrl,
        resolveApiBaseUrl
    };
})();
