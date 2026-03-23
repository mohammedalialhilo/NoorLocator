window.NoorLocatorAuth = (() => {
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

    function isAuthenticated() {
        return Boolean(getToken() && getUser());
    }

    function hasRole(...roles) {
        const user = getUser();
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

    function notifyChange() {
        window.dispatchEvent(new CustomEvent("noorlocator:auth-changed", { detail: getUser() }));
    }

    async function syncCurrentUser() {
        const token = getToken();
        if (!token) {
            return null;
        }

        try {
            const response = await fetch("/api/auth/me", {
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            if (!response.ok) {
                clearSession();
                return null;
            }

            const payload = await response.json();
            if (payload?.data) {
                localStorage.setItem(userKey, JSON.stringify(payload.data));
                notifyChange();
                return payload.data;
            }

            return null;
        } catch {
            clearSession();
            return null;
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
        getToken,
        getUser,
        hasRole,
        isAuthenticated,
        requireAuth,
        setSession,
        syncCurrentUser
    };
})();
