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

    function toQueryString(params) {
        const searchParams = new URLSearchParams();

        Object.entries(params || {}).forEach(([key, value]) => {
            if (value === undefined || value === null) {
                return;
            }

            const normalized = typeof value === "string" ? value.trim() : value;
            if (normalized === "") {
                return;
            }

            searchParams.set(key, normalized);
        });

        const queryString = searchParams.toString();
        return queryString ? `?${queryString}` : "";
    }

    return {
        getHealth() {
            return request("/api/health");
        },
        getCenters(params = {}) {
            return request(`/api/centers${toQueryString(params)}`);
        },
        searchCenters(params) {
            return request(`/api/centers/search${toQueryString(params)}`);
        },
        getNearestCenters(params) {
            return request(`/api/centers/nearest${toQueryString(params)}`);
        },
        getCenter(id, params = {}) {
            return request(`/api/centers/${id}${toQueryString(params)}`);
        },
        getCenterMajalis(id) {
            return request(`/api/centers/${id}/majalis`);
        },
        getCenterLanguages(id) {
            return request(`/api/centers/${id}/languages`);
        },
        getLanguages() {
            return request("/api/languages");
        },
        createCenterRequest(payload) {
            return request("/api/center-requests", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        },
        getMyCenterRequests() {
            return request("/api/center-requests/my");
        },
        createSuggestion(payload) {
            return request("/api/suggestions", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        },
        createCenterLanguageSuggestion(payload) {
            return request("/api/center-language-suggestions", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        },
        requestManagerAccess(payload) {
            return request("/api/manager/request", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        },
        getManagerCenters() {
            return request("/api/manager/my-centers");
        },
        getMajalis(centerId) {
            const query = centerId ? `?centerId=${encodeURIComponent(centerId)}` : "";
            return request(`/api/majalis${query}`);
        },
        getMajlis(id) {
            return request(`/api/majalis/${id}`);
        },
        createMajlis(payload) {
            return request("/api/majalis", {
                method: "POST",
                body: JSON.stringify(payload)
            });
        },
        updateMajlis(id, payload) {
            return request(`/api/majalis/${id}`, {
                method: "PUT",
                body: JSON.stringify(payload)
            });
        },
        deleteMajlis(id) {
            return request(`/api/majalis/${id}`, {
                method: "DELETE"
            });
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
