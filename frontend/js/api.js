window.NoorLocatorApi = (() => {
    const apiBaseUrl = document.body?.dataset.apiBaseUrl ?? "";

    async function request(path, options = {}) {
        const response = await fetch(`${apiBaseUrl}${path}`, {
            headers: {
                "Content-Type": "application/json",
                ...(options.headers || {})
            },
            ...options
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
        register(payload) {
            return request("/api/auth/register", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        }
    };
})();
