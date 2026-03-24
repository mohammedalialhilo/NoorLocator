window.NoorLocatorConfig = (() => {
    const apiBaseUrlStorageKey = "noorlocator.api.baseUrl";
    const frontendOnlyPorts = new Set(["5500", "5501", "3000", "4173"]);

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

    function getSameOriginCandidate() {
        if (!window.location || !/^https?:$/i.test(window.location.protocol)) {
            return "";
        }

        if (frontendOnlyPorts.has(window.location.port || "")) {
            return "";
        }

        return normalizeBaseUrl(window.location.origin);
    }

    function getApiBaseCandidates() {
        const configured = normalizeBaseUrl(document.body?.dataset.apiBaseUrl);
        const stored = readStoredBaseUrl();
        const sameOrigin = getSameOriginCandidate();

        return [
            configured,
            stored,
            sameOrigin,
            "http://localhost:5141",
            "https://localhost:7132",
            "http://127.0.0.1:5141",
            "https://127.0.0.1:7132"
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

    return {
        buildApiUrl,
        fetchApi,
        getApiBaseCandidates,
        rememberApiBaseUrl
    };
})();
