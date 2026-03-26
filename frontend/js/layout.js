window.NoorLocatorLayout = (() => {
    const navPanelId = "site-nav-panel";
    const brandLogoPath = "assets/logo_bkg.png";
    const brandLogoAlt = "NoorLocator logo";
    const attribution = "Driven by \u0645\u0648\u0643\u0628 \u062e\u062f\u0627\u0645 \u0623\u0647\u0644 \u0627\u0644\u0628\u064a\u062a (\u0639\u0644\u064a\u0647\u0645 \u0627\u0644\u0633\u0644\u0627\u0645), Copenhagen, Denmark.";
    const tagline = "Connecting you to Shia centers and majalis worldwide";
    let serviceWorkerSetupStarted = false;

    function upsertHeadLink(rel, href, type = "") {
        let link = document.querySelector(`link[rel="${rel}"]`);
        if (!link) {
            link = document.createElement("link");
            link.rel = rel;
            document.head.appendChild(link);
        }

        link.href = href;
        if (type) {
            link.type = type;
        }
    }

    function applyBrandAssets(root = document) {
        root.querySelectorAll("[data-brand-logo]").forEach(image => {
            image.setAttribute("src", brandLogoPath);
            image.setAttribute("alt", image.getAttribute("alt") || brandLogoAlt);
        });

        upsertHeadLink("icon", brandLogoPath, "image/png");
        upsertHeadLink("shortcut icon", brandLogoPath, "image/png");
        upsertHeadLink("apple-touch-icon", brandLogoPath, "image/png");
    }

    function renderHeader() {
        const mount = document.querySelector("[data-site-header]");
        if (!mount) {
            return;
        }

        const currentPage = document.body.dataset.page;
        const user = window.NoorLocatorAuth.getSessionUser();
        const navigation = buildNavigation(user);
        const navMarkup = navigation
            .map(item => `<a class="site-nav__link${item.page === currentPage ? " is-active" : ""}" href="${item.href}">${item.label}</a>`)
            .join("");
        const authMarkup = user
            ? `
                <div class="utility-row utility-row--panel">
                    <span class="card__meta">${user.name} | ${user.role}</span>
                    <a class="button button--secondary" href="profile.html">My profile</a>
                    <a class="button button--ghost" href="logout.html" data-logout-action data-logout-redirect="login.html?loggedOut=1">Logout</a>
                </div>
            `
            : "";

        mount.innerHTML = `
            <header class="site-header">
                <div class="site-header__inner">
                    <a class="brand" href="index.html" aria-label="NoorLocator home">
                        <img class="site-logo site-logo--nav brand__mark" data-brand-logo src="${brandLogoPath}" alt="${brandLogoAlt}">
                        <span class="brand__copy">
                            <span class="brand__eyebrow">Community Connection</span>
                            <span class="brand__title">NoorLocator</span>
                        </span>
                    </a>
                    <button class="site-nav-toggle" type="button" aria-expanded="false" aria-controls="${navPanelId}" data-nav-toggle>
                        <span class="sr-only">Toggle navigation</span>
                        <span></span>
                        <span></span>
                        <span></span>
                    </button>
                    <div id="${navPanelId}" class="site-header__panel" data-nav-panel>
                        <nav class="site-nav" aria-label="Primary navigation">
                            ${navMarkup}
                        </nav>
                        ${authMarkup}
                    </div>
                </div>
            </header>
        `;

        window.NoorLocatorAuth.bindLogoutControls(mount);

        const toggleButton = mount.querySelector("[data-nav-toggle]");
        const navPanel = mount.querySelector("[data-nav-panel]");
        if (!toggleButton || !navPanel) {
            return;
        }

        const setExpanded = expanded => {
            toggleButton.setAttribute("aria-expanded", String(expanded));
            navPanel.classList.toggle("is-open", expanded);
        };

        setExpanded(false);

        toggleButton.addEventListener("click", () => {
            const expanded = toggleButton.getAttribute("aria-expanded") === "true";
            setExpanded(!expanded);
        });

        navPanel.querySelectorAll("a, button").forEach(element => {
            element.addEventListener("click", () => setExpanded(false));
        });
    }

    function renderFooter() {
        const mount = document.querySelector("[data-site-footer]");
        if (!mount) {
            return;
        }

        const user = window.NoorLocatorAuth.getSessionUser();
        const suggestionsHref = user ? "dashboard.html#suggestion" : "login.html";

        mount.innerHTML = `
            <footer class="site-footer">
                <div class="site-footer__inner">
                    <div class="site-footer__grid">
                        <div class="site-footer__brand">
                            <a class="site-footer__brand-link" href="index.html" aria-label="NoorLocator home">
                                <img class="site-logo site-logo--footer site-footer__logo" data-brand-logo src="${brandLogoPath}" alt="${brandLogoAlt}">
                            </a>
                            <div>
                                <p class="site-footer__title">NoorLocator</p>
                                <p>${tagline}</p>
                                <p class="site-footer__credit">${attribution}</p>
                            </div>
                        </div>
                        <nav class="footer-nav" aria-label="Footer navigation">
                            <a class="footer-nav__link" href="index.html">Home</a>
                            <a class="footer-nav__link" href="centers.html">Centers</a>
                            <a class="footer-nav__link" href="/about">About</a>
                            <a class="footer-nav__link" href="${suggestionsHref}">Contact / Suggestions</a>
                        </nav>
                    </div>
                </div>
            </footer>
        `;
    }

    async function registerServiceWorker() {
        if (!("serviceWorker" in navigator)) {
            return;
        }

        const isLocal = isLikelyLocalDevelopmentHost();
        if (isLocal) {
            const registrations = await navigator.serviceWorker.getRegistrations();
            await Promise.all(registrations.map(registration => registration.unregister()));

            if ("caches" in window) {
                const cacheKeys = await caches.keys();
                await Promise.all(
                    cacheKeys
                        .filter(key => key.startsWith("noorlocator-"))
                        .map(key => caches.delete(key)));
            }

            return;
        }

        if (!window.isSecureContext) {
            return;
        }

        try {
            await navigator.serviceWorker.register("service-worker.js");
        } catch (error) {
            console.warn("NoorLocator service worker registration failed.", error);
        }
    }

    function isLikelyLocalDevelopmentHost() {
        if (!window.location || !/^https?:$/i.test(window.location.protocol)) {
            return false;
        }

        const hostname = window.location.hostname || "";
        if (!hostname) {
            return false;
        }

        const labels = hostname.split(".").filter(Boolean);
        if (labels.length <= 1) {
            return true;
        }

        if (labels.length !== 4 || labels.some(label => !/^\d+$/.test(label))) {
            return false;
        }

        const octets = labels.map(label => Number(label));
        return octets[0] === 127;
    }

    function buildNavigation(user) {
        const items = [
            { href: "index.html", label: "Home", page: "home" },
            { href: "centers.html", label: "Centers", page: "centers" },
            { href: "/about", label: "About", page: "about" }
        ];

        if (user) {
            items.push({ href: "dashboard.html", label: "Dashboard", page: "dashboard" });
            items.push({ href: "profile.html", label: "Profile", page: "profile" });

            if (user.role === "Manager" || user.role === "Admin") {
                items.push({ href: "manager.html", label: "Manager", page: "manager" });
            }

            if (user.role === "Admin") {
                items.push({ href: "admin.html", label: "Admin", page: "admin" });
            }

            return items;
        }

        items.push({ href: "login.html", label: "Login", page: "login" });
        items.push({ href: "register.html", label: "Register", page: "register" });
        return items;
    }

    function renderShell() {
        renderHeader();
        renderFooter();
        applyBrandAssets();
    }

    return {
        branding: {
            logoPath: brandLogoPath,
            logoAlt: brandLogoAlt
        },
        init() {
            renderShell();

            if (!serviceWorkerSetupStarted) {
                serviceWorkerSetupStarted = true;
                registerServiceWorker().catch(error => {
                    console.warn("NoorLocator service worker setup failed.", error);
                });
            }
        },
        refreshAuthUi() {
            renderShell();
        }
    };
})();

window.addEventListener("noorlocator:auth-changed", () => {
    window.NoorLocatorLayout.refreshAuthUi();
});
