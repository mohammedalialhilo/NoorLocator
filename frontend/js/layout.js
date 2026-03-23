window.NoorLocatorLayout = (() => {
    const navigation = [
        { href: "index.html", label: "Home", page: "home" },
        { href: "centers.html", label: "Centers", page: "centers" },
        { href: "center-details.html", label: "Center Details", page: "center-details" },
        { href: "login.html", label: "Login", page: "login" },
        { href: "register.html", label: "Register", page: "register" },
        { href: "dashboard.html", label: "Dashboard", page: "dashboard" },
        { href: "manager.html", label: "Manager", page: "manager" },
        { href: "admin.html", label: "Admin", page: "admin" }
    ];

    function renderHeader() {
        const mount = document.querySelector("[data-site-header]");
        if (!mount) {
            return;
        }

        const currentPage = document.body.dataset.page;
        const navMarkup = navigation
            .map(item => `<a class="site-nav__link${item.page === currentPage ? " is-active" : ""}" href="${item.href}">${item.label}</a>`)
            .join("");

        mount.innerHTML = `
            <header class="site-header">
                <div class="site-header__inner">
                    <a class="brand" href="index.html" aria-label="NoorLocator home">
                        <img class="brand__mark" src="assets/logo.svg" alt="NoorLocator logo">
                        <span>
                            <span class="brand__eyebrow">Phase 1 Scaffold</span>
                            <span class="brand__title">NoorLocator</span>
                        </span>
                    </a>
                    <nav class="site-nav" aria-label="Primary navigation">
                        ${navMarkup}
                    </nav>
                </div>
            </header>
        `;
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
                    <p>NoorLocator Phase 1 provides the clean architecture, API wiring, and branded web scaffold for future feature delivery.</p>
                </div>
            </footer>
        `;
    }

    return {
        init() {
            renderHeader();
            renderFooter();
        }
    };
})();

document.addEventListener("DOMContentLoaded", () => {
    window.NoorLocatorLayout.init();
});
