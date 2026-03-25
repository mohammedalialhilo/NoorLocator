window.NoorLocatorApi = (() => {
    async function request(path, options = {}) {
        const headers = new Headers(options.headers || {});
        const token = window.NoorLocatorAuth?.getToken?.();

        if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
            headers.set("Content-Type", "application/json");
        }

        if (token && !headers.has("Authorization")) {
            headers.set("Authorization", `Bearer ${token}`);
        }

        let fetchResult;

        try {
            fetchResult = await window.NoorLocatorConfig.fetchApi(path, {
                ...options,
                headers
            });
        } catch {
            throw {
                success: false,
                message: "NoorLocator could not reach the API. Check that the backend is running and try again.",
                data: null,
                status: 0
            };
        }

        const response = fetchResult.response;
        const contentType = response.headers.get("content-type") || "";
        const payload = contentType.includes("application/json") ? await response.json() : null;
        const normalized = {
            success: payload?.success ?? response.ok,
            message: payload?.message ?? defaultStatusMessage(response),
            data: payload?.data ?? null,
            errors: payload?.data?.errors ?? [],
            traceId: payload?.data?.traceId ?? "",
            status: response.status
        };

        if (response.status === 401 && token && !path.startsWith("/api/auth/login") && !path.startsWith("/api/auth/register") && !path.startsWith("/api/auth/logout")) {
            window.NoorLocatorAuth?.handleUnauthorized?.();
        }

        if (!response.ok) {
            throw normalized;
        }

        return normalized;
    }

    function defaultStatusMessage(response) {
        if (response.ok) {
            return "Request completed.";
        }

        if (response.status === 401) {
            return "Your session is no longer valid. Please sign in again.";
        }

        if (response.status === 403) {
            return "You do not have permission to perform this action.";
        }

        if (response.status >= 500) {
            return "NoorLocator hit a server error while processing the request.";
        }

        return `Request failed with status ${response.status}.`;
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
        getAboutContent(languageCode = "") {
            return request(`/api/content/about${toQueryString({ languageCode })}`);
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
        getCenterImages(id) {
            return request(`/api/centers/${id}/images`);
        },
        getCenterLanguages(id) {
            return request(`/api/centers/${id}/languages`);
        },
        getEventAnnouncements(centerId) {
            return request(`/api/event-announcements${toQueryString({ centerId })}`);
        },
        getEventAnnouncement(id) {
            return request(`/api/event-announcements/${id}`);
        },
        createEventAnnouncement(payload) {
            return request("/api/event-announcements", {
                method: "POST",
                body: payload
            });
        },
        updateEventAnnouncement(id, payload) {
            return request(`/api/event-announcements/${id}`, {
                method: "PUT",
                body: payload
            });
        },
        deleteEventAnnouncement(id) {
            return request(`/api/event-announcements/${id}`, {
                method: "DELETE"
            });
        },
        uploadCenterImage(payload) {
            return request("/api/center-images/upload", {
                method: "POST",
                body: payload
            });
        },
        deleteCenterImage(id) {
            return request(`/api/center-images/${id}`, {
                method: "DELETE"
            });
        },
        setPrimaryCenterImage(id) {
            return request(`/api/center-images/${id}/set-primary`, {
                method: "PUT"
            });
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
        getAdminDashboard() {
            return request("/api/admin/dashboard");
        },
        getAdminCenterRequests() {
            return request("/api/admin/center-requests");
        },
        approveAdminCenterRequest(id) {
            return request(`/api/admin/center-requests/${id}/approve`, {
                method: "POST"
            });
        },
        rejectAdminCenterRequest(id) {
            return request(`/api/admin/center-requests/${id}/reject`, {
                method: "POST"
            });
        },
        getAdminManagerRequests() {
            return request("/api/admin/manager-requests");
        },
        approveAdminManagerRequest(id) {
            return request(`/api/admin/manager-requests/${id}/approve`, {
                method: "POST"
            });
        },
        rejectAdminManagerRequest(id) {
            return request(`/api/admin/manager-requests/${id}/reject`, {
                method: "POST"
            });
        },
        getAdminCenterLanguageSuggestions() {
            return request("/api/admin/center-language-suggestions");
        },
        approveAdminCenterLanguageSuggestion(id) {
            return request(`/api/admin/center-language-suggestions/${id}/approve`, {
                method: "POST"
            });
        },
        rejectAdminCenterLanguageSuggestion(id) {
            return request(`/api/admin/center-language-suggestions/${id}/reject`, {
                method: "POST"
            });
        },
        getAdminSuggestions() {
            return request("/api/admin/suggestions");
        },
        reviewAdminSuggestion(id) {
            return request(`/api/admin/suggestions/${id}/review`, {
                method: "PUT"
            });
        },
        getAdminUsers() {
            return request("/api/admin/users");
        },
        getAdminCenters() {
            return request("/api/admin/centers");
        },
        updateAdminCenter(id, payload) {
            return request(`/api/admin/centers/${id}`, {
                method: "PUT",
                body: JSON.stringify(payload)
            });
        },
        deleteAdminCenter(id) {
            return request(`/api/admin/centers/${id}`, {
                method: "DELETE"
            });
        },
        getAdminAuditLogs() {
            return request("/api/admin/audit-logs");
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
        },
        logout(payload) {
            return request("/api/auth/logout", {
                method: "POST",
                body: JSON.stringify(payload || {})
            });
        }
    };
})();
