window.NoorLocatorAuth = (() => {
    const storageKeys = Object.freeze({
        token: "noorlocator.auth.token",
        refreshToken: "noorlocator.auth.refreshToken",
        user: "noorlocator.auth.user"
    });
    const defaultLogoutRedirect = "login.html?loggedOut=1";
    const defaultUnauthorizedRedirect = "login.html?sessionExpired=1";
    const session = {
        token: "",
        refreshToken: "",
        user: null
    };
    let logoutPromise = null;
    let bootstrapPromise = null;

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
            // Ignore storage write issues and continue with in-memory state.
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

    function clearCookie(key) {
        if (!document?.cookie) {
            return;
        }

        document.cookie = `${key}=; Max-Age=0; path=/; SameSite=Lax`;
    }

    function clearAuthCookies() {
        clearCookie(storageKeys.token);
        clearCookie(storageKeys.refreshToken);
        clearCookie(storageKeys.user);
    }

    function parseUser(rawUser) {
        if (!rawUser) {
            return null;
        }

        try {
            return JSON.parse(rawUser);
        } catch {
            return null;
        }
    }

    function refreshSessionFromStorage() {
        session.token = readStorage(storageKeys.token);
        session.refreshToken = readStorage(storageKeys.refreshToken);
        session.user = parseUser(readStorage(storageKeys.user));
    }

    function notifyChange() {
        window.dispatchEvent(new CustomEvent("noorlocator:auth-changed", { detail: getSessionUser() }));
    }

    function setProtectedPageReady(isReady) {
        if (document.body?.dataset.authRequired === "true") {
            document.body.dataset.authReady = isReady ? "true" : "false";
        }
    }

    function isProtectedPage() {
        return document.body?.dataset.authRequired === "true";
    }

    function getRequiredRoles() {
        return (document.body?.dataset.authRoles || "")
            .split(",")
            .map(role => role.trim())
            .filter(Boolean);
    }

    function buildRedirectPath(targetPath, flagName) {
        const url = new URL(targetPath, window.location.href);
        if (flagName) {
            url.searchParams.set(flagName, "1");
        }

        return `${url.pathname}${url.search}${url.hash}`;
    }

    function redirectTo(targetPath) {
        window.location.replace(targetPath);
    }

    function getToken() {
        return session.token;
    }

    function getRefreshToken() {
        return session.refreshToken;
    }

    function getUser() {
        return session.user;
    }

    function getSessionUser() {
        if (!session.token || !session.user) {
            return null;
        }

        return session.user;
    }

    function isAuthenticated() {
        return Boolean(getSessionUser());
    }

    function hasRole(...roles) {
        const user = getSessionUser();
        return Boolean(user && roles.includes(user.role));
    }

    function persistUser(user) {
        writeStorage(storageKeys.user, JSON.stringify(user));
        refreshSessionFromStorage();
    }

    function updateSessionUser(user) {
        if (!session.token || !user) {
            return null;
        }

        persistUser(user);
        setProtectedPageReady(true);
        notifyChange();
        return session.user;
    }

    function setSession(authResponse) {
        if (!authResponse?.token || !authResponse?.user) {
            return;
        }

        writeStorage(storageKeys.token, authResponse.token);
        writeStorage(storageKeys.refreshToken, authResponse.refreshToken || "");
        persistUser(authResponse.user);
        setProtectedPageReady(true);
        notifyChange();
    }

    function clearSession(options = {}) {
        const notify = options.notify !== false;

        removeStorage(storageKeys.token);
        removeStorage(storageKeys.refreshToken);
        removeStorage(storageKeys.user);
        clearAuthCookies();
        refreshSessionFromStorage();
        setProtectedPageReady(false);

        if (notify) {
            notifyChange();
        }
    }

    async function clearClientAuthArtifacts() {
        if (!("caches" in window)) {
            return;
        }

        try {
            const cacheKeys = await caches.keys();
            await Promise.all(
                cacheKeys
                    .filter(key => key.startsWith("noorlocator-"))
                    .map(key => caches.delete(key)));
        } catch {
            // Ignore cache cleanup failures during logout.
        }
    }

    async function callLogoutEndpoint(token, refreshToken) {
        if (!token) {
            return;
        }

        await window.NoorLocatorConfig.fetchApi("/api/auth/logout", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${token}`
            },
            body: JSON.stringify({ refreshToken }),
            cache: "no-store",
            keepalive: true
        });
    }

    async function logout(options = {}) {
        if (logoutPromise) {
            return logoutPromise;
        }

        const redirectPath = options.redirectPath || defaultLogoutRedirect;
        const token = getToken();
        const refreshToken = getRefreshToken();
        setProtectedPageReady(false);

        logoutPromise = (async () => {
            try {
                await callLogoutEndpoint(token, refreshToken);
            } catch {
                // Ignore logout transport errors and continue clearing the local session.
            } finally {
                clearSession();
                await clearClientAuthArtifacts();
                redirectTo(redirectPath);
            }
        })();

        try {
            return await logoutPromise;
        } finally {
            logoutPromise = null;
        }
    }

    function handleUnauthorized(options = {}) {
        const shouldRedirect = options.redirect ?? isProtectedPage();
        const redirectPath = options.redirectPath || defaultUnauthorizedRedirect;

        clearSession();

        if (shouldRedirect) {
            redirectTo(redirectPath);
        }
    }

    async function syncCurrentUser(options = {}) {
        const strict = options.strict === true;
        const requestedToken = getToken();
        if (!requestedToken) {
            return null;
        }

        try {
            const fetchResult = await window.NoorLocatorConfig.fetchApi("/api/auth/me", {
                headers: {
                    Authorization: `Bearer ${requestedToken}`
                },
                cache: "no-store"
            });
            const response = fetchResult.response;

            if (!response.ok) {
                if (response.status === 401 || response.status === 403) {
                    handleUnauthorized({
                        redirect: options.redirectOnUnauthorized,
                        redirectPath: options.redirectPath
                    });
                } else if (strict) {
                    clearSession();
                }

                return null;
            }

            const payload = await response.json();
            if (!payload?.data) {
                if (strict) {
                    clearSession();
                }

                return null;
            }

            if (getToken() !== requestedToken) {
                return getSessionUser();
            }

            persistUser(payload.data);
            window.NoorLocatorConfig.rememberApiBaseUrl(fetchResult.baseUrl);
            notifyChange();
            return payload.data;
        } catch {
            return strict ? null : getSessionUser();
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
            redirectTo("login.html");
            return false;
        }

        if (roles.length && !roles.some(role => hasRole(role))) {
            redirectTo(getDefaultRoute());
            return false;
        }

        return true;
    }

    function bindLogoutControls(root = document) {
        root.querySelectorAll("[data-logout-action]").forEach(control => {
            if (control.dataset.logoutBound === "true") {
                return;
            }

            control.dataset.logoutBound = "true";
            control.addEventListener("click", event => {
                event.preventDefault();
                logout({
                    redirectPath: control.dataset.logoutRedirect || defaultLogoutRedirect
                });
            });
        });
    }

    async function bootstrapPageAuth(options = {}) {
        if (bootstrapPromise && options.force !== true) {
            return bootstrapPromise;
        }

        bootstrapPromise = (async () => {
            refreshSessionFromStorage();

            if (!isProtectedPage()) {
                setProtectedPageReady(true);

                if (getToken()) {
                    syncCurrentUser({
                        redirectOnUnauthorized: false
                    }).catch(() => null);
                }

                return true;
            }

            setProtectedPageReady(false);

            if (!getToken() || !getUser()) {
                clearSession();
                redirectTo("login.html");
                return false;
            }

            const user = await syncCurrentUser({
                strict: true,
                redirectOnUnauthorized: false
            });

            if (!user) {
                handleUnauthorized({
                    redirect: true,
                    redirectPath: defaultUnauthorizedRedirect
                });
                return false;
            }

            const requiredRoles = getRequiredRoles();
            if (requiredRoles.length && !requiredRoles.some(role => hasRole(role))) {
                setProtectedPageReady(false);
                redirectTo(getDefaultRoute());
                return false;
            }

            setProtectedPageReady(true);
            return true;
        })();

        try {
            return await bootstrapPromise;
        } finally {
            bootstrapPromise = null;
        }
    }

    function handleStorageEvent(event) {
        if (event.key && !Object.values(storageKeys).includes(event.key)) {
            return;
        }

        refreshSessionFromStorage();
        notifyChange();

        if (isProtectedPage()) {
            bootstrapPageAuth({ force: true }).catch(() => {
                handleUnauthorized({
                    redirect: true,
                    redirectPath: defaultUnauthorizedRedirect
                });
            });
        }
    }

    function handlePageShow(event) {
        if (!event.persisted) {
            return;
        }

        refreshSessionFromStorage();

        if (isProtectedPage()) {
            bootstrapPageAuth({ force: true }).catch(() => {
                handleUnauthorized({
                    redirect: true,
                    redirectPath: defaultUnauthorizedRedirect
                });
            });
            return;
        }

        notifyChange();
    }

    refreshSessionFromStorage();
    window.addEventListener("storage", handleStorageEvent);
    window.addEventListener("pageshow", handlePageShow);

    return {
        bindLogoutControls,
        bootstrapPageAuth,
        clearSession,
        getDefaultRoute,
        getRefreshToken,
        getSessionUser,
        getToken,
        getUser,
        handleUnauthorized,
        hasRole,
        isAuthenticated,
        logout,
        requireAuth,
        setSession,
        syncCurrentUser,
        updateSessionUser
    };
})();
