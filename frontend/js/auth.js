window.NoorLocatorAuth = (() => {
    const tokenKey = "noorlocator.auth.token";
    const refreshTokenKey = "noorlocator.auth.refreshToken";
    const userKey = "noorlocator.auth.user";

    function readStorage(key) {
        try {
            return localStorage.getItem(key) || sessionStorage.getItem(key) || "";
        } catch {
            return "";
        }
    }

    function writeStorage(key, value) {
        try {
            localStorage.setItem(key, value);
            sessionStorage.removeItem(key);
        } catch {
            // Ignore storage write issues and continue with in-memory behavior.
        }
    }

    function removeStorage(key) {
        try {
            localStorage.removeItem(key);
        } catch {
            // Ignore local storage cleanup errors.
        }

        try {
            sessionStorage.removeItem(key);
        } catch {
            // Ignore session storage cleanup errors.
        }
    }

    function getToken() {
        return readStorage(tokenKey);
    }

    function getRefreshToken() {
        return readStorage(refreshTokenKey);
    }

    function getUser() {
        const rawUser = readStorage(userKey);
        if (!rawUser) {
            return null;
        }

        try {
            return JSON.parse(rawUser);
        } catch {
            return null;
        }
    }

    function getSessionUser() {
        if (!getToken()) {
            return null;
        }

        return getUser();
    }

    function isAuthenticated() {
        return Boolean(getSessionUser());
    }

    function hasRole(...roles) {
        const user = getSessionUser();
        return Boolean(user && roles.includes(user.role));
    }

    function setSession(authResponse) {
        if (!authResponse?.token || !authResponse?.user) {
            return;
        }

        writeStorage(tokenKey, authResponse.token);
        writeStorage(refreshTokenKey, authResponse.refreshToken || "");
        writeStorage(userKey, JSON.stringify(authResponse.user));
        notifyChange();
    }

    function clearSession() {
        removeStorage(tokenKey);
        removeStorage(refreshTokenKey);
        removeStorage(userKey);
        notifyChange();
    }

    async function logout(redirectPath = "index.html?loggedOut=1") {
        const token = getToken();
        const refreshToken = getRefreshToken();

        if (token) {
            try {
                await window.NoorLocatorConfig.fetchApi("/api/auth/logout", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: `Bearer ${token}`
                    },
                    body: JSON.stringify({ refreshToken }),
                    keepalive: true
                });
            } catch {
                // Ignore logout transport errors and continue clearing the local session.
            }
        }

        clearSession();
        window.location.replace(redirectPath);
    }

    function notifyChange() {
        window.dispatchEvent(new CustomEvent("noorlocator:auth-changed", { detail: getSessionUser() }));
    }

    async function syncCurrentUser() {
        const requestedToken = getToken();
        if (!requestedToken) {
            return null;
        }

        try {
            const fetchResult = await window.NoorLocatorConfig.fetchApi("/api/auth/me", {
                headers: {
                    Authorization: `Bearer ${requestedToken}`
                }
            });
            const response = fetchResult.response;

            if (!response.ok) {
                if (response.status === 401 || response.status === 403) {
                    clearSession();
                }

                return null;
            }

            const payload = await response.json();
            if (payload?.data) {
                if (getToken() !== requestedToken) {
                    return getSessionUser();
                }

                writeStorage(userKey, JSON.stringify(payload.data));
                window.NoorLocatorConfig.rememberApiBaseUrl(fetchResult.baseUrl);
                notifyChange();
                return payload.data;
            }

            return null;
        } catch {
            return getSessionUser();
        }
    }

    function getDefaultRoute() {
        if (hasRole("Admin")) {
            return "admin.html";
        }

        if (hasRole("Manager")) {
            return "manager.html";
        }

        if (isAuthenticated()) {
            return "dashboard.html";
        }

        return "index.html";
    }

    function requireAuth(roles = []) {
        if (!isAuthenticated()) {
            window.location.href = "login.html";
            return false;
        }

        if (roles.length && !roles.some(role => hasRole(role))) {
            window.location.href = getDefaultRoute();
            return false;
        }

        return true;
    }

    return {
        clearSession,
        getDefaultRoute,
        getRefreshToken,
        getSessionUser,
        getToken,
        getUser,
        hasRole,
        isAuthenticated,
        logout,
        requireAuth,
        setSession,
        syncCurrentUser
    };
})();
