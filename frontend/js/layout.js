window.NoorLocatorLayout = (() => {
    const navPanelId = "site-nav-panel";
    const navScrimId = "site-nav-scrim";
    const accountTriggerId = "site-account-trigger";
    const accountMenuId = "site-account-menu";
    const mobileNavBreakpoint = 1050;
    const brandLogoPath = "assets/logo_bkg.png";
    const defaultBrandLogoAlt = "NoorLocator logo";
    const aboutPageHref = "about.html";
    const attribution = "Driven by Mowkab Khoddam Ahlulbayt (AS), Copenhagen, Denmark.";
    const defaultTagline = "Connecting you to Shia centers and majalis worldwide";
    let serviceWorkerSetupStarted = false;
    let serviceWorkerReloadTriggered = false;
    let installPromptSetupStarted = false;
    let navCleanupController = null;
    let notificationCount = 0;

    function t(key, fallback, params = {}) {
        return window.NoorLocatorI18n?.t?.(key, params, fallback) || fallback;
    }

    function getBrandLogoAlt() {
        return t("app.logoAlt", defaultBrandLogoAlt);
    }

    function getAttribution() {
        return t("app.attribution", attribution);
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
        document.body.classList.toggle("nav-open", isLocked);
    }

    function normalizeNotificationCount(count) {
        const normalizedCount = Number(count);
        return Number.isFinite(normalizedCount) && normalizedCount > 0
            ? Math.floor(normalizedCount)
            : 0;
    }

    function setNotificationCount(count) {
        notificationCount = normalizeNotificationCount(count);
        const badgeValue = notificationCount > 99 ? "99+" : String(notificationCount);

        document.querySelectorAll("[data-notification-count]").forEach(badge => {
            badge.textContent = badgeValue;
            badge.hidden = notificationCount <= 0;
        });
    }

    function getLanguageOptionsMarkup() {
        return window.NoorLocatorI18n.getSupportedLanguages()
            .map(language => {
                const optionLabel = window.NoorLocatorI18n.getLanguageOptionLabel?.(language.code, {
                    native: true
                }) || language.nativeName;

                return `<option value="${escapeHtml(language.code)}">${escapeHtml(optionLabel)}</option>`;
            })
            .join("");
    }

    function renderLanguageSwitcher(options = {}) {
        const {
            variant = "public",
            includeLabel = true
        } = options;

        return `
            <label class="site-language-switcher site-language-switcher--${escapeHtml(variant)}">
                ${includeLabel ? `<span class="site-language-switcher__label">${escapeHtml(t("language.switcher", "Language"))}</span>` : ""}
                <span class="site-language-switcher__field">
                    <select class="site-language-switcher__select" data-language-selector aria-label="${escapeHtml(t("language.switcherAria", "Choose language"))}">
                        ${getLanguageOptionsMarkup()}
                    </select>
                    <span class="site-language-switcher__chevron" aria-hidden="true">
                        <svg viewBox="0 0 16 16" focusable="false">
                            <path d="M3.25 5.75 8 10.5l4.75-4.75"></path>
                        </svg>
                    </span>
                </span>
            </label>
        `;
    }

    function renderAccountMenu(user, currentPage) {
        const displayName = window.NoorLocatorAuth.formatUserDisplayName(user);
        const profileInitial = getProfileInitial(user);
        const isAccountPage = currentPage === "profile" || currentPage === "notifications";

        return `
            <div class="site-account" data-account-root>
                <button
                    id="${accountTriggerId}"
                    class="site-account__trigger${isAccountPage ? " is-active" : ""}"
                    type="button"
                    aria-expanded="false"
                    aria-controls="${accountMenuId}"
                    aria-haspopup="dialog"
                    data-account-trigger>
                    <span class="site-account__avatar" aria-hidden="true">${escapeHtml(profileInitial)}</span>
                    <span class="site-account__trigger-copy">${escapeHtml(displayName)}</span>
                    <span class="site-account__trigger-badge" data-notification-count hidden>0</span>
                    <span class="site-account__trigger-chevron" aria-hidden="true">
                        <svg viewBox="0 0 16 16" focusable="false">
                            <path d="M3.25 5.75 8 10.5l4.75-4.75"></path>
                        </svg>
                    </span>
                </button>
                <div
                    id="${accountMenuId}"
                    class="site-account__menu"
                    data-account-menu
                    aria-labelledby="${accountTriggerId}">
                    <div class="site-account__menu-summary">
                        <span class="site-account__menu-avatar" aria-hidden="true">${escapeHtml(profileInitial)}</span>
                        <div class="site-account__menu-copy">
                            <p class="site-account__menu-eyebrow">${escapeHtml(t("nav.accountMenu", "Profile and settings"))}</p>
                            <p class="site-account__menu-title">${escapeHtml(displayName)}</p>
                            <p class="site-account__menu-subtitle">${escapeHtml(user?.email || "")}</p>
                        </div>
                    </div>
                    <div class="site-account__menu-group">
                        <a class="site-account__menu-link${currentPage === "profile" ? " is-active" : ""}" href="profile.html" data-account-close-on-activate>
                            <span class="site-account__menu-link-copy">${escapeHtml(t("nav.profile", "Profile"))}</span>
                        </a>
                        <a class="site-account__menu-link${currentPage === "notifications" ? " is-active" : ""}" href="notifications.html" data-account-close-on-activate>
                            <span class="site-account__menu-link-copy">${escapeHtml(t("nav.notifications", "Notifications"))}</span>
                            <span class="site-account__menu-link-badge" data-notification-count hidden>0</span>
                        </a>
                    </div>
                    <div class="site-account__menu-section">
                        <p class="site-account__section-label">${escapeHtml(t("language.switcher", "Language"))}</p>
                        ${renderLanguageSwitcher({ variant: "account", includeLabel: false })}
                    </div>
                    <button
                        class="site-account__menu-button"
                        type="button"
                        data-account-close-on-activate
                        data-logout-action
                        data-logout-redirect="login.html?loggedOut=1">
                        ${escapeHtml(t("actions.logout", "Logout"))}
                    </button>
                </div>
            </div>
        `;
    }

    function bindAccountMenuBehavior(mount, signal) {
        const accountRoot = mount.querySelector("[data-account-root]");
        const trigger = mount.querySelector("[data-account-trigger]");
        const menu = mount.querySelector("[data-account-menu]");
        if (!accountRoot || !trigger || !menu) {
            return;
        }

        const focusableSelector = [
            "a[href]",
            "button:not([disabled])",
            "select:not([disabled])",
            "[tabindex]:not([tabindex='-1'])"
        ].join(", ");

        const isDesktopMenuOpen = () => accountRoot.classList.contains("is-open");

        const syncMenuState = (expanded) => {
            if (isCompactViewport()) {
                accountRoot.classList.remove("is-open");
                trigger.setAttribute("aria-expanded", "false");
                menu.inert = false;
                menu.setAttribute("aria-hidden", "false");
                return;
            }

            const isOpen = Boolean(expanded);
            accountRoot.classList.toggle("is-open", isOpen);
            trigger.setAttribute("aria-expanded", String(isOpen));
            menu.inert = !isOpen;
            menu.setAttribute("aria-hidden", String(!isOpen));
        };

        const closeDesktopMenu = (options = {}) => {
            if (isCompactViewport()) {
                return;
            }

            syncMenuState(false);
            if (options.focusTrigger === true) {
                trigger.focus();
            }
        };

        syncMenuState(false);

        trigger.addEventListener("click", event => {
            if (isCompactViewport()) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            syncMenuState(!isDesktopMenuOpen());
        }, { signal });

        trigger.addEventListener("keydown", event => {
            if (isCompactViewport() || event.key !== "ArrowDown") {
                return;
            }

            event.preventDefault();
            syncMenuState(true);
            const firstInteractiveControl = menu.querySelector(focusableSelector);
            firstInteractiveControl?.focus();
        }, { signal });

        document.addEventListener("pointerdown", event => {
            if (isCompactViewport() || !isDesktopMenuOpen()) {
                return;
            }

            const target = event.target;
            if (target instanceof Node && accountRoot.contains(target)) {
                return;
            }

            closeDesktopMenu();
        }, { signal });

        document.addEventListener("focusin", event => {
            if (isCompactViewport() || !isDesktopMenuOpen()) {
                return;
            }

            const target = event.target;
            if (target instanceof Node && accountRoot.contains(target)) {
                return;
            }

            closeDesktopMenu();
        }, { signal });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && isDesktopMenuOpen()) {
                event.preventDefault();
                closeDesktopMenu({ focusTrigger: true });
            }
        }, { signal });

        accountRoot.querySelectorAll("[data-account-close-on-activate]").forEach(element => {
            element.addEventListener("click", () => {
                closeDesktopMenu();
            }, { signal });
        });

        menu.querySelector("[data-language-selector]")?.addEventListener("change", () => {
            closeDesktopMenu();
        }, { signal });

        window.addEventListener("resize", () => {
            if (isCompactViewport()) {
                syncMenuState(false);
                return;
            }

            syncMenuState(isDesktopMenuOpen());
        }, { signal });

        window.addEventListener("pageshow", () => {
            syncMenuState(false);
        }, { signal });
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
            if (event.key === "Escape" && isCompactViewport()) {
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

        bindAccountMenuBehavior(mount, signal);
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
                        <nav class="site-nav" aria-label="${escapeHtml(t("nav.primaryAria", "Primary navigation"))}">
                            ${navMarkup}
                        </nav>
                        ${user
                            ? renderAccountMenu(user, currentPage)
                            : renderLanguageSwitcher({ variant: "public" })}
                    </div>
                    <button id="${navScrimId}" class="site-nav-scrim" type="button" hidden tabindex="-1" aria-label="${escapeHtml(t("nav.closeMenu", "Close navigation menu"))}" data-nav-scrim></button>
                </div>
            </header>
        `;

        bindNavigationBehavior(mount);
        window.NoorLocatorI18n.bindLanguageSelectors(mount);
        window.NoorLocatorAuth?.bindLogoutControls?.(mount);
        setNotificationCount(notificationCount);
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
            navigator.serviceWorker.addEventListener("controllerchange", () => {
                if (serviceWorkerReloadTriggered) {
                    return;
                }

                serviceWorkerReloadTriggered = true;
                window.location.reload();
            });

            const registration = await navigator.serviceWorker.register("service-worker.js");
            await registration.update();
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
            } else {
                items.push({
                    href: window.NoorLocatorAuth.getVerificationRoute(user),
                    label: t("nav.verifyEmail", "Verify Email"),
                    page: "verify-email"
                });
            }

            return items;
        }

        items.push({ href: "login.html", label: t("nav.login", "Login"), page: "login" });
        items.push({ href: "register.html", label: t("nav.register", "Register"), page: "register" });
        return items;
    }

    function renderNavigationItem(item, currentPage) {
        const isActive = item.page === currentPage;
        const className = `site-nav__link${isActive ? " is-active" : ""}`;
        const ariaCurrent = isActive ? ' aria-current="page"' : "";
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
        },
        getNotificationCount() {
            return notificationCount;
        },
        setNotificationCount
    };
})();

window.addEventListener("noorlocator:auth-changed", () => {
    window.NoorLocatorLayout.refreshAuthUi();
});
