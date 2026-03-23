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

    container.innerHTML = `<div class="empty-state">Loading approved centers from the API scaffold...</div>`;

    try {
        const response = await window.NoorLocatorApi.getCenters();
        const centers = response.data || [];

        if (!centers.length) {
            container.innerHTML = `<div class="empty-state">No centers are published yet. Phase 2 will populate this page from approved center records.</div>`;
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
            throw { message: "Center details are not available yet in Phase 1." };
        }

        title.textContent = center.name;
        meta.innerHTML = `<span class="status-pill">${center.city}, ${center.country}</span><span class="status-pill status-pill--muted">${center.address}</span>`;
        description.textContent = center.description || "A full center profile will appear here once moderation and data management are live.";
        languages.innerHTML = (center.languages || []).length
            ? center.languages.map(language => `<li class="list__item">${language.name} (${language.code})</li>`).join("")
            : `<li class="list__item">Supported languages will appear here after approval.</li>`;
        majalis.innerHTML = (center.majalis || []).length
            ? center.majalis.map(majlisItem => `<li class="list__item">${majlisItem.title} • ${majlisItem.date}</li>`).join("")
            : `<li class="list__item">No majalis are published in the Phase 1 scaffold yet.</li>`;
    } catch (error) {
        meta.innerHTML = `<span class="status-pill status-pill--muted">Phase 1 placeholder</span>`;
        description.textContent = error.message || "Center details will be connected once approved data is available.";
        languages.innerHTML = `<li class="list__item">Language associations will be displayed here later.</li>`;
        majalis.innerHTML = `<li class="list__item">Majalis for this center will appear here later.</li>`;
    }
}

function initLoginPage() {
    bindAuthForm("login-form", async formData => window.NoorLocatorApi.login(formData));
}

function initRegisterPage() {
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
            setMessage(message, response.message, "success");
        } catch (error) {
            setMessage(message, error.message || "The request could not be completed.", "error");
        }
    });
}

function initDashboardPage() {
    populateCards("dashboard-cards", [
        {
            title: "Profile foundation",
            body: "Role-aware dashboard regions are scaffolded so user-specific actions can be added without restructuring the frontend."
        },
        {
            title: "Center requests",
            body: "The next phase will connect authenticated users to the moderated center request flow."
        },
        {
            title: "Suggestions",
            body: "Feedback and correction pathways are mapped into the API and ready for storage workflows."
        }
    ]);
}

function initManagerPage() {
    populateCards("manager-cards", [
        {
            title: "Majalis publishing",
            body: "Manager-facing publishing panels are reserved for approved center representatives."
        },
        {
            title: "Center stewardship",
            body: "Future manager tools will be restricted to assigned centers only, following the spec."
        },
        {
            title: "Language moderation",
            body: "Suggested language additions will flow through admin approval instead of direct edits."
        }
    ]);
}

function initAdminPage() {
    populateCards("admin-cards", [
        {
            title: "Approval pipeline",
            body: "Admin endpoints already exist for approving managers and language suggestions."
        },
        {
            title: "Suggestion review",
            body: "The review queue is scaffolded and currently returns an empty dataset until persistence is added."
        },
        {
            title: "System integrity",
            body: "JWT, CORS, and role-ready route structure are prepared for the moderation-heavy workflows ahead."
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
