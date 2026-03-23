window.NoorLocatorLayout = (() => {
    function renderHeader() {
        const mount = document.querySelector("[data-site-header]");
        if (!mount) {
            return;
        }

        const currentPage = document.body.dataset.page;
        const user = window.NoorLocatorAuth.getUser();
        const navigation = buildNavigation(user);
        const navMarkup = navigation
            .map(item => `<a class="site-nav__link${item.page === currentPage ? " is-active" : ""}" href="${item.href}">${item.label}</a>`)
            .join("");
        const authMarkup = user
            ? `
                <div class="utility-row">
                    <span class="card__meta">${user.name} · ${user.role}</span>
                    <button class="button button--ghost" type="button" data-logout-button>Logout</button>
                </div>
            `
            : "";

        mount.innerHTML = `
            <header class="site-header">
                <div class="site-header__inner">
                    <a class="brand" href="index.html" aria-label="NoorLocator home">
                        <img class="brand__mark" src="assets/logo.svg" alt="NoorLocator logo">
                        <span>
                            <span class="brand__eyebrow">Public Discovery</span>
                            <span class="brand__title">NoorLocator</span>
                        </span>
                    </a>
                    <nav class="site-nav" aria-label="Primary navigation">
                        ${navMarkup}
                    </nav>
                    ${authMarkup}
                </div>
            </header>
        `;

        const logoutButton = mount.querySelector("[data-logout-button]");
        if (logoutButton) {
            logoutButton.addEventListener("click", () => {
                window.NoorLocatorAuth.clearSession();
                window.location.href = "index.html";
            });
        }
    }

    function renderFooter() {
        const mount = document.querySelector("[data-site-footer]");
        if (!mount) {
            return;
        }

        mount.innerHTML = `
            <footer class="site-footer">
                <div class="site-footer__inner">
                    <p class="site-footer__credit">Driven by موكب خدام اهل البيت (عليهم السلام), Copenhagen, Denmark.</p>
                    <p>NoorLocator helps guests discover nearby centers and upcoming majalis through real API-backed public search.</p>
                </div>
            </footer>
        `;
    }

    function buildNavigation(user) {
        const items = [
            { href: "index.html", label: "Home", page: "home" },
            { href: "centers.html", label: "Centers", page: "centers" }
        ];

        if (user) {
            items.push({ href: "dashboard.html", label: "Dashboard", page: "dashboard" });

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

    return {
        init() {
            renderHeader();
            renderFooter();

            if (window.NoorLocatorAuth.isAuthenticated()) {
                window.NoorLocatorAuth.syncCurrentUser().finally(renderHeader);
            }
        }
    };
})();

document.addEventListener("DOMContentLoaded", () => {
    window.NoorLocatorLayout.init();
});

window.addEventListener("noorlocator:auth-changed", () => {
    window.NoorLocatorLayout.init();
});
