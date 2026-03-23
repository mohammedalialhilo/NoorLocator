document.addEventListener("DOMContentLoaded", () => {
    const page = document.body.dataset.page;

    switch (page) {
        case "home":
            initHomePage();
            break;
        case "centers":
            initCentersPage();
            break;
        case "center-details":
            initCenterDetailsPage();
            break;
        case "login":
            initLoginPage();
            break;
        case "register":
            initRegisterPage();
            break;
        case "dashboard":
            initDashboardPage();
            break;
        case "manager":
            initManagerPage();
            break;
        case "admin":
            initAdminPage();
            break;
        default:
            break;
    }
});

function setMessage(element, message, type = "") {
    if (!element) {
        return;
    }

    element.textContent = message;
    element.className = `message${type ? ` message--${type}` : ""}`;
}

async function initHomePage() {
    const healthBadge = document.getElementById("health-badge");
    const healthMessage = document.getElementById("health-message");

    setMessage(healthMessage, "Checking API health...");

    try {
        const response = await window.NoorLocatorApi.getHealth();
        if (healthBadge) {
            healthBadge.textContent = "Healthy";
            healthBadge.classList.add("status-pill--success");
        }

        setMessage(healthMessage, response.message, "success");
    } catch (error) {
        if (healthBadge) {
            healthBadge.textContent = "Unavailable";
        }

        setMessage(healthMessage, error.message || "Unable to reach the API.", "error");
    }
}

async function initCentersPage() {
    const container = document.getElementById("centers-list");
    if (!container) {
        return;
    }

    container.innerHTML = `<div class="empty-state">Loading approved centers from the API...</div>`;

    try {
        const response = await window.NoorLocatorApi.getCenters();
        const centers = response.data || [];

        if (!centers.length) {
            container.innerHTML = `<div class="empty-state">No centers are published yet.</div>`;
            return;
        }

        container.innerHTML = centers.map(center => `
            <article class="card">
                <span class="card__meta">${center.city}, ${center.country}</span>
                <h3>${center.name}</h3>
                <p>${center.address}</p>
                <div class="button-row">
                    <a class="button button--secondary" href="center-details.html?id=${center.id}">View details</a>
                </div>
            </article>
        `).join("");
    } catch (error) {
        container.innerHTML = `<div class="empty-state">The centers endpoint returned an error: ${error.message || "Unknown error."}</div>`;
    }
}

async function initCenterDetailsPage() {
    const title = document.getElementById("center-title");
    const meta = document.getElementById("center-meta");
    const description = document.getElementById("center-description");
    const languages = document.getElementById("center-languages");
    const majalis = document.getElementById("center-majalis");
    const params = new URLSearchParams(window.location.search);
    const id = params.get("id") || "1";

    if (title) {
        title.textContent = `Center #${id}`;
    }

    try {
        const response = await window.NoorLocatorApi.getCenter(id);
        const center = response.data;

        if (!center) {
            throw { message: "Center details are not available yet." };
        }

        title.textContent = center.name;
        meta.innerHTML = `<span class="status-pill">${center.city}, ${center.country}</span><span class="status-pill status-pill--muted">${center.address}</span>`;
        description.textContent = center.description || "A full center profile will appear here once moderation and data management are live.";
        languages.innerHTML = (center.languages || []).length
            ? center.languages.map(language => `<li class="list__item">${language.name} (${language.code})</li>`).join("")
            : `<li class="list__item">Supported languages will appear here after approval.</li>`;
        majalis.innerHTML = (center.majalis || []).length
            ? center.majalis.map(majlisItem => `<li class="list__item">${majlisItem.title} - ${new Date(majlisItem.date).toLocaleString()}</li>`).join("")
            : `<li class="list__item">No majalis are published for this center yet.</li>`;
    } catch (error) {
        meta.innerHTML = `<span class="status-pill status-pill--muted">Profile unavailable</span>`;
        description.textContent = error.message || "Center details could not be loaded.";
        languages.innerHTML = `<li class="list__item">Language associations are not available right now.</li>`;
        majalis.innerHTML = `<li class="list__item">Majalis for this center are not available right now.</li>`;
    }
}

function initLoginPage() {
    if (window.NoorLocatorAuth.isAuthenticated()) {
        window.location.href = window.NoorLocatorAuth.getDefaultRoute();
        return;
    }

    bindAuthForm("login-form", async formData => window.NoorLocatorApi.login(formData));
}

function initRegisterPage() {
    if (window.NoorLocatorAuth.isAuthenticated()) {
        window.location.href = window.NoorLocatorAuth.getDefaultRoute();
        return;
    }

    bindAuthForm("register-form", async formData => window.NoorLocatorApi.register(formData));
}

function bindAuthForm(formId, submitAction) {
    const form = document.getElementById(formId);
    const message = document.querySelector(`[data-form-message="${formId}"]`);

    if (!form) {
        return;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const formData = Object.fromEntries(new FormData(form).entries());
        setMessage(message, "Sending request to the API...");

        try {
            const response = await submitAction(formData);
            if (response.data) {
                window.NoorLocatorAuth.setSession(response.data);
            }

            setMessage(message, response.message, "success");

            window.setTimeout(() => {
                window.location.href = window.NoorLocatorAuth.getDefaultRoute();
            }, 500);
        } catch (error) {
            setMessage(message, error.message || "The request could not be completed.", "error");
        }
    });
}

function initDashboardPage() {
    if (!window.NoorLocatorAuth.requireAuth()) {
        return;
    }

    const user = window.NoorLocatorAuth.getUser();
    populateCards("dashboard-cards", [
        {
            title: "Authenticated session",
            body: `${user.name} is signed in as ${user.role}.`
        },
        {
            title: "Center requests",
            body: "Authenticated users can now submit moderated center requests through the API."
        },
        {
            title: "Suggestions",
            body: "Feedback and correction submissions are now persisted for admin review."
        }
    ]);
}

function initManagerPage() {
    if (!window.NoorLocatorAuth.requireAuth(["Manager", "Admin"])) {
        return;
    }

    populateCards("manager-cards", [
        {
            title: "Majalis publishing",
            body: "Managers assigned to a center can now create majalis through the secured API."
        },
        {
            title: "Center stewardship",
            body: "Center assignments are enforced through the CenterManagers relationship in the database."
        },
        {
            title: "Language moderation",
            body: "Suggested language additions flow through admin approval instead of direct edits."
        }
    ]);
}

function initAdminPage() {
    if (!window.NoorLocatorAuth.requireAuth(["Admin"])) {
        return;
    }

    populateCards("admin-cards", [
        {
            title: "Approval pipeline",
            body: "Admins can approve manager requests and center language suggestions."
        },
        {
            title: "Suggestion review",
            body: "The admin suggestion endpoint now returns persisted suggestions from the database."
        },
        {
            title: "System integrity",
            body: "Audit logs capture authentication-critical activity for traceability."
        }
    ]);
}

function populateCards(containerId, cards) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    container.innerHTML = cards.map(card => `
        <article class="card">
            <h3>${card.title}</h3>
            <p>${card.body}</p>
        </article>
    `).join("");
}
