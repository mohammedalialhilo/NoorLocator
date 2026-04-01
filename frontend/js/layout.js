window.NoorLocatorLayout = (() => {
    const navPanelId = "site-nav-panel";
    const navScrimId = "site-nav-scrim";
    const mobileNavBreakpoint = 1050;
    const brandLogoPath = "assets/logo_bkg.png";
    const defaultBrandLogoAlt = "NoorLocator logo";
    const aboutPageHref = "about.html";
    const attribution = "Driven by موكب خدام أهل البيت (عليهم السلام), Copenhagen, Denmark.";
    const defaultTagline = "Connecting you to Shia centers and majalis worldwide";
    let serviceWorkerSetupStarted = false;
    let installPromptSetupStarted = false;
    let navCleanupController = null;

    function t(key, fallback, params = {}) {
        return window.NoorLocatorI18n?.t?.(key, params, fallback) || fallback;
    }

    function getBrandLogoAlt() {
        return t("app.logoAlt", defaultBrandLogoAlt);
    }

    function getAttribution() {
        return t("app.attribution", attribution || "Driven by Mowkab Khoddam Ahlulbayt (AS), Copenhagen, Denmark.");
    }

    function getTagline() {
        return t("app.tagline", defaultTagline);
    }

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
            image.setAttribute("alt", image.getAttribute("alt") || getBrandLogoAlt());
        });

        upsertHeadLink("icon", brandLogoPath, "image/png");
        upsertHeadLink("shortcut icon", brandLogoPath, "image/png");
        upsertHeadLink("apple-touch-icon", brandLogoPath, "image/png");
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function getProfileInitial(user) {
        const sourceName = typeof user?.name === "string" && user.name.trim()
            ? user.name.trim()
            : "Member";

        return sourceName.charAt(0).toUpperCase();
    }

    function isCompactViewport() {
        if (!window.matchMedia) {
            return window.innerWidth <= mobileNavBreakpoint;
        }

        return window.matchMedia(`(max-width: ${mobileNavBreakpoint}px)`).matches;
    }

    function setScrollLockState(isLocked) {
        document.body.classList.toggle("nav-scroll-lock", isLocked);
    }

    function bindNavigationBehavior(mount) {
        const siteHeader = mount.querySelector(".site-header");
        const toggleButton = mount.querySelector("[data-nav-toggle]");
        const navPanel = mount.querySelector("[data-nav-panel]");
        const navScrim = mount.querySelector("[data-nav-scrim]");
        if (!siteHeader || !toggleButton || !navPanel || !navScrim) {
            return;
        }

        navCleanupController?.abort();
        navCleanupController = new AbortController();
        const { signal } = navCleanupController;

        const syncHeaderOffset = () => {
            const headerHeight = Math.ceil(siteHeader.getBoundingClientRect().height);
            mount.style.setProperty("--site-header-height", `${headerHeight}px`);
        };

        const setExpanded = (expanded, options = {}) => {
            const canUseDrawer = isCompactViewport();
            const isOpen = canUseDrawer && expanded;

            toggleButton.setAttribute("aria-expanded", String(isOpen));
            toggleButton.setAttribute("aria-label", isOpen
                ? t("nav.closeMenu", "Close navigation menu")
                : t("nav.openMenu", "Open navigation menu"));
            toggleButton.classList.toggle("is-open", isOpen);
            navPanel.classList.toggle("is-open", isOpen);
            navScrim.classList.toggle("is-visible", isOpen);
            navScrim.hidden = !isOpen;
            setScrollLockState(isOpen);
            syncHeaderOffset();

            if (!isOpen && options.focusToggle === true) {
                toggleButton.focus();
            }
        };

        const closeMenu = (options = {}) => {
            setExpanded(false, options);
        };

        syncHeaderOffset();
        setExpanded(false);

        if ("ResizeObserver" in window) {
            const headerObserver = new ResizeObserver(() => {
                syncHeaderOffset();
            });
            headerObserver.observe(siteHeader);
            signal.addEventListener("abort", () => {
                headerObserver.disconnect();
            }, { once: true });
        }

        toggleButton.addEventListener("click", event => {
            event.preventDefault();
            event.stopPropagation();
            setExpanded(toggleButton.getAttribute("aria-expanded") !== "true");
        }, { signal });

        navScrim.addEventListener("click", () => {
            closeMenu({ focusToggle: true });
        }, { signal });

        document.addEventListener("pointerdown", event => {
            if (!isCompactViewport() || toggleButton.getAttribute("aria-expanded") !== "true") {
                return;
            }

            const target = event.target;
            if (target instanceof Node && (navPanel.contains(target) || toggleButton.contains(target))) {
                return;
            }

            closeMenu();
        }, { signal });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape") {
                closeMenu({ focusToggle: true });
            }
        }, { signal });

        window.addEventListener("resize", () => {
            syncHeaderOffset();
            if (!isCompactViewport()) {
                closeMenu();
            }
        }, { signal });

        window.addEventListener("orientationchange", () => {
            syncHeaderOffset();
            closeMenu();
        }, { signal });

        window.addEventListener("pageshow", () => {
            syncHeaderOffset();
            closeMenu();
        }, { signal });

        navPanel.querySelectorAll("a, button").forEach(element => {
            element.addEventListener("click", () => {
                closeMenu();
            }, { signal });
        });
    }

    function renderHeader() {
        const mount = document.querySelector("[data-site-header]");
        if (!mount) {
            return;
        }

        const brandLogoAlt = getBrandLogoAlt();
        const currentPage = document.body.dataset.page;
        const user = window.NoorLocatorAuth.getSessionUser();
        const navigation = buildNavigation(user);
        const navMarkup = navigation
            .map(item => renderNavigationItem(item, currentPage))
            .join("");
        const languageOptions = window.NoorLocatorI18n.getSupportedLanguages()
            .map(language => `<option value="${escapeHtml(language.code)}">${escapeHtml(language.nativeName)}</option>`)
            .join("");

        mount.innerHTML = `
            <header class="site-header">
                <div class="site-header__inner">
                    <a class="brand" href="index.html" aria-label="${escapeHtml(t("app.homeAria", "NoorLocator home"))}">
                        <img class="site-logo site-logo--nav brand__mark" data-brand-logo src="${brandLogoPath}" alt="${brandLogoAlt}">
                        <span class="brand__copy">
                            <span class="brand__eyebrow">${escapeHtml(t("app.brandEyebrow", "Community Connection"))}</span>
                            <span class="brand__title">NoorLocator</span>
                        </span>
                    </a>
                    <button
                        class="site-nav-toggle"
                        type="button"
                        aria-expanded="false"
                        aria-label="${escapeHtml(t("nav.openMenu", "Open navigation menu"))}"
                        aria-controls="${navPanelId}"
                        data-nav-toggle>
                        <span class="sr-only">${escapeHtml(t("nav.toggle", "Toggle navigation"))}</span>
                        <span class="site-nav-toggle__line"></span>
                        <span class="site-nav-toggle__line"></span>
                        <span class="site-nav-toggle__line"></span>
                    </button>
                    <div id="${navPanelId}" class="site-header__panel" data-nav-panel>
                        <label class="site-language-switcher">
                            <span class="site-language-switcher__label">${escapeHtml(t("language.switcher", "Language"))}</span>
                            <select class="site-language-switcher__select" data-language-selector aria-label="${escapeHtml(t("language.switcherAria", "Choose language"))}">
                                ${languageOptions}
                            </select>
                        </label>
                        <nav class="site-nav" aria-label="${escapeHtml(t("nav.primaryAria", "Primary navigation"))}">
                            ${navMarkup}
                        </nav>
                    </div>
                    <button id="${navScrimId}" class="site-nav-scrim" type="button" hidden tabindex="-1" aria-label="${escapeHtml(t("nav.closeMenu", "Close navigation menu"))}" data-nav-scrim></button>
                </div>
            </header>
        `;

        bindNavigationBehavior(mount);
        window.NoorLocatorI18n.bindLanguageSelectors(mount);
    }

    function renderFooter() {
        const mount = document.querySelector("[data-site-footer]");
        if (!mount) {
            return;
        }

        const brandLogoAlt = getBrandLogoAlt();
        const user = window.NoorLocatorAuth.getSessionUser();
        const suggestionsHref = user ? "dashboard.html#suggestion" : "login.html";

        mount.innerHTML = `
            <footer class="site-footer">
                <div class="site-footer__inner">
                    <div class="site-footer__grid">
                        <div class="site-footer__brand">
                            <a class="site-footer__brand-link" href="index.html" aria-label="${escapeHtml(t("app.homeAria", "NoorLocator home"))}">
                                <img class="site-logo site-logo--footer site-footer__logo" data-brand-logo src="${brandLogoPath}" alt="${brandLogoAlt}">
                            </a>
                            <div>
                                <p class="site-footer__title">NoorLocator</p>
                                <p>${escapeHtml(getTagline())}</p>
                                <p class="site-footer__credit">${escapeHtml(getAttribution())}</p>
                            </div>
                        </div>
                        <nav class="footer-nav" aria-label="${escapeHtml(t("footer.navAria", "Footer navigation"))}">
                            <a class="footer-nav__link" href="index.html">${escapeHtml(t("nav.home", "Home"))}</a>
                            <a class="footer-nav__link" href="centers.html">${escapeHtml(t("nav.centers", "Centers"))}</a>
                            <a class="footer-nav__link" href="${aboutPageHref}">${escapeHtml(t("nav.about", "About"))}</a>
                            <a class="footer-nav__link" href="${suggestionsHref}">${escapeHtml(t("footer.contactSuggestions", "Contact / Suggestions"))}</a>
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

        if (!window.location || !/^https?:$/i.test(window.location.protocol)) {
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

    function registerInstallPromptHooks() {
        if (installPromptSetupStarted) {
            return;
        }

        installPromptSetupStarted = true;

        window.addEventListener("beforeinstallprompt", event => {
            event.preventDefault();
            window.NoorLocatorBeforeInstallPrompt = event;
        });

        window.addEventListener("appinstalled", () => {
            window.NoorLocatorBeforeInstallPrompt = null;
        });
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
            { href: "index.html", label: t("nav.home", "Home"), page: "home" },
            { href: "centers.html", label: t("nav.centers", "Centers"), page: "centers" },
            { href: aboutPageHref, label: t("nav.about", "About"), page: "about" }
        ];

        if (user) {
            if (window.NoorLocatorAuth.isEmailVerified(user)) {
                items.push({ href: "dashboard.html", label: t("nav.dashboard", "Dashboard"), page: "dashboard" });

                if (user.role === "Manager" || user.role === "Admin") {
                    items.push({ href: "manager.html", label: t("nav.manager", "Manager"), page: "manager" });
                }

                if (user.role === "Admin") {
                    items.push({ href: "admin.html", label: t("nav.admin", "Admin"), page: "admin" });
                }

                items.push({
                    href: "notifications.html",
                    label: t("nav.notifications", "Notifications"),
                    page: "notifications",
                    variant: "notification"
                });
            } else {
                items.push({
                    href: window.NoorLocatorAuth.getVerificationRoute(user),
                    label: t("nav.verifyEmail", "Verify Email"),
                    page: "verify-email"
                });
            }

            items.push({
                href: "profile.html",
                label: window.NoorLocatorAuth.formatUserDisplayName(user),
                page: "profile",
                variant: "profile",
                initial: getProfileInitial(user)
            });

            return items;
        }

        items.push({ href: "login.html", label: t("nav.login", "Login"), page: "login" });
        items.push({ href: "register.html", label: t("nav.register", "Register"), page: "register" });
        return items;
    }

    function renderNavigationItem(item, currentPage) {
        const isActive = item.page === currentPage;
        const className = `site-nav__link${isActive ? " is-active" : ""}${item.variant === "profile" ? " site-nav__link--profile" : ""}${item.variant === "notification" ? " site-nav__link--notification" : ""}`;
        const ariaCurrent = isActive ? ' aria-current="page"' : "";

        if (item.variant === "profile") {
            const profileLabel = escapeHtml(item.label);
            const profileInitial = escapeHtml(item.initial);

            return `
                <a class="${className}" href="${item.href}" data-profile-nav${ariaCurrent} aria-label="${escapeHtml(t("nav.profileAria", "Open profile for {name}", { name: item.label }))}">
                    <span class="site-nav__profile-mark" aria-hidden="true">${profileInitial}</span>
                    <span class="site-nav__profile-copy">${profileLabel}</span>
                </a>
            `;
        }

        if (item.variant === "notification") {
            return `
                <a class="${className}" href="${item.href}" data-notification-nav${ariaCurrent} aria-label="${escapeHtml(t("nav.notificationsAria", "Open notifications"))}">
                    <span class="site-nav__notification-icon" aria-hidden="true">
                        <svg viewBox="0 0 24 24" focusable="false">
                            <path d="M12 22a2.5 2.5 0 0 0 2.29-1.5h-4.58A2.5 2.5 0 0 0 12 22Zm7-4H5a1 1 0 0 1-.78-1.63l1.28-1.59V10a6.5 6.5 0 1 1 13 0v4.78l1.28 1.59A1 1 0 0 1 19 18Z"></path>
                        </svg>
                    </span>
                    <span class="site-nav__notification-copy">${escapeHtml(t("nav.notifications", "Notifications"))}</span>
                    <span class="site-nav__notification-badge" data-notification-count hidden>0</span>
                </a>
            `;
        }

        return `<a class="${className}" href="${item.href}"${ariaCurrent}>${escapeHtml(item.label)}</a>`;
    }

    function renderShell() {
        renderHeader();
        renderFooter();
        applyBrandAssets();
    }

    return {
        branding: {
            logoPath: brandLogoPath,
            logoAlt: getBrandLogoAlt()
        },
        init() {
            renderShell();
            registerInstallPromptHooks();

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
