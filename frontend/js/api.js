window.NoorLocatorApi = (() => {
    const apiBaseUrl = document.body?.dataset.apiBaseUrl ?? "";

    async function request(path, options = {}) {
        const headers = new Headers(options.headers || {});
        const token = window.NoorLocatorAuth?.getToken?.();

        if (options.body && !headers.has("Content-Type")) {
            headers.set("Content-Type", "application/json");
        }

        if (token && !headers.has("Authorization")) {
            headers.set("Authorization", `Bearer ${token}`);
        }

        const response = await fetch(`${apiBaseUrl}${path}`, {
            ...options,
            headers
        });

        const contentType = response.headers.get("content-type") || "";
        const payload = contentType.includes("application/json") ? await response.json() : null;
        const normalized = {
            success: payload?.success ?? response.ok,
            message: payload?.message ?? (response.ok ? "Request completed." : `Request failed with status ${response.status}.`),
            data: payload?.data ?? null,
            status: response.status
        };

        if (!response.ok) {
            throw normalized;
        }

        return normalized;
    }

    return {
        getHealth() {
            return request("/api/health");
        },
        getCenters() {
            return request("/api/centers");
        },
        getCenter(id) {
            return request(`/api/centers/${id}`);
        },
        getMajalis(centerId) {
            const query = centerId ? `?centerId=${encodeURIComponent(centerId)}` : "";
            return request(`/api/majalis${query}`);
        },
        login(payload) {
            return request("/api/auth/login", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        },
        me() {
            return request("/api/auth/me");
        },
        register(payload) {
            return request("/api/auth/register", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        }
    };
})();
