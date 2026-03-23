window.NoorLocatorAuth = (() => {
    const apiBaseUrl = document.body?.dataset.apiBaseUrl ?? "";
    const tokenKey = "noorlocator.auth.token";
    const refreshTokenKey = "noorlocator.auth.refreshToken";
    const userKey = "noorlocator.auth.user";

    function getToken() {
        return localStorage.getItem(tokenKey) || "";
    }

    function getRefreshToken() {
        return localStorage.getItem(refreshTokenKey) || "";
    }

    function getUser() {
        const rawUser = localStorage.getItem(userKey);
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

        localStorage.setItem(tokenKey, authResponse.token);
        localStorage.setItem(refreshTokenKey, authResponse.refreshToken || "");
        localStorage.setItem(userKey, JSON.stringify(authResponse.user));
        notifyChange();
    }

    function clearSession() {
        localStorage.removeItem(tokenKey);
        localStorage.removeItem(refreshTokenKey);
        localStorage.removeItem(userKey);
        notifyChange();
    }

    function logout(redirectPath = "index.html") {
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
            const response = await fetch(`${apiBaseUrl}/api/auth/me`, {
                headers: {
                    Authorization: `Bearer ${requestedToken}`
                }
            });

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

                localStorage.setItem(userKey, JSON.stringify(payload.data));
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
