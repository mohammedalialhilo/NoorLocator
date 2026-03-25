const DISCOVERY_LOCATION_KEY = "noorlocator.discovery.location";
const CENTER_IMAGE_MAX_SIZE_BYTES = 5 * 1024 * 1024;
const CENTER_IMAGE_ALLOWED_EXTENSIONS = new Set([".jpg", ".jpeg", ".png", ".webp"]);

document.addEventListener("DOMContentLoaded", async () => {
    const authReady = await window.NoorLocatorAuth.bootstrapPageAuth();
    if (!authReady && document.body?.dataset.authRequired === "true") {
        return;
    }

    window.NoorLocatorLayout.init();
    window.NoorLocatorAuth.bindLogoutControls(document);
    notifyAuthStatus();

    const page = document.body.dataset.page;

    switch (page) {
        case "home":
            initHomePage();
            break;
        case "about":
            initAboutPage();
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
        case "profile":
            initProfilePage();
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

function notifyAuthStatus() {
    const url = new URL(window.location.href);
    let message = "";
    let type = "success";

    if (url.searchParams.get("loggedOut") === "1") {
        url.searchParams.delete("loggedOut");
        message = "You have been signed out successfully.";
    } else if (url.searchParams.get("sessionExpired") === "1") {
        url.searchParams.delete("sessionExpired");
        message = "Your session ended. Please sign in again.";
        type = "error";
    }

    if (!message) {
        return;
    }

    if (window.history?.replaceState) {
        const query = url.searchParams.toString();
        window.history.replaceState({}, document.title, `${url.pathname}${query ? `?${query}` : ""}${url.hash}`);
    }

    showToast(message, type);
}

function setMessage(element, message, type = "") {
    if (!element) {
        return;
    }

    element.textContent = message;
    element.className = `message${type ? ` message--${type}` : ""}`;
}

function setContainerMessage(container, message, modifier = "") {
    if (!container) {
        return;
    }

    const className = modifier ? `empty-state empty-state--${modifier}` : "empty-state";
    container.innerHTML = `<div class="${className}">${escapeHtml(message)}</div>`;
}

function setCardLoadingState(container, count = 3) {
    if (!container) {
        return;
    }

    container.innerHTML = Array.from({ length: count }, () => `
        <article class="card card--loading">
            <span class="skeleton skeleton--line skeleton--sm"></span>
            <span class="skeleton skeleton--line skeleton--lg"></span>
            <span class="skeleton skeleton--line"></span>
            <span class="skeleton skeleton--line skeleton--md"></span>
        </article>
    `).join("");
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

function truncateText(value, maxLength = 140) {
    const text = String(value || "").trim();
    if (text.length <= maxLength) {
        return text;
    }

    return `${text.slice(0, Math.max(0, maxLength - 3)).trimEnd()}...`;
}

function formatDistance(distanceKm) {
    if (typeof distanceKm !== "number" || Number.isNaN(distanceKm)) {
        return "";
    }

    return `${distanceKm.toFixed(1)} km away`;
}

function formatDateTime(dateValue) {
    if (!dateValue) {
        return "Date to be announced";
    }

    const date = new Date(dateValue);
    if (Number.isNaN(date.getTime())) {
        return "Date to be announced";
    }

    return new Intl.DateTimeFormat(undefined, {
        dateStyle: "medium",
        timeStyle: "short"
    }).format(date);
}

function buildMapLink(center) {
    if (typeof center?.latitude !== "number" || typeof center?.longitude !== "number") {
        return "#";
    }

    return `https://www.google.com/maps?q=${encodeURIComponent(`${center.latitude},${center.longitude}`)}`;
}

function parseLocation(latValue, lngValue) {
    const lat = typeof latValue === "number" ? latValue : Number(latValue);
    const lng = typeof lngValue === "number" ? lngValue : Number(lngValue);

    if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
        return null;
    }

    return {
        lat: Number(lat.toFixed(6)),
        lng: Number(lng.toFixed(6))
    };
}

function getStoredDiscoveryLocation() {
    try {
        const raw = window.localStorage.getItem(DISCOVERY_LOCATION_KEY);
        if (!raw) {
            return null;
        }

        const parsed = JSON.parse(raw);
        return parseLocation(parsed.lat, parsed.lng);
    } catch {
        return null;
    }
}

function getDiscoveryLocationFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const lat = params.get("lat");
    const lng = params.get("lng");

    if (!lat || !lng) {
        return null;
    }

    return parseLocation(lat, lng);
}

function getDiscoveryLocation() {
    return getDiscoveryLocationFromUrl() || getStoredDiscoveryLocation();
}

function setDiscoveryLocation(location) {
    if (!location) {
        return;
    }

    try {
        window.localStorage.setItem(DISCOVERY_LOCATION_KEY, JSON.stringify(location));
    } catch {
        // Ignore storage failures and keep the UI running.
    }
}

function hasSearchFilters(filters) {
    return Boolean(
        filters.query ||
        filters.city ||
        filters.country ||
        filters.languageCode
    );
}

function appendLocationParams(params, location) {
    if (!location) {
        return params;
    }

    return {
        ...params,
        lat: location.lat,
        lng: location.lng
    };
}

function getTrimmedFormValues(form) {
    return Object.fromEntries(
        Array.from(new FormData(form).entries()).map(([key, value]) => [key, String(value).trim()]));
}

function buildCenterDetailsHref(centerId, location = getDiscoveryLocation()) {
    const params = new URLSearchParams({ id: String(centerId) });

    if (location) {
        params.set("lat", String(location.lat));
        params.set("lng", String(location.lng));
    }

    return `center-details.html?${params.toString()}`;
}

function normalizeErrorMessage(error, fallbackMessage) {
    if (Array.isArray(error?.errors) && error.errors.length > 0 && typeof error.errors[0] === "string") {
        return error.errors[0].trim();
    }

    if (typeof error?.message === "string" && error.message.trim()) {
        return error.message.trim();
    }

    return fallbackMessage;
}

function ensureToastRoot() {
    let toastRoot = document.querySelector("[data-toast-root]");

    if (!toastRoot) {
        toastRoot = document.createElement("div");
        toastRoot.className = "toast-root";
        toastRoot.setAttribute("data-toast-root", "true");
        toastRoot.setAttribute("role", "status");
        toastRoot.setAttribute("aria-live", "polite");
        document.body.appendChild(toastRoot);
    }

    return toastRoot;
}

function showToast(message, type = "success") {
    if (!message) {
        return;
    }

    const toastRoot = ensureToastRoot();
    const toast = document.createElement("div");
    toast.className = `toast toast--${type}`;
    toast.innerHTML = `
        <div class="toast__content">
            <strong class="toast__title">${type === "error" ? "Action needed" : "NoorLocator"}</strong>
            <span>${escapeHtml(message)}</span>
        </div>
        <button class="toast__close" type="button" aria-label="Dismiss notification">&times;</button>
    `;
    toastRoot.appendChild(toast);

    const closeToast = () => {
        toast.classList.add("toast--leaving");

        window.setTimeout(() => {
            toast.remove();
        }, 220);
    };

    toast.querySelector(".toast__close")?.addEventListener("click", closeToast);

    window.setTimeout(() => {
        closeToast();
    }, 3600);
}

function renderStatusBadge(status) {
    const value = String(status || "Pending");
    const normalized = value.toLowerCase();
    const modifier = ["approved", "rejected", "reviewed", "draft", "published", "archived"].includes(normalized)
        ? normalized
        : "pending";
    return `<span class="status-badge status-badge--${modifier}">${escapeHtml(value)}</span>`;
}

function populateSelectOptions(selectElements, items, options) {
    const selects = Array.isArray(selectElements)
        ? selectElements.filter(Boolean)
        : Array.from(selectElements || []).filter(Boolean);

    if (!selects.length) {
        return;
    }

    const {
        placeholder,
        getValue,
        getLabel
    } = options;

    const markup = [
        `<option value="">${escapeHtml(placeholder)}</option>`,
        ...items.map(item => `<option value="${escapeHtml(getValue(item))}">${escapeHtml(getLabel(item))}</option>`)
    ].join("");

    selects.forEach(select => {
        select.innerHTML = markup;
        select.disabled = items.length === 0;
    });
}

function setSubmitButtonState(form, isBusy, busyLabel) {
    const submitButton = form?.querySelector('button[type="submit"]');
    if (!submitButton) {
        return;
    }

    if (!submitButton.dataset.defaultLabel) {
        submitButton.dataset.defaultLabel = submitButton.textContent || "Submit";
    }

    submitButton.disabled = isBusy;
    submitButton.textContent = isBusy ? busyLabel : submitButton.dataset.defaultLabel;
}

function renderCenterCards(container, centers, emptyMessage, options = {}) {
    if (!container) {
        return;
    }

    if (!centers.length) {
        setContainerMessage(container, emptyMessage, "soft");
        return;
    }

    const {
        limit,
        titleLevel = "h3"
    } = options;

    const visibleCenters = typeof limit === "number" ? centers.slice(0, limit) : centers;

    container.innerHTML = visibleCenters.map(center => `
        <article class="card card--interactive">
            <div class="card__header">
                <span class="card__meta">${escapeHtml(`${center.city}, ${center.country}`)}</span>
                ${typeof center.distanceKm === "number" ? `<span class="status-pill status-pill--success">${escapeHtml(formatDistance(center.distanceKm))}</span>` : ""}
            </div>
            <${titleLevel}>${escapeHtml(center.name)}</${titleLevel}>
            <p class="card__excerpt">${escapeHtml(truncateText(center.description || "Public center details are available on the profile page.", 150))}</p>
            <p>${escapeHtml(center.address)}</p>
            <div class="button-row">
                <a class="button button--secondary" href="${buildCenterDetailsHref(center.id)}">View details</a>
                <a class="button button--ghost" href="${buildMapLink(center)}" target="_blank" rel="noreferrer noopener">Open map</a>
            </div>
        </article>
    `).join("");
}

function renderLanguageChips(container, languages) {
    if (!container) {
        return;
    }

    if (!languages.length) {
        container.innerHTML = `<span class="empty-state empty-state--soft">No supported languages are published for this center yet.</span>`;
        return;
    }

    container.innerHTML = languages
        .map(language => `<span class="chip">${escapeHtml(`${language.name} (${language.code})`)}</span>`)
        .join("");
}

function renderMajalis(container, majalis) {
    if (!container) {
        return;
    }

    if (!majalis.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">No upcoming majalis are currently published for this center.</div>`;
        return;
    }

    container.innerHTML = majalis.map(majlis => `
        <article class="list-card">
            ${majlis.imageUrl ? `<img class="majlis-card__image" src="${escapeHtml(majlis.imageUrl)}" alt="${escapeHtml(`${majlis.title} image`)}" loading="lazy">` : ""}
            <div class="list-card__head">
                <h4>${escapeHtml(majlis.title)}</h4>
                <span class="status-pill">${escapeHtml(formatDateTime(majlis.date))}</span>
            </div>
            <p>${escapeHtml(majlis.description || "Majlis details will appear here when available.")}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(majlis.time || "Time to be confirmed")}</span>
                ${(majlis.languages || []).map(language => `<span class="chip chip--muted">${escapeHtml(language.name)}</span>`).join("")}
            </div>
        </article>
    `).join("");
}

function pickPrimaryCenterImage(images) {
    if (!Array.isArray(images) || !images.length) {
        return null;
    }

    return images.find(image => image.isPrimary) || images[0];
}

function getSecondaryCenterImages(images) {
    const primaryImage = pickPrimaryCenterImage(images);
    if (!primaryImage) {
        return [];
    }

    return images.filter(image => image.id !== primaryImage.id);
}

function applyCenterHeroImage(heroImage, logoFallback, centerName, images) {
    if (!heroImage || !logoFallback) {
        return;
    }

    const primaryImage = pickPrimaryCenterImage(images);
    if (!primaryImage?.imageUrl) {
        heroImage.hidden = true;
        heroImage.removeAttribute("src");
        logoFallback.hidden = false;
        return;
    }

    heroImage.src = primaryImage.imageUrl;
    heroImage.alt = `${centerName} primary image`;
    heroImage.hidden = false;
    logoFallback.hidden = true;
}

function renderCenterGallery(container, images, options = {}) {
    if (!container) {
        return;
    }

    const manageable = options.manageable === true;
    const galleryImages = manageable || !options.omitPrimary
        ? images
        : getSecondaryCenterImages(images);

    if (!galleryImages.length) {
        const emptyMessage = options.emptyMessage
            || (!manageable && images.length
                ? "The primary center image is shown above. No additional gallery images are available yet."
                : "No public center photos have been uploaded yet.");
        setContainerMessage(container, emptyMessage, "soft");
        return;
    }

    container.innerHTML = galleryImages.map(image => `
        <article class="gallery-card">
            <img class="gallery-card__image" src="${escapeHtml(image.imageUrl)}" alt="${escapeHtml(options.imageAlt || "Center gallery image")}" loading="lazy">
            <div class="gallery-card__head">
                <div>
                    ${image.isPrimary ? `<span class="status-badge status-badge--published">Primary</span>` : `<span class="card__meta">Gallery image</span>`}
                    <p class="gallery-card__meta">Uploaded ${escapeHtml(formatDateTime(image.createdAt))}</p>
                </div>
            </div>
            ${manageable ? `
                <div class="gallery-card__actions">
                    ${image.isPrimary ? "" : `<button class="button button--secondary" type="button" data-set-primary-image-id="${escapeHtml(image.id)}">Set primary</button>`}
                    <button class="button button--danger" type="button" data-delete-center-image-id="${escapeHtml(image.id)}">Delete</button>
                </div>
            ` : ""}
        </article>
    `).join("");
}

function renderCenterAnnouncements(container, announcements, options = {}) {
    if (!container) {
        return;
    }

    if (!announcements.length) {
        const emptyMessage = options.emptyMessage || "No public announcements are available for this center right now.";
        setContainerMessage(container, emptyMessage, "soft");
        return;
    }

    const manageable = options.manageable === true;

    container.innerHTML = announcements.map(announcement => `
        <article class="list-card">
            ${announcement.imageUrl ? `<img class="announcement-card__image" src="${escapeHtml(announcement.imageUrl)}" alt="${escapeHtml(announcement.title)}" loading="lazy">` : ""}
            <div class="announcement-card__head">
                <div>
                    <h4>${escapeHtml(announcement.title)}</h4>
                    <p class="announcement-card__meta">${escapeHtml(announcement.centerName || "")}</p>
                </div>
                ${manageable ? renderStatusBadge(announcement.status) : `<span class="status-pill">${escapeHtml(formatDateTime(announcement.createdAt))}</span>`}
            </div>
            <p>${escapeHtml(announcement.description || "No additional announcement details have been provided.")}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">Published ${escapeHtml(formatDateTime(announcement.createdAt))}</span>
                ${manageable ? `<span class="card__meta">Status: ${escapeHtml(String(announcement.status))}</span>` : ""}
            </div>
            ${manageable ? `
                <div class="button-row">
                    <button class="button button--secondary" type="button" data-edit-announcement-id="${escapeHtml(announcement.id)}">Edit</button>
                    <button class="button button--danger" type="button" data-delete-announcement-id="${escapeHtml(announcement.id)}" data-announcement-title="${escapeHtml(announcement.title)}">Delete</button>
                </div>
            ` : ""}
        </article>
    `).join("");
}

function renderCenterRequestList(container, requests) {
    if (!container) {
        return;
    }

    if (!requests.length) {
        setContainerMessage(
            container,
            "You have not submitted a center request yet. Use the form to send a new center into the moderation queue.",
            "soft");
        return;
    }

    container.innerHTML = requests.map(request => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(request.name)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${request.city}, ${request.country}`)}</p>
                </div>
                ${renderStatusBadge(request.status)}
            </div>
            <p>${escapeHtml(request.address)}</p>
            <p>${escapeHtml(truncateText(request.description || "No description was submitted for this request.", 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">Submitted ${escapeHtml(formatDateTime(request.createdAt))}</span>
                <span class="card__meta">${escapeHtml(`${Number(request.latitude).toFixed(4)}, ${Number(request.longitude).toFixed(4)}`)}</span>
            </div>
        </article>
    `).join("");
}

function requestBrowserLocation() {
    if (!("geolocation" in navigator)) {
        return Promise.reject(new Error("Geolocation is not supported in this browser. Use the city and country filters instead."));
    }

    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            position => {
                const location = parseLocation(position.coords.latitude, position.coords.longitude);
                if (!location) {
                    reject(new Error("Your browser returned an invalid location."));
                    return;
                }

                resolve(location);
            },
            error => {
                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        reject(new Error("Location access was denied. Use city and country search instead."));
                        break;
                    case error.TIMEOUT:
                        reject(new Error("Location lookup timed out. Please try again or use city and country search."));
                        break;
                    default:
                        reject(new Error("Unable to determine your location right now."));
                        break;
                }
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 300000
            });
    });
}

function renderFeatureHighlights(container, features) {
    if (!container) {
        return;
    }

    if (!features.length) {
        setContainerMessage(container, "No identity highlights are available right now.", "soft");
        return;
    }

    container.innerHTML = features.map(feature => `
        <article class="card">
            <span class="card__meta">Feature</span>
            <h3>${escapeHtml(feature.title)}</h3>
            <p>${escapeHtml(feature.description)}</p>
        </article>
    `).join("");
}

function renderManifestoItems(container, items) {
    if (!container) {
        return;
    }

    if (!items.length) {
        setContainerMessage(container, "No manifesto details are available right now.", "soft");
        return;
    }

    container.innerHTML = items.map(item => `
        <article class="manifesto-list__item">
            <div class="manifesto-list__icon"></div>
            <p>${escapeHtml(item)}</p>
        </article>
    `).join("");
}

function renderPrinciples(container, principles) {
    if (!container) {
        return;
    }

    if (!principles.length) {
        setContainerMessage(container, "No core principles are available right now.", "soft");
        return;
    }

    container.innerHTML = principles.map(principle => `
        <article class="principle-card">
            <div class="principle-card__mark">${escapeHtml(principle.title.charAt(0) || "P")}</div>
            <h3>${escapeHtml(principle.title)}</h3>
            <p>${escapeHtml(principle.description)}</p>
        </article>
    `).join("");
}

async function initHomePage() {
    const featuredCenters = document.getElementById("featured-centers");
    const homeStatus = document.getElementById("home-status");
    const centerCount = document.getElementById("home-center-count");
    const heroTitle = document.getElementById("home-hero-title");
    const heroDescription = document.getElementById("home-hero-description");
    const heroHighlight = document.getElementById("home-hero-highlight");
    const missionTitle = document.getElementById("home-mission-title");
    const missionDescription = document.getElementById("home-mission-description");
    const missionHighlight = document.getElementById("home-mission-highlight");
    const featuresTitle = document.getElementById("home-features-title");
    const featuresDescription = document.getElementById("home-features-description");
    const featureHighlights = document.getElementById("home-feature-highlights");
    const submitCenterLink = document.getElementById("home-submit-center-link");
    const location = getDiscoveryLocation();

    if (submitCenterLink) {
        submitCenterLink.href = window.NoorLocatorAuth.isAuthenticated()
            ? "dashboard.html#center-request"
            : "register.html";
    }

    setCardLoadingState(featuredCenters, 3);
    setCardLoadingState(featureHighlights, 3);
    setMessage(homeStatus, "Connecting to the live NoorLocator directory...");

    try {
        const [contentResponse, centersResponse] = await Promise.all([
            window.NoorLocatorApi.getAboutContent(),
            location
                ? window.NoorLocatorApi.getNearestCenters(location)
                : window.NoorLocatorApi.getCenters()
        ]);
        const content = contentResponse.data;
        const centers = centersResponse.data || [];

        if (heroTitle) {
            heroTitle.textContent = content.homeHero.title;
        }

        if (heroDescription) {
            heroDescription.textContent = content.homeHero.description;
        }

        if (heroHighlight) {
            heroHighlight.textContent = content.homeHero.highlight;
        }

        if (missionTitle) {
            missionTitle.textContent = content.homeMission.title;
        }

        if (missionDescription) {
            missionDescription.textContent = content.homeMission.description;
        }

        if (missionHighlight) {
            missionHighlight.textContent = content.homeMission.highlight;
        }

        if (featuresTitle) {
            featuresTitle.textContent = content.homeFeatures.title;
        }

        if (featuresDescription) {
            featuresDescription.textContent = content.homeFeatures.description;
        }

        renderFeatureHighlights(featureHighlights, content.homeFeatures.items || []);

        if (centerCount) {
            centerCount.textContent = String(centers.length);
        }

        renderCenterCards(
            featuredCenters,
            centers,
            "No public centers are available yet.",
            { limit: 3 });

        setMessage(
            homeStatus,
            location
                ? "Showing the closest published centers based on your saved location."
                : "Showing a live preview of published centers from the public API.",
            "success");
    } catch (error) {
        if (centerCount) {
            centerCount.textContent = "0";
        }

        setContainerMessage(featuredCenters, "The public center preview could not be loaded right now.", "error");
        setContainerMessage(featureHighlights, "The mission highlights could not be loaded right now.", "error");
        setMessage(homeStatus, error.message || "Unable to load the public center preview.", "error");
    }
}

async function initAboutPage() {
    const pageMessage = document.getElementById("about-page-message");
    const submitCenterLink = document.getElementById("about-submit-center-link");
    const heroTitle = document.getElementById("about-hero-title");
    const heroDescription = document.getElementById("about-hero-description");
    const heroHighlight = document.getElementById("about-hero-highlight");
    const visionTitle = document.getElementById("about-vision-title");
    const visionDescription = document.getElementById("about-vision-description");
    const visionHighlight = document.getElementById("about-vision-highlight");
    const problemTitle = document.getElementById("about-problem-title");
    const problemDescription = document.getElementById("about-problem-description");
    const problemItems = document.getElementById("about-problem-items");
    const missionTitle = document.getElementById("about-mission-title");
    const missionDescription = document.getElementById("about-mission-description");
    const missionItems = document.getElementById("about-mission-items");
    const principlesTitle = document.getElementById("about-principles-title");
    const principlesDescription = document.getElementById("about-principles-description");
    const principles = document.getElementById("about-principles");
    const identityTitle = document.getElementById("about-identity-title");
    const identityDescription = document.getElementById("about-identity-description");
    const identityHighlight = document.getElementById("about-identity-highlight");
    const closingTitle = document.getElementById("about-closing-title");
    const closingDescription = document.getElementById("about-closing-description");
    const closingHighlight = document.getElementById("about-closing-highlight");

    if (submitCenterLink) {
        submitCenterLink.href = window.NoorLocatorAuth.isAuthenticated()
            ? "dashboard.html#center-request"
            : "register.html";
    }

    try {
        const response = await window.NoorLocatorApi.getAboutContent();
        const content = response.data;

        document.title = "About NoorLocator";

        if (heroDescription) {
            heroDescription.textContent = content.vision.description;
        }

        if (heroHighlight) {
            heroHighlight.textContent = content.siteTagline;
        }

        if (visionTitle) {
            visionTitle.textContent = content.vision.title;
        }

        if (visionDescription) {
            visionDescription.textContent = content.vision.description;
        }

        if (visionHighlight) {
            visionHighlight.textContent = content.vision.highlight;
        }

        if (problemTitle) {
            problemTitle.textContent = content.problemStatement.title;
        }

        if (problemDescription) {
            problemDescription.textContent = content.problemStatement.description;
        }

        renderManifestoItems(problemItems, content.problemStatement.items || []);

        if (missionTitle) {
            missionTitle.textContent = content.mission.title;
        }

        if (missionDescription) {
            missionDescription.textContent = content.mission.description;
        }

        renderManifestoItems(missionItems, content.mission.items || []);

        if (principlesTitle) {
            principlesTitle.textContent = content.corePrinciples.title;
        }

        if (principlesDescription) {
            principlesDescription.textContent = content.corePrinciples.description;
        }

        renderPrinciples(principles, content.corePrinciples.items || []);

        if (identityTitle) {
            identityTitle.textContent = content.whoWeAre.title;
        }

        if (identityDescription) {
            identityDescription.textContent = content.whoWeAre.description;
        }

        if (identityHighlight) {
            identityHighlight.textContent = content.whoWeAre.highlight;
        }

        if (closingTitle) {
            closingTitle.textContent = content.closing.title;
        }

        if (closingDescription) {
            closingDescription.textContent = content.closing.description;
        }

        if (closingHighlight) {
            closingHighlight.textContent = content.closing.highlight;
        }

        setMessage(pageMessage, "Manifesto-driven identity content loaded successfully.", "success");
    } catch (error) {
        const message = normalizeErrorMessage(error, "The About page could not be loaded right now.");
        setMessage(pageMessage, message, "error");
        renderManifestoItems(problemItems, []);
        renderManifestoItems(missionItems, []);
        renderPrinciples(principles, []);
    }
}

async function initCentersPage() {
    const centersContainer = document.getElementById("centers-list");
    const nearbyContainer = document.getElementById("nearby-centers");
    const searchForm = document.getElementById("center-search-form");
    const searchMessage = document.querySelector('[data-form-message="center-search-form"]');
    const pageMessage = document.getElementById("centers-page-message");
    const resultsSummary = document.getElementById("centers-results-summary");
    const locationStatus = document.getElementById("location-status");
    const currentLocation = document.getElementById("current-location");
    const locateButtons = document.querySelectorAll("[data-locate-centers]");
    const clearButton = document.getElementById("clear-center-filters");
    const state = {
        location: getDiscoveryLocation()
    };

    if (!centersContainer || !nearbyContainer || !searchForm) {
        return;
    }

    async function loadDirectory(filters = null) {
        setCardLoadingState(centersContainer, 4);
        const response = filters && hasSearchFilters(filters)
            ? await window.NoorLocatorApi.searchCenters(appendLocationParams(filters, state.location))
            : await window.NoorLocatorApi.getCenters(appendLocationParams({}, state.location));
        const centers = response.data || [];

        renderCenterCards(centersContainer, centers, "No centers matched the current filters.");

        if (resultsSummary) {
            resultsSummary.textContent = filters && hasSearchFilters(filters)
                ? `Showing ${centers.length} center${centers.length === 1 ? "" : "s"} that match your search.`
                : `Showing ${centers.length} published center${centers.length === 1 ? "" : "s"} from the public API.`;
        }
    }

    async function loadNearbyCenters() {
        if (!state.location) {
            setContainerMessage(nearbyContainer, "Enable location access to see the closest centers, or use the search filters below.", "soft");
            return;
        }

        setCardLoadingState(nearbyContainer, 3);

        try {
            const response = await window.NoorLocatorApi.getNearestCenters(state.location);
            const centers = response.data || [];
            renderCenterCards(nearbyContainer, centers, "No nearby centers are available yet.", { limit: 3 });
        } catch (error) {
            setContainerMessage(nearbyContainer, error.message || "Nearby center lookup is currently unavailable.", "error");
        }
    }

    function updateLocationPanel(message, detail, type = "") {
        if (locationStatus) {
            locationStatus.textContent = message;
            locationStatus.className = type ? `text-emphasis text-emphasis--${type}` : "text-emphasis";
        }

        if (currentLocation) {
            currentLocation.textContent = detail;
        }
    }

    async function refreshLocation() {
        updateLocationPanel("Locating", "Requesting your browser location to calculate distances and nearby centers.");
        setMessage(pageMessage, "Requesting your browser location...");

        try {
            const location = await requestBrowserLocation();
            state.location = location;
            setDiscoveryLocation(location);

            updateLocationPanel(
                "Enabled",
                `Approximate coordinates active: ${location.lat.toFixed(3)}, ${location.lng.toFixed(3)}. Distances are now served from the API.`,
                "success");
            setMessage(pageMessage, "Location enabled. Distances and nearby centers are now available.", "success");

            const filters = getTrimmedFormValues(searchForm);

            try {
                await Promise.all([
                    loadDirectory(filters),
                    loadNearbyCenters()
                ]);
            } catch (loadError) {
                setContainerMessage(centersContainer, loadError.message || "The public center list could not be refreshed.", "error");
                setMessage(pageMessage, loadError.message || "Location was enabled, but the center list could not be refreshed.", "error");
            }
        } catch (error) {
            updateLocationPanel("Unavailable", error.message || "Location access is currently unavailable.", "error");
            setMessage(pageMessage, error.message || "Location access is unavailable. Use city and country search instead.", "error");
            setContainerMessage(nearbyContainer, "Location access is unavailable. Use city or country search to find relevant centers.", "soft");
        }
    }

    locateButtons.forEach(button => {
        button.addEventListener("click", () => {
            refreshLocation();
        });
    });

    searchForm.addEventListener("submit", async event => {
        event.preventDefault();
        const filters = getTrimmedFormValues(searchForm);

        setMessage(searchMessage, "Searching the public center directory...");

        try {
            await loadDirectory(filters);
            setMessage(
                searchMessage,
                hasSearchFilters(filters)
                    ? "Search results loaded from the API."
                    : "Showing all published centers.",
                "success");
        } catch (error) {
            setContainerMessage(centersContainer, error.message || "Search could not be completed.", "error");
            setMessage(searchMessage, error.message || "Search could not be completed.", "error");
        }
    });

    clearButton?.addEventListener("click", async () => {
        searchForm.reset();
        setMessage(searchMessage, "Showing all published centers...");
        await loadDirectory();
        setMessage(searchMessage, "Showing all published centers.", "success");
    });

    setCardLoadingState(centersContainer, 4);
    setCardLoadingState(nearbyContainer, 3);

    if (state.location) {
        updateLocationPanel(
            "Enabled",
            `Using saved coordinates: ${state.location.lat.toFixed(3)}, ${state.location.lng.toFixed(3)}.`,
            "success");
    } else {
        updateLocationPanel("Checking", "No saved location was found. NoorLocator will try browser geolocation next.");
    }

    try {
        await loadDirectory();
    } catch (error) {
        setContainerMessage(centersContainer, error.message || "The public center directory could not be loaded.", "error");
        setMessage(pageMessage, error.message || "The public center directory could not be loaded.", "error");
    }

    if (state.location) {
        await loadNearbyCenters();
        setMessage(pageMessage, "Showing distances and nearby centers using your saved location.", "success");
        return;
    }

    if (new URLSearchParams(window.location.search).get("locate") === "1") {
        await refreshLocation();
        return;
    }

    await refreshLocation();
}

async function initCenterDetailsPage() {
    const title = document.getElementById("center-title");
    const meta = document.getElementById("center-meta");
    const description = document.getElementById("center-description");
    const languages = document.getElementById("center-languages");
    const majalis = document.getElementById("center-majalis");
    const gallery = document.getElementById("center-gallery");
    const announcements = document.getElementById("center-announcements");
    const infoGrid = document.getElementById("center-info-grid");
    const detailMessage = document.getElementById("center-detail-message");
    const mapLink = document.getElementById("center-map-link");
    const heroImage = document.getElementById("center-hero-image");
    const heroFallback = document.getElementById("center-logo-fallback");
    const params = new URLSearchParams(window.location.search);
    const id = Number(params.get("id"));
    const location = getDiscoveryLocation();

    if (!Number.isInteger(id) || id <= 0) {
        if (title) {
            title.textContent = "Center not found";
        }

        if (description) {
            description.textContent = "The requested center identifier is invalid.";
        }

        setMessage(detailMessage, "Open the center directory and choose a valid center.", "error");
        renderLanguageChips(languages, []);
        renderMajalis(majalis, []);
        renderCenterGallery(gallery, []);
        renderCenterAnnouncements(announcements, []);
        if (infoGrid) {
            infoGrid.innerHTML = "";
        }
        applyCenterHeroImage(heroImage, heroFallback, "NoorLocator", []);
        return;
    }

    if (title) {
        title.textContent = "Loading center details...";
    }

    if (meta) {
        meta.innerHTML = `<span class="status-pill">Loading profile data</span>`;
    }

    if (description) {
        description.textContent = "Fetching this center from the public NoorLocator API.";
    }

    if (languages) {
        languages.innerHTML = `<span class="empty-state">Loading supported languages...</span>`;
    }

    if (majalis) {
        majalis.innerHTML = `<div class="empty-state">Loading upcoming majalis...</div>`;
    }

    if (gallery) {
        gallery.innerHTML = `<div class="empty-state">Loading center gallery...</div>`;
    }

    if (announcements) {
        announcements.innerHTML = `<div class="empty-state">Loading center announcements...</div>`;
    }

    if (infoGrid) {
        infoGrid.innerHTML = Array.from({ length: 3 }, () => `
            <div class="info-card info-card--loading">
                <span class="skeleton skeleton--line skeleton--sm"></span>
                <span class="skeleton skeleton--line"></span>
            </div>
        `).join("");
    }

    try {
        const [centerResult, languagesResult, majalisResult, imagesResult, announcementsResult] = await Promise.allSettled([
            window.NoorLocatorApi.getCenter(id, appendLocationParams({}, location)),
            window.NoorLocatorApi.getCenterLanguages(id),
            window.NoorLocatorApi.getCenterMajalis(id),
            window.NoorLocatorApi.getCenterImages(id),
            window.NoorLocatorApi.getEventAnnouncements(id)
        ]);

        if (centerResult.status !== "fulfilled") {
            throw centerResult.reason;
        }

        const center = centerResult.value.data;
        const centerLanguages = languagesResult.status === "fulfilled"
            ? (languagesResult.value.data || [])
            : (center.languages || []);
        const centerMajalis = majalisResult.status === "fulfilled"
            ? (majalisResult.value.data || [])
            : (center.majalis || []);
        const centerImages = imagesResult.status === "fulfilled"
            ? (imagesResult.value.data || [])
            : [];
        const centerAnnouncements = announcementsResult.status === "fulfilled"
            ? (announcementsResult.value.data || [])
            : [];

        document.title = `${center.name} | NoorLocator`;
        title.textContent = center.name;
        meta.innerHTML = `
            <span class="status-pill">${escapeHtml(`${center.city}, ${center.country}`)}</span>
            <span class="status-pill status-pill--muted">${escapeHtml(center.address)}</span>
            ${typeof center.distanceKm === "number" ? `<span class="status-pill status-pill--success">${escapeHtml(formatDistance(center.distanceKm))}</span>` : ""}
        `;
        description.textContent = center.description || "This center has not published a public description yet.";
        renderLanguageChips(languages, centerLanguages);
        renderMajalis(majalis, centerMajalis);
        renderCenterGallery(gallery, centerImages, {
            omitPrimary: true
        });
        renderCenterAnnouncements(announcements, centerAnnouncements);
        applyCenterHeroImage(heroImage, heroFallback, center.name, centerImages);

        if (infoGrid) {
            infoGrid.innerHTML = `
                <article class="info-card">
                    <span class="card__meta">Address</span>
                    <strong>${escapeHtml(center.address)}</strong>
                    <p>${escapeHtml(`${center.city}, ${center.country}`)}</p>
                </article>
                <article class="info-card">
                    <span class="card__meta">Distance</span>
                    <strong>${escapeHtml(typeof center.distanceKm === "number" ? formatDistance(center.distanceKm) : "Unavailable")}</strong>
                    <p>${typeof center.distanceKm === "number" ? "Approximate distance calculated server-side." : "Enable location to see approximate distance."}</p>
                </article>
                <article class="info-card">
                    <span class="card__meta">Coordinates</span>
                    <strong>${escapeHtml(center.latitude.toFixed(4))}, ${escapeHtml(center.longitude.toFixed(4))}</strong>
                    <p>Use the map button to open turn-by-turn directions.</p>
                </article>
            `;
        }

        if (mapLink) {
            mapLink.href = buildMapLink(center);
        }

        if (
            languagesResult.status !== "fulfilled" ||
            majalisResult.status !== "fulfilled" ||
            imagesResult.status !== "fulfilled" ||
            announcementsResult.status !== "fulfilled"
        ) {
            setMessage(detailMessage, "Some supporting sections could not be refreshed independently, so NoorLocator used the center detail payload as a fallback.", "error");
            return;
        }

        setMessage(detailMessage, "Center profile loaded successfully from the public API.", "success");
    } catch (error) {
        if (title) {
            title.textContent = "Center profile unavailable";
        }

        if (meta) {
            meta.innerHTML = `<span class="status-pill status-pill--muted">Profile unavailable</span>`;
        }

        if (description) {
            description.textContent = error.message || "Center details could not be loaded.";
        }

        renderLanguageChips(languages, []);
        renderMajalis(majalis, []);
        renderCenterGallery(gallery, []);
        renderCenterAnnouncements(announcements, []);
        applyCenterHeroImage(heroImage, heroFallback, "NoorLocator", []);

        if (infoGrid) {
            infoGrid.innerHTML = `<div class="empty-state empty-state--error">The center profile could not be loaded from the API right now.</div>`;
        }

        setMessage(detailMessage, error.message || "Center details could not be loaded.", "error");
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

function initProfilePage() {
    if (!window.NoorLocatorAuth.requireAuth()) {
        return;
    }

    const pageMessage = document.getElementById("profile-page-message");
    const cardsContainer = document.getElementById("profile-cards");
    const form = document.getElementById("profile-form");
    const formMessage = document.querySelector('[data-form-message="profile-form"]');
    const nameInput = document.querySelector('#profile-form input[name="name"]');
    const emailInput = document.querySelector('#profile-form input[name="email"]');
    const roleBadge = document.getElementById("profile-role-badge");
    const createdAt = document.getElementById("profile-created-at");
    const roleDisplay = document.getElementById("profile-role-display");
    const centerCount = document.getElementById("profile-center-count");
    const workspaceLink = document.getElementById("profile-workspace-link");
    const state = {
        profile: window.NoorLocatorAuth.getSessionUser()
    };

    if (!pageMessage || !cardsContainer || !form || !formMessage || !nameInput || !emailInput || !roleBadge || !createdAt || !roleDisplay || !centerCount || !workspaceLink) {
        return;
    }

    setCardLoadingState(cardsContainer, 3);
    setMessage(pageMessage, "Loading your profile...");

    function getWorkspaceTarget(profile) {
        if (profile?.role === "Admin") {
            return {
                href: "admin.html",
                label: "Back to admin workspace"
            };
        }

        if (profile?.role === "Manager") {
            return {
                href: "manager.html",
                label: "Back to manager workspace"
            };
        }

        return {
            href: "dashboard.html",
            label: "Back to dashboard"
        };
    }

    function renderProfile(profile) {
        if (!profile) {
            return;
        }

        nameInput.value = profile.name || "";
        emailInput.value = profile.email || "";
        roleBadge.textContent = profile.role || "User";
        roleDisplay.textContent = profile.role || "User";
        createdAt.textContent = profile.createdAt ? formatDateTime(profile.createdAt) : "Unknown";

        const assignedCenterCount = (profile.assignedCenterIds || []).length;
        centerCount.textContent = `${assignedCenterCount} center${assignedCenterCount === 1 ? "" : "s"}`;

        const workspaceTarget = getWorkspaceTarget(profile);
        workspaceLink.href = workspaceTarget.href;
        workspaceLink.textContent = workspaceTarget.label;
    }

    function refreshOverviewCards() {
        const profile = state.profile || window.NoorLocatorAuth.getSessionUser() || { name: "Member", role: "User", email: "" };
        const assignedCenterCount = (profile.assignedCenterIds || []).length;

        populateCards("profile-cards", [
            {
                title: "Signed in as",
                body: `${profile.name} is currently authenticated as ${profile.role}. Profile edits update personal details only and do not change permissions.`
            },
            {
                title: "Email on file",
                body: profile.email
                    ? `${profile.email} is the account email NoorLocator currently uses for sign-in and account contact.`
                    : "No account email is available right now."
            },
            {
                title: "Assigned centers",
                body: assignedCenterCount
                    ? `${assignedCenterCount} approved center assignment${assignedCenterCount === 1 ? "" : "s"} are linked to this account.`
                    : "No approved center assignments are currently linked to this account."
            }
        ]);
    }

    async function loadProfile(successMessage = "Your profile is ready to edit.") {
        try {
            const [userResponse, profileResponse] = await Promise.all([
                window.NoorLocatorAuth.syncCurrentUser(),
                window.NoorLocatorApi.getMyProfile()
            ]);

            state.profile = profileResponse.data || userResponse || window.NoorLocatorAuth.getSessionUser();
            renderProfile(state.profile);
            refreshOverviewCards();
            setMessage(pageMessage, successMessage, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Your profile could not be loaded right now.");
            setMessage(pageMessage, message, "error");
            setMessage(formMessage, message, "error");
            showToast(message, "error");
        }
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        setSubmitButtonState(form, true, "Saving profile...");
        setMessage(formMessage, "Saving your profile...");

        const values = getTrimmedFormValues(form);
        const payload = {
            name: values.name,
            email: values.email
        };

        try {
            const response = await window.NoorLocatorApi.updateMyProfile(payload);
            state.profile = response.data || state.profile;

            if (state.profile) {
                window.NoorLocatorAuth.updateSessionUser(state.profile);
                renderProfile(state.profile);
                refreshOverviewCards();
            }

            setMessage(formMessage, response.message || "Profile updated successfully.", "success");
            setMessage(pageMessage, response.message || "Profile updated successfully.", "success");
            showToast(response.message || "Profile updated successfully.", "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Your profile could not be updated.");
            setMessage(formMessage, message, "error");
            setMessage(pageMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(form, false, "Saving profile...");
        }
    });

    window.addEventListener("noorlocator:auth-changed", event => {
        if (!event.detail) {
            return;
        }

        state.profile = {
            ...state.profile,
            ...event.detail
        };
        renderProfile(state.profile);
        refreshOverviewCards();
    });

    loadProfile();
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

    const pageMessage = document.getElementById("dashboard-page-message");
    const cardsContainer = document.getElementById("dashboard-cards");
    const centerRequestForm = document.getElementById("center-request-form");
    const suggestionForm = document.getElementById("suggestion-form");
    const languageSuggestionForm = document.getElementById("language-suggestion-form");
    const managerRequestForm = document.getElementById("manager-request-form");
    const myRequestsContainer = document.getElementById("my-center-requests");
    const centerSelects = Array.from(document.querySelectorAll("[data-center-select]"));
    const languageSelect = document.getElementById("language-select");
    const savedLocation = getDiscoveryLocation();
    const state = {
        user: window.NoorLocatorAuth.getSessionUser(),
        centers: [],
        languages: [],
        requests: []
    };

    if (!cardsContainer || !centerRequestForm || !suggestionForm || !languageSuggestionForm || !managerRequestForm || !myRequestsContainer || !languageSelect) {
        return;
    }

    setCardLoadingState(cardsContainer, 4);
    setContainerMessage(myRequestsContainer, "Loading your center requests...", "soft");
    setMessage(pageMessage, "Loading your contribution tools...");

    const latitudeInput = centerRequestForm.elements.namedItem("latitude");
    const longitudeInput = centerRequestForm.elements.namedItem("longitude");
    if (savedLocation && latitudeInput instanceof HTMLInputElement && longitudeInput instanceof HTMLInputElement) {
        latitudeInput.value = String(savedLocation.lat);
        longitudeInput.value = String(savedLocation.lng);
    }

    function refreshOverviewCards() {
        const currentUser = state.user || window.NoorLocatorAuth.getSessionUser() || { name: "Contributor", role: "User" };
        populateCards("dashboard-cards", [
            {
                title: "Signed in",
                body: `${currentUser.name} is contributing as ${currentUser.role}. Every submission enters moderation before it reaches the public directory.`
            },
            {
                title: "My center requests",
                body: state.requests.length
                    ? `You have ${state.requests.length} center request${state.requests.length === 1 ? "" : "s"} on file, with statuses such as Pending, Approved, and Rejected.`
                    : "You have not submitted a center request yet."
            },
            {
                title: "Published centers",
                body: state.centers.length
                    ? `${state.centers.length} published center${state.centers.length === 1 ? "" : "s"} are available for manager requests and language suggestions.`
                    : "Published centers could not be loaded right now."
            },
            {
                title: "Predefined languages",
                body: state.languages.length
                    ? `${state.languages.length} predefined language option${state.languages.length === 1 ? "" : "s"} are available from the live API.`
                    : "Language lookup data is currently unavailable."
            }
        ]);
    }

    window.addEventListener("noorlocator:auth-changed", event => {
        if (!event.detail) {
            return;
        }

        state.user = event.detail;
        refreshOverviewCards();
    });

    function refreshSelects() {
        populateSelectOptions(centerSelects, state.centers, {
            placeholder: state.centers.length ? "Select a center" : "No published centers available",
            getValue: center => String(center.id),
            getLabel: center => `${center.name} (${center.city}, ${center.country})`
        });

        populateSelectOptions([languageSelect], state.languages, {
            placeholder: state.languages.length ? "Select a language" : "No languages available",
            getValue: language => String(language.id),
            getLabel: language => `${language.name} (${language.code})`
        });
    }

    async function refreshMyRequests() {
        try {
            const response = await window.NoorLocatorApi.getMyCenterRequests();
            state.requests = response.data || [];
            renderCenterRequestList(myRequestsContainer, state.requests);
            refreshOverviewCards();
        } catch (error) {
            const message = normalizeErrorMessage(error, "Your center requests could not be loaded right now.");
            setContainerMessage(myRequestsContainer, message, "error");
            setMessage(pageMessage, message, "error");
        }
    }

    function bindContributionForm(form, submitAction, buildPayload, options) {
        const messageElement = document.querySelector(`[data-form-message="${form.id}"]`);
        const {
            busyMessage,
            fallbackSuccessMessage,
            onSuccess
        } = options;

        form.addEventListener("submit", async event => {
            event.preventDefault();
            setSubmitButtonState(form, true, busyMessage);
            setMessage(messageElement, busyMessage);

            try {
                const payload = buildPayload(getTrimmedFormValues(form));
                const response = await submitAction(payload);
                const successMessage = response.message || fallbackSuccessMessage;

                setMessage(messageElement, successMessage, "success");
                showToast(successMessage, "success");
                form.reset();

                if (form === centerRequestForm && savedLocation && latitudeInput instanceof HTMLInputElement && longitudeInput instanceof HTMLInputElement) {
                    latitudeInput.value = String(savedLocation.lat);
                    longitudeInput.value = String(savedLocation.lng);
                }

                if (typeof onSuccess === "function") {
                    await onSuccess(response);
                }
            } catch (error) {
                const message = normalizeErrorMessage(error, "The request could not be completed.");
                setMessage(messageElement, message, "error");
                showToast(message, "error");
            } finally {
                setSubmitButtonState(form, false, busyMessage);
            }
        });
    }

    bindContributionForm(
        centerRequestForm,
        payload => window.NoorLocatorApi.createCenterRequest(payload),
        values => ({
            name: values.name,
            address: values.address,
            city: values.city,
            country: values.country,
            latitude: Number(values.latitude),
            longitude: Number(values.longitude),
            description: values.description
        }),
        {
            busyMessage: "Submitting center request...",
            fallbackSuccessMessage: "Center request submitted for review.",
            onSuccess: async () => {
                await refreshMyRequests();
            }
        });

    bindContributionForm(
        suggestionForm,
        payload => window.NoorLocatorApi.createSuggestion(payload),
        values => ({
            type: values.type,
            message: values.message
        }),
        {
            busyMessage: "Submitting suggestion...",
            fallbackSuccessMessage: "Suggestion submitted for review."
        });

    bindContributionForm(
        languageSuggestionForm,
        payload => window.NoorLocatorApi.createCenterLanguageSuggestion(payload),
        values => ({
            centerId: Number(values.centerId),
            languageId: Number(values.languageId)
        }),
        {
            busyMessage: "Submitting language suggestion...",
            fallbackSuccessMessage: "Language suggestion submitted for review."
        });

    bindContributionForm(
        managerRequestForm,
        payload => window.NoorLocatorApi.requestManagerAccess(payload),
        values => ({
            centerId: Number(values.centerId)
        }),
        {
            busyMessage: "Submitting manager request...",
            fallbackSuccessMessage: "Manager request submitted for review."
        });

    Promise.allSettled([
        window.NoorLocatorAuth.syncCurrentUser(),
        window.NoorLocatorApi.getCenters(),
        window.NoorLocatorApi.getLanguages(),
        window.NoorLocatorApi.getMyCenterRequests()
    ]).then(results => {
        const [userResult, centersResult, languagesResult, requestsResult] = results;

        if (userResult.status === "fulfilled" && userResult.value) {
            state.user = userResult.value;
        }

        if (centersResult.status === "fulfilled") {
            state.centers = centersResult.value.data || [];
        }

        if (languagesResult.status === "fulfilled") {
            state.languages = languagesResult.value.data || [];
        }

        if (requestsResult.status === "fulfilled") {
            state.requests = requestsResult.value.data || [];
            renderCenterRequestList(myRequestsContainer, state.requests);
        } else {
            setContainerMessage(
                myRequestsContainer,
                normalizeErrorMessage(requestsResult.reason, "Your center requests could not be loaded right now."),
                "error");
        }

        refreshSelects();
        refreshOverviewCards();

        const failures = [centersResult, languagesResult, requestsResult]
            .filter(result => result.status === "rejected")
            .map(result => normalizeErrorMessage(result.reason, "A dashboard request failed."));

        if (failures.length) {
            const message = failures[0];
            setMessage(pageMessage, message, "error");
            showToast(message, "error");
            return;
        }

        setMessage(
            pageMessage,
            "Your contribution tools are ready. New submissions are stored as pending until a moderator reviews them.",
            "success");
    }).catch(error => {
        const message = normalizeErrorMessage(error, "The dashboard could not be loaded right now.");
        setMessage(pageMessage, message, "error");
        setContainerMessage(myRequestsContainer, message, "error");
        showToast(message, "error");
    });
}

function formatDateForInput(dateValue) {
    if (!dateValue) {
        return "";
    }

    const date = new Date(dateValue);
    if (Number.isNaN(date.getTime())) {
        return "";
    }

    return date.toISOString().slice(0, 10);
}

function formatFileSize(bytes) {
    if (!Number.isFinite(bytes) || bytes <= 0) {
        return "0 B";
    }

    if (bytes < 1024) {
        return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
        return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getCenterImageValidationError(file) {
    return getImageFileValidationError(file, {
        requiredMessage: "Choose an image file before uploading."
    });
}

function getMajlisImageValidationError(file) {
    return getImageFileValidationError(file);
}

function getImageFileValidationError(file, options = {}) {
    const {
        requiredMessage = ""
    } = options;

    if (!(file instanceof File)) {
        return requiredMessage;
    }

    const extension = file.name.includes(".")
        ? file.name.slice(file.name.lastIndexOf(".")).toLowerCase()
        : "";

    if (!CENTER_IMAGE_ALLOWED_EXTENSIONS.has(extension)) {
        return "Only JPG, JPEG, PNG, and WEBP files are allowed.";
    }

    if (file.size > CENTER_IMAGE_MAX_SIZE_BYTES) {
        return "Image files must be 5MB or smaller.";
    }

    return "";
}

function setUploadProgress(progressElement, metaElement, percent, message = "") {
    if (progressElement instanceof HTMLProgressElement) {
        progressElement.hidden = false;

        if (typeof percent === "number") {
            progressElement.max = 100;
            progressElement.value = percent;
        } else {
            progressElement.removeAttribute("value");
        }
    }

    if (metaElement) {
        metaElement.textContent = message;
    }
}

function resetUploadProgress(progressElement, metaElement) {
    if (progressElement instanceof HTMLProgressElement) {
        progressElement.hidden = true;
        progressElement.max = 100;
        progressElement.value = 0;
    }

    if (metaElement) {
        metaElement.textContent = "";
    }
}

function renderManagedCenters(container, centers) {
    if (!container) {
        return;
    }

    if (!centers.length) {
        setContainerMessage(container, "No approved centers are currently assigned to this manager account.", "soft");
        return;
    }

    container.innerHTML = centers.map(center => `
        <article class="card">
            <div class="card__header">
                <span class="card__meta">${escapeHtml(`${center.city}, ${center.country}`)}</span>
                <span class="status-pill status-pill--success">Assigned</span>
            </div>
            <h3>${escapeHtml(center.name)}</h3>
            <p class="card__excerpt">${escapeHtml(truncateText(center.description || "This center is ready for majlis publishing through the manager workspace.", 150))}</p>
            <p>${escapeHtml(center.address)}</p>
        </article>
    `).join("");
}

function renderMajlisLanguageOptions(container, languages, selectedIds = []) {
    if (!container) {
        return;
    }

    if (!languages.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">No predefined languages are available.</div>`;
        return;
    }

    const selected = new Set(selectedIds.map(Number));
    container.innerHTML = languages.map(language => `
        <label class="checkbox-card">
            <input type="checkbox" name="languageIds" value="${escapeHtml(language.id)}"${selected.has(Number(language.id)) ? " checked" : ""}>
            <span>
                <strong>${escapeHtml(language.name)}</strong>
                <span>${escapeHtml(language.code)}</span>
            </span>
        </label>
    `).join("");
}

function getSelectedLanguageIds(container) {
    if (!container) {
        return [];
    }

    return Array.from(container.querySelectorAll('input[name="languageIds"]:checked'))
        .map(input => Number(input.value))
        .filter(languageId => Number.isInteger(languageId) && languageId > 0);
}

function renderManagerMajalis(container, majalis) {
    if (!container) {
        return;
    }

    if (!majalis.length) {
        setContainerMessage(container, "No majalis are published for the selected center yet.", "soft");
        return;
    }

    container.innerHTML = majalis.map(majlis => `
        <article class="list-card">
            ${majlis.imageUrl ? `<img class="majlis-card__image" src="${escapeHtml(majlis.imageUrl)}" alt="${escapeHtml(`${majlis.title} image`)}" loading="lazy">` : ""}
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(majlis.title)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${majlis.centerName} (${majlis.centerCity}, ${majlis.centerCountry})`)}</p>
                </div>
                <span class="status-pill">${escapeHtml(formatDateTime(majlis.date))}</span>
            </div>
            <p>${escapeHtml(truncateText(majlis.description || "No public description is available for this majlis yet.", 190))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">Time: ${escapeHtml(majlis.time || "To be announced")}</span>
                ${(majlis.languages || []).map(language => `<span class="chip chip--muted">${escapeHtml(language.name)}</span>`).join("")}
            </div>
            <div class="button-row">
                <button class="button button--secondary" type="button" data-edit-majlis-id="${escapeHtml(majlis.id)}">Edit</button>
                <button class="button button--danger" type="button" data-delete-majlis-id="${escapeHtml(majlis.id)}" data-majlis-title="${escapeHtml(majlis.title)}">Delete</button>
            </div>
        </article>
    `).join("");
}

function renderManagerAnnouncements(container, announcements) {
    renderCenterAnnouncements(container, announcements, {
        manageable: true,
        emptyMessage: "No announcements exist for the selected center yet."
    });
}

function renderManagerCenterImages(container, images) {
    renderCenterGallery(container, images, {
        manageable: true,
        emptyMessage: "No gallery images have been uploaded for the selected center yet.",
        imageAlt: "Managed center gallery image"
    });
}

function renderAdminCenterImages(container, images) {
    renderCenterGallery(container, images, {
        manageable: true,
        emptyMessage: "No gallery images are currently stored for the selected center.",
        imageAlt: "Admin moderated center gallery image"
    });
}

function bindImageGalleryActions(container, handlers) {
    if (!container) {
        return;
    }

    container.querySelectorAll("[data-set-primary-image-id]").forEach(button => {
        button.addEventListener("click", async () => {
            const imageId = Number(button.getAttribute("data-set-primary-image-id"));
            if (!Number.isInteger(imageId) || imageId <= 0 || typeof handlers.onSetPrimary !== "function") {
                return;
            }

            await handlers.onSetPrimary(imageId);
        });
    });

    container.querySelectorAll("[data-delete-center-image-id]").forEach(button => {
        button.addEventListener("click", async () => {
            const imageId = Number(button.getAttribute("data-delete-center-image-id"));
            if (!Number.isInteger(imageId) || imageId <= 0 || typeof handlers.onDelete !== "function") {
                return;
            }

            await handlers.onDelete(imageId);
        });
    });
}

function initManagerPage() {
    if (!window.NoorLocatorAuth.requireAuth(["Manager", "Admin"])) {
        return;
    }

    const pageMessage = document.getElementById("manager-page-message");
    const centerCount = document.getElementById("manager-center-count");
    const majlisCount = document.getElementById("manager-majlis-count");
    const announcementCount = document.getElementById("manager-announcement-count");
    const imageCount = document.getElementById("manager-image-count");
    const cardsContainer = document.getElementById("manager-cards");
    const centersContainer = document.getElementById("manager-centers");
    const majlisListContainer = document.getElementById("manager-majalis");
    const announcementsContainer = document.getElementById("manager-announcements");
    const centerImagesContainer = document.getElementById("manager-center-images");
    const form = document.getElementById("majlis-form");
    const formMessage = document.querySelector('[data-form-message="majlis-form"]');
    const formHeading = document.getElementById("majlis-form-heading");
    const submitButton = document.getElementById("majlis-submit-button");
    const cancelButton = document.getElementById("majlis-cancel-button");
    const refreshButton = document.getElementById("refresh-majalis-button");
    const formCenterSelect = document.getElementById("majlis-center-select");
    const filterCenterSelect = document.getElementById("majlis-filter-center");
    const languageOptions = document.getElementById("majlis-language-options");
    const majlisImageInput = form?.elements?.namedItem("image");
    const announcementForm = document.getElementById("event-announcement-form");
    const announcementFormMessage = document.querySelector('[data-form-message="event-announcement-form"]');
    const announcementFormHeading = document.getElementById("event-announcement-form-heading");
    const announcementSubmitButton = document.getElementById("event-announcement-submit-button");
    const announcementCancelButton = document.getElementById("event-announcement-cancel-button");
    const announcementFormCenterSelect = document.getElementById("announcement-center-select");
    const announcementFilterCenterSelect = document.getElementById("announcement-filter-center");
    const refreshAnnouncementsButton = document.getElementById("refresh-announcements-button");
    const imageUploadForm = document.getElementById("center-image-upload-form");
    const imageUploadFormMessage = document.querySelector('[data-form-message="center-image-upload-form"]');
    const imageUploadProgress = document.getElementById("center-image-upload-progress");
    const imageUploadProgressMeta = document.getElementById("center-image-upload-progress-meta");
    const imageUploadInput = imageUploadForm?.elements?.namedItem("image");
    const imageFormCenterSelect = document.getElementById("center-image-center-select");
    const imageFilterCenterSelect = document.getElementById("center-image-filter-center");
    const refreshCenterImagesButton = document.getElementById("refresh-center-images-button");
    const state = {
        user: window.NoorLocatorAuth.getSessionUser(),
        centers: [],
        languages: [],
        majalis: [],
        announcements: [],
        centerImages: [],
        selectedCenterId: null,
        editingMajlisId: null,
        editingAnnouncementId: null
    };

    if (
        !pageMessage ||
        !centerCount ||
        !majlisCount ||
        !announcementCount ||
        !imageCount ||
        !cardsContainer ||
        !centersContainer ||
        !majlisListContainer ||
        !announcementsContainer ||
        !centerImagesContainer ||
        !form ||
        !formMessage ||
        !formHeading ||
        !submitButton ||
        !cancelButton ||
        !refreshButton ||
        !formCenterSelect ||
        !filterCenterSelect ||
        !languageOptions ||
        !announcementForm ||
        !announcementFormMessage ||
        !announcementFormHeading ||
        !announcementSubmitButton ||
        !announcementCancelButton ||
        !announcementFormCenterSelect ||
        !announcementFilterCenterSelect ||
        !refreshAnnouncementsButton ||
        !imageUploadForm ||
        !imageUploadFormMessage ||
        !imageUploadProgress ||
        !imageUploadProgressMeta ||
        !imageFormCenterSelect ||
        !imageFilterCenterSelect ||
        !refreshCenterImagesButton
    ) {
        return;
    }

    setCardLoadingState(cardsContainer, 3);
    setContainerMessage(centersContainer, "Loading your assigned centers...", "soft");
    setContainerMessage(majlisListContainer, "Loading majalis...", "soft");
    setContainerMessage(announcementsContainer, "Loading announcements...", "soft");
    setContainerMessage(centerImagesContainer, "Loading center gallery...", "soft");
    setMessage(pageMessage, "Loading your manager workspace...");

    function updateCounts() {
        centerCount.textContent = String(state.centers.length);
        majlisCount.textContent = String(state.majalis.length);
        announcementCount.textContent = String(state.announcements.length);
        imageCount.textContent = String(state.centerImages.length);
    }

    function refreshOverviewCards() {
        const currentUser = state.user || window.NoorLocatorAuth.getSessionUser() || { name: "Manager", role: "Manager" };
        populateCards("manager-cards", [
            {
                title: "Manager session",
                body: `${currentUser.name} is signed in as ${currentUser.role}. Majalis, announcements, and gallery changes are accepted only for approved center assignments.`
            },
            {
                title: "Assigned centers",
                body: state.centers.length
                    ? `${state.centers.length} center${state.centers.length === 1 ? "" : "s"} are available in this workspace.`
                    : "No assigned centers are available for this account."
            },
            {
                title: "Majalis in view",
                body: state.majalis.length
                    ? `${state.majalis.length} majlis record${state.majalis.length === 1 ? "" : "s"} are loaded for the selected center.`
                    : "No majalis are currently loaded for the selected center."
            },
            {
                title: "Direct announcements",
                body: state.announcements.length
                    ? `${state.announcements.length} announcement${state.announcements.length === 1 ? "" : "s"} are loaded for the selected center.`
                    : "No announcements are currently loaded for the selected center."
            },
            {
                title: "Center gallery",
                body: state.centerImages.length
                    ? `${state.centerImages.length} image${state.centerImages.length === 1 ? "" : "s"} are available for the selected center.`
                    : "No gallery images are currently loaded for the selected center."
            }
        ]);
    }

    window.addEventListener("noorlocator:auth-changed", event => {
        if (!event.detail) {
            return;
        }

        state.user = event.detail;
        refreshOverviewCards();
    });

    function syncCenterSelection(centerId) {
        const validCenterId = state.centers.some(center => center.id === centerId)
            ? centerId
            : (state.centers[0]?.id || null);

        state.selectedCenterId = validCenterId;

        const selectedValue = validCenterId ? String(validCenterId) : "";
        [
            filterCenterSelect,
            announcementFilterCenterSelect,
            imageFilterCenterSelect
        ].forEach(select => {
            select.value = selectedValue;
        });

        if (!state.editingMajlisId) {
            formCenterSelect.value = selectedValue;
        }

        if (!state.editingAnnouncementId) {
            announcementFormCenterSelect.value = selectedValue;
        }

        imageFormCenterSelect.value = selectedValue;
    }

    function resetMajlisForm(preferredCenterId = null) {
        state.editingMajlisId = null;
        form.reset();
        form.elements.namedItem("majlisId").value = "";
        formHeading.textContent = "Create a new majlis";
        submitButton.textContent = "Create majlis";
        submitButton.dataset.defaultLabel = "Create majlis";
        cancelButton.hidden = true;

        const fallbackCenterId = preferredCenterId || state.selectedCenterId || state.centers[0]?.id || null;
        if (fallbackCenterId) {
            formCenterSelect.value = String(fallbackCenterId);
        }

        renderMajlisLanguageOptions(languageOptions, state.languages, []);
        const removeImageField = form.elements.namedItem("removeImage");
        if (removeImageField instanceof HTMLInputElement) {
            removeImageField.checked = false;
        }

        setMessage(formMessage, "Create a majlis for one of your assigned centers. You can optionally add a poster image.");
    }

    function resetAnnouncementForm(preferredCenterId = null) {
        state.editingAnnouncementId = null;
        announcementForm.reset();
        announcementForm.elements.namedItem("announcementId").value = "";
        announcementFormHeading.textContent = "Create a center announcement";
        announcementSubmitButton.textContent = "Publish announcement";
        announcementSubmitButton.dataset.defaultLabel = "Publish announcement";
        announcementCancelButton.hidden = true;

        const fallbackCenterId = preferredCenterId || state.selectedCenterId || state.centers[0]?.id || null;
        if (fallbackCenterId) {
            announcementFormCenterSelect.value = String(fallbackCenterId);
        }

        setMessage(announcementFormMessage, "Announcements publish directly for your assigned centers.");
    }

    function resetImageUploadForm(preferredCenterId = null) {
        imageUploadForm.reset();
        resetUploadProgress(imageUploadProgress, imageUploadProgressMeta);

        const fallbackCenterId = preferredCenterId || state.selectedCenterId || state.centers[0]?.id || null;
        if (fallbackCenterId) {
            imageFormCenterSelect.value = String(fallbackCenterId);
        }

        setMessage(imageUploadFormMessage, "Upload JPG, PNG, or WEBP files up to 5MB.");
    }

    function populateCenterControls() {
        populateSelectOptions(
            [
                formCenterSelect,
                filterCenterSelect,
                announcementFormCenterSelect,
                announcementFilterCenterSelect,
                imageFormCenterSelect,
                imageFilterCenterSelect
            ],
            state.centers,
            {
            placeholder: state.centers.length ? "Select a center" : "No centers available",
            getValue: center => String(center.id),
            getLabel: center => `${center.name} (${center.city}, ${center.country})`
            });

        if (!state.centers.length) {
            return;
        }

        syncCenterSelection(state.selectedCenterId || state.centers[0].id);
    }

    function bindMajlisListActions() {
        majlisListContainer.querySelectorAll("[data-edit-majlis-id]").forEach(button => {
            button.addEventListener("click", async () => {
                const majlisId = Number(button.getAttribute("data-edit-majlis-id"));
                if (!Number.isInteger(majlisId) || majlisId <= 0) {
                    return;
                }

                setMessage(formMessage, "Loading majlis details...");

                try {
                    const response = await window.NoorLocatorApi.getMajlis(majlisId);
                    const majlis = response.data;

                    if (!state.centers.some(center => center.id === majlis.centerId)) {
                        throw new Error("This majlis is outside your assigned centers.");
                    }

                    state.editingMajlisId = majlis.id;
                    form.elements.namedItem("majlisId").value = String(majlis.id);
                    form.elements.namedItem("title").value = majlis.title || "";
                    form.elements.namedItem("description").value = majlis.description || "";
                    form.elements.namedItem("date").value = formatDateForInput(majlis.date);
                    form.elements.namedItem("time").value = majlis.time || "";
                    form.elements.namedItem("removeImage").checked = false;
                    formCenterSelect.value = String(majlis.centerId);
                    syncCenterSelection(majlis.centerId);
                    formHeading.textContent = "Edit majlis";
                    submitButton.textContent = "Save changes";
                    submitButton.dataset.defaultLabel = "Save changes";
                    cancelButton.hidden = false;
                    renderMajlisLanguageOptions(languageOptions, state.languages, (majlis.languages || []).map(language => language.id));
                    setMessage(
                        formMessage,
                        majlis.imageUrl
                            ? `Editing "${majlis.title}". Leave the image empty to keep the current one, or check remove to clear it.`
                            : `Editing "${majlis.title}". You can add an optional image before saving.`,
                        "success");
                    document.getElementById("majlis-editor")?.scrollIntoView({ behavior: "smooth", block: "start" });
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Majlis details could not be loaded.");
                    setMessage(formMessage, message, "error");
                    showToast(message, "error");
                }
            });
        });

        majlisListContainer.querySelectorAll("[data-delete-majlis-id]").forEach(button => {
            button.addEventListener("click", async () => {
                const majlisId = Number(button.getAttribute("data-delete-majlis-id"));
                const title = button.getAttribute("data-majlis-title") || "this majlis";

                if (!Number.isInteger(majlisId) || majlisId <= 0) {
                    return;
                }

                if (!window.confirm(`Delete "${title}" from NoorLocator?`)) {
                    return;
                }

                try {
                    const response = await window.NoorLocatorApi.deleteMajlis(majlisId);
                    showToast(response.message || "Majlis deleted successfully.", "success");

                    if (state.editingMajlisId === majlisId) {
                        resetMajlisForm(state.selectedCenterId);
                    }

                    await loadMajalisForSelectedCenter();
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Majlis deletion could not be completed.");
                    showToast(message, "error");
                    setMessage(pageMessage, message, "error");
                }
            });
        });
    }

    function bindAnnouncementActions() {
        announcementsContainer.querySelectorAll("[data-edit-announcement-id]").forEach(button => {
            button.addEventListener("click", async () => {
                const announcementId = Number(button.getAttribute("data-edit-announcement-id"));
                if (!Number.isInteger(announcementId) || announcementId <= 0) {
                    return;
                }

                setMessage(announcementFormMessage, "Loading announcement details...");

                try {
                    const response = await window.NoorLocatorApi.getEventAnnouncement(announcementId);
                    const announcement = response.data;

                    if (!state.centers.some(center => center.id === announcement.centerId)) {
                        throw new Error("This announcement is outside your assigned centers.");
                    }

                    state.editingAnnouncementId = announcement.id;
                    announcementForm.elements.namedItem("announcementId").value = String(announcement.id);
                    announcementForm.elements.namedItem("title").value = announcement.title || "";
                    announcementForm.elements.namedItem("description").value = announcement.description || "";
                    announcementForm.elements.namedItem("status").value = announcement.status || "Published";
                    announcementForm.elements.namedItem("removeImage").checked = false;
                    announcementFormCenterSelect.value = String(announcement.centerId);
                    syncCenterSelection(announcement.centerId);
                    announcementFormHeading.textContent = "Edit center announcement";
                    announcementSubmitButton.textContent = "Save announcement";
                    announcementSubmitButton.dataset.defaultLabel = "Save announcement";
                    announcementCancelButton.hidden = false;
                    setMessage(
                        announcementFormMessage,
                        announcement.imageUrl
                            ? `Editing "${announcement.title}". Leave the image empty to keep the current one.`
                            : `Editing "${announcement.title}".`,
                        "success");
                    document.getElementById("announcement-editor")?.scrollIntoView({ behavior: "smooth", block: "start" });
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Announcement details could not be loaded.");
                    setMessage(announcementFormMessage, message, "error");
                    showToast(message, "error");
                }
            });
        });

        announcementsContainer.querySelectorAll("[data-delete-announcement-id]").forEach(button => {
            button.addEventListener("click", async () => {
                const announcementId = Number(button.getAttribute("data-delete-announcement-id"));
                const title = button.getAttribute("data-announcement-title") || "this announcement";

                if (!Number.isInteger(announcementId) || announcementId <= 0) {
                    return;
                }

                if (!window.confirm(`Delete "${title}" from NoorLocator?`)) {
                    return;
                }

                try {
                    const response = await window.NoorLocatorApi.deleteEventAnnouncement(announcementId);
                    if (state.editingAnnouncementId === announcementId) {
                        resetAnnouncementForm(state.selectedCenterId);
                    }

                    await loadAnnouncementsForSelectedCenter();
                    updateCounts();
                    refreshOverviewCards();
                    setMessage(pageMessage, response.message || "Announcement deleted successfully.", "success");
                    showToast(response.message || "Announcement deleted successfully.", "success");
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Announcement deletion could not be completed.");
                    setMessage(pageMessage, message, "error");
                    showToast(message, "error");
                }
            });
        });
    }

    function bindCenterImageActions() {
        bindImageGalleryActions(centerImagesContainer, {
            onSetPrimary: async imageId => {
                try {
                    const response = await window.NoorLocatorApi.setPrimaryCenterImage(imageId);
                    await loadCenterImagesForSelectedCenter();
                    updateCounts();
                    refreshOverviewCards();
                    setMessage(pageMessage, response.message || "Primary image updated successfully.", "success");
                    showToast(response.message || "Primary image updated successfully.", "success");
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Primary image could not be updated.");
                    setMessage(pageMessage, message, "error");
                    showToast(message, "error");
                }
            },
            onDelete: async imageId => {
                if (!window.confirm("Delete this center image from the gallery?")) {
                    return;
                }

                try {
                    const response = await window.NoorLocatorApi.deleteCenterImage(imageId);
                    await loadCenterImagesForSelectedCenter();
                    updateCounts();
                    refreshOverviewCards();
                    setMessage(pageMessage, response.message || "Center image deleted successfully.", "success");
                    showToast(response.message || "Center image deleted successfully.", "success");
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Center image could not be deleted.");
                    setMessage(pageMessage, message, "error");
                    showToast(message, "error");
                }
            }
        });
    }

    async function loadMajalisForSelectedCenter() {
        if (!state.selectedCenterId) {
            state.majalis = [];
            setContainerMessage(majlisListContainer, "Select one of your assigned centers to manage majalis.", "soft");
            return;
        }

        setContainerMessage(majlisListContainer, "Loading majalis for the selected center...", "soft");

        try {
            const response = await window.NoorLocatorApi.getMajalis(state.selectedCenterId);
            state.majalis = (response.data || []).filter(majlis =>
                state.centers.some(center => center.id === majlis.centerId));

            renderManagerMajalis(majlisListContainer, state.majalis);
            bindMajlisListActions();
        } catch (error) {
            const message = normalizeErrorMessage(error, "Majalis could not be loaded for the selected center.");
            state.majalis = [];
            setContainerMessage(majlisListContainer, message, "error");
        }
    }

    async function loadAnnouncementsForSelectedCenter() {
        if (!state.selectedCenterId) {
            state.announcements = [];
            setContainerMessage(announcementsContainer, "Select one of your assigned centers to manage announcements.", "soft");
            return;
        }

        setContainerMessage(announcementsContainer, "Loading announcements for the selected center...", "soft");

        try {
            const response = await window.NoorLocatorApi.getEventAnnouncements(state.selectedCenterId);
            state.announcements = response.data || [];

            renderManagerAnnouncements(announcementsContainer, state.announcements);
            bindAnnouncementActions();
        } catch (error) {
            const message = normalizeErrorMessage(error, "Announcements could not be loaded for the selected center.");
            state.announcements = [];
            setContainerMessage(announcementsContainer, message, "error");
        }
    }

    async function loadCenterImagesForSelectedCenter() {
        if (!state.selectedCenterId) {
            state.centerImages = [];
            setContainerMessage(centerImagesContainer, "Select one of your assigned centers to manage the gallery.", "soft");
            return;
        }

        setContainerMessage(centerImagesContainer, "Loading center gallery...", "soft");

        try {
            const response = await window.NoorLocatorApi.getCenterImages(state.selectedCenterId);
            state.centerImages = response.data || [];

            renderManagerCenterImages(centerImagesContainer, state.centerImages);
            bindCenterImageActions();
        } catch (error) {
            const message = normalizeErrorMessage(error, "Center images could not be loaded for the selected center.");
            state.centerImages = [];
            setContainerMessage(centerImagesContainer, message, "error");
        }
    }

    async function refreshSelectedCenterWorkspace(successMessage = "Manager workspace loaded from the live API.") {
        try {
            await Promise.all([
                loadMajalisForSelectedCenter(),
                loadAnnouncementsForSelectedCenter(),
                loadCenterImagesForSelectedCenter()
            ]);
            updateCounts();
            refreshOverviewCards();
            setMessage(pageMessage, successMessage, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "The manager workspace could not be loaded right now.");
            setMessage(pageMessage, message, "error");
        }
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        setSubmitButtonState(form, true, state.editingMajlisId ? "Saving changes..." : "Creating majlis...");
        setMessage(formMessage, state.editingMajlisId ? "Saving majlis changes..." : "Creating majlis...");

        const selectedFile = majlisImageInput instanceof HTMLInputElement
            ? majlisImageInput.files?.[0]
            : null;
        const imageValidationError = getMajlisImageValidationError(selectedFile);
        if (imageValidationError) {
            setMessage(formMessage, imageValidationError, "error");
            setSubmitButtonState(form, false, state.editingMajlisId ? "Saving changes..." : "Creating majlis...");
            return;
        }

        const formData = new FormData(form);
        const centerId = Number(formData.get("centerId"));
        const dateValue = String(formData.get("date") || "").trim();
        const timeValue = String(formData.get("time") || "").trim();

        formData.set("centerId", Number.isInteger(centerId) && centerId > 0 ? String(centerId) : "");
        formData.set("date", dateValue ? `${dateValue}T00:00:00` : "");
        formData.set("time", timeValue);
        formData.delete("languageIds");

        getSelectedLanguageIds(languageOptions)
            .forEach(languageId => formData.append("languageIds", String(languageId)));

        try {
            const response = state.editingMajlisId
                ? await window.NoorLocatorApi.updateMajlis(state.editingMajlisId, formData)
                : await window.NoorLocatorApi.createMajlis(formData);

            syncCenterSelection(centerId);
            await refreshSelectedCenterWorkspace(response.message);
            resetMajlisForm(centerId);
            setMessage(formMessage, response.message, "success");
            showToast(response.message, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Majlis changes could not be saved.");
            setMessage(formMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(form, false, state.editingMajlisId ? "Saving changes..." : "Creating majlis...");
        }
    });

    if (majlisImageInput instanceof HTMLInputElement) {
        majlisImageInput.addEventListener("change", () => {
            const file = majlisImageInput.files?.[0];

            if (!file) {
                setMessage(
                    formMessage,
                    state.editingMajlisId
                        ? "Leave the image empty to keep the current one, or choose a new file to replace it."
                        : "Create a majlis for one of your assigned centers. You can optionally add a poster image.");
                return;
            }

            const validationError = getMajlisImageValidationError(file);
            if (validationError) {
                majlisImageInput.value = "";
                setMessage(formMessage, validationError, "error");
                return;
            }

            setMessage(formMessage, `${file.name} is ready to upload with this majlis (${formatFileSize(file.size)}).`);
        });
    }

    cancelButton.addEventListener("click", () => {
        resetMajlisForm(state.selectedCenterId);
    });

    announcementForm.addEventListener("submit", async event => {
        event.preventDefault();

        setSubmitButtonState(
            announcementForm,
            true,
            state.editingAnnouncementId ? "Saving announcement..." : "Publishing announcement...");
        setMessage(
            announcementFormMessage,
            state.editingAnnouncementId ? "Saving announcement changes..." : "Publishing announcement...");

        const formData = new FormData(announcementForm);
        const centerId = Number(formData.get("centerId"));

        try {
            const response = state.editingAnnouncementId
                ? await window.NoorLocatorApi.updateEventAnnouncement(state.editingAnnouncementId, formData)
                : await window.NoorLocatorApi.createEventAnnouncement(formData);

            syncCenterSelection(centerId);
            await refreshSelectedCenterWorkspace(response.message);
            resetAnnouncementForm(centerId);
            setMessage(announcementFormMessage, response.message, "success");
            showToast(response.message, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Announcement changes could not be saved.");
            setMessage(announcementFormMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(
                announcementForm,
                false,
                state.editingAnnouncementId ? "Saving announcement..." : "Publishing announcement...");
        }
    });

    announcementCancelButton.addEventListener("click", () => {
        resetAnnouncementForm(state.selectedCenterId);
    });

    if (imageUploadInput instanceof HTMLInputElement) {
        imageUploadInput.addEventListener("change", () => {
            const file = imageUploadInput.files?.[0];
            resetUploadProgress(imageUploadProgress, imageUploadProgressMeta);

            if (!file) {
                setMessage(imageUploadFormMessage, "Upload JPG, PNG, or WEBP files up to 5MB.");
                return;
            }

            const validationError = getCenterImageValidationError(file);
            if (validationError) {
                imageUploadInput.value = "";
                setMessage(imageUploadFormMessage, validationError, "error");
                return;
            }

            setMessage(
                imageUploadFormMessage,
                `${file.name} is ready to upload (${formatFileSize(file.size)}).`);
        });
    }

    imageUploadForm.addEventListener("submit", async event => {
        event.preventDefault();

        const selectedFile = imageUploadInput instanceof HTMLInputElement
            ? imageUploadInput.files?.[0]
            : null;
        const validationError = getCenterImageValidationError(selectedFile);
        if (validationError) {
            setMessage(imageUploadFormMessage, validationError, "error");
            return;
        }

        setSubmitButtonState(imageUploadForm, true, "Uploading image...");
        setMessage(imageUploadFormMessage, "Uploading center image...");
        setUploadProgress(imageUploadProgress, imageUploadProgressMeta, 0, "Starting upload...");

        const formData = new FormData(imageUploadForm);
        const centerId = Number(formData.get("centerId"));

        try {
            const response = await window.NoorLocatorApi.uploadCenterImage(formData, {
                onProgress: percent => {
                    if (typeof percent === "number") {
                        setUploadProgress(imageUploadProgress, imageUploadProgressMeta, percent, `Uploading image... ${percent}%`);
                        return;
                    }

                    setUploadProgress(imageUploadProgress, imageUploadProgressMeta, null, "Uploading image...");
                }
            });
            syncCenterSelection(centerId);
            await refreshSelectedCenterWorkspace(response.message);
            resetImageUploadForm(centerId);
            setMessage(imageUploadFormMessage, response.message, "success");
            showToast(response.message, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Center image upload could not be completed.");
            resetUploadProgress(imageUploadProgress, imageUploadProgressMeta);
            setMessage(imageUploadFormMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(imageUploadForm, false, "Uploading image...");
        }
    });

    const handleCenterFilterChange = async event => {
        const selectedCenterId = Number(event.target.value);
        syncCenterSelection(Number.isInteger(selectedCenterId) && selectedCenterId > 0 ? selectedCenterId : null);
        await refreshSelectedCenterWorkspace("Manager content refreshed for the selected center.");
    };

    filterCenterSelect.addEventListener("change", handleCenterFilterChange);
    announcementFilterCenterSelect.addEventListener("change", handleCenterFilterChange);
    imageFilterCenterSelect.addEventListener("change", handleCenterFilterChange);

    refreshButton.addEventListener("click", async () => {
        await refreshSelectedCenterWorkspace("Majalis refreshed for the selected center.");
    });

    refreshAnnouncementsButton.addEventListener("click", async () => {
        await refreshSelectedCenterWorkspace("Announcements refreshed for the selected center.");
    });

    refreshCenterImagesButton.addEventListener("click", async () => {
        await refreshSelectedCenterWorkspace("Center gallery refreshed for the selected center.");
    });

    Promise.allSettled([
        window.NoorLocatorAuth.syncCurrentUser(),
        window.NoorLocatorApi.getManagerCenters(),
        window.NoorLocatorApi.getLanguages()
    ]).then(async results => {
        const [userResult, centersResult, languagesResult] = results;

        if (userResult.status === "fulfilled" && userResult.value) {
            state.user = userResult.value;
        }

        if (centersResult.status === "fulfilled") {
            state.centers = centersResult.value.data || [];
        } else {
            throw centersResult.reason;
        }

        if (languagesResult.status === "fulfilled") {
            state.languages = languagesResult.value.data || [];
        } else {
            throw languagesResult.reason;
        }

        renderManagedCenters(centersContainer, state.centers);
        renderMajlisLanguageOptions(languageOptions, state.languages, []);
        populateCenterControls();
        updateCounts();
        refreshOverviewCards();

        if (!state.centers.length) {
            setMessage(pageMessage, "No assigned centers were found for this account.", "error");
            setContainerMessage(majlisListContainer, "No majalis can be managed until a center assignment exists.", "soft");
            setContainerMessage(announcementsContainer, "No announcements can be managed until a center assignment exists.", "soft");
            setContainerMessage(centerImagesContainer, "No center images can be managed until a center assignment exists.", "soft");
            return;
        }

        resetMajlisForm(state.selectedCenterId);
        resetAnnouncementForm(state.selectedCenterId);
        resetImageUploadForm(state.selectedCenterId);
        await refreshSelectedCenterWorkspace();
    }).catch(error => {
        const message = normalizeErrorMessage(error, "The manager workspace could not be loaded right now.");
        setMessage(pageMessage, message, "error");
        setContainerMessage(centersContainer, message, "error");
        setContainerMessage(majlisListContainer, message, "error");
        setContainerMessage(announcementsContainer, message, "error");
        setContainerMessage(centerImagesContainer, message, "error");
        showToast(message, "error");
    });
}

function renderAdminCenterRequests(container, requests) {
    if (!container) {
        return;
    }

    if (!requests.length) {
        setContainerMessage(container, "No center requests are waiting in the moderation queue.", "soft");
        return;
    }

    container.innerHTML = requests.map(request => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(request.name)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${request.city}, ${request.country}`)}</p>
                </div>
                ${renderStatusBadge(request.status)}
            </div>
            <p>${escapeHtml(request.address)}</p>
            <p>${escapeHtml(truncateText(request.description || "No description was submitted for this center request.", 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">By ${escapeHtml(request.requestedByUserName || request.requestedByUserEmail)}</span>
                <span class="card__meta">${escapeHtml(request.requestedByUserEmail)}</span>
                <span class="card__meta">Submitted ${escapeHtml(formatDateTime(request.createdAt))}</span>
            </div>
            <div class="button-row">
                <button class="button button--primary" type="button" data-admin-center-request-approve="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>Approve</button>
                <button class="button button--danger" type="button" data-admin-center-request-reject="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>Reject</button>
            </div>
        </article>
    `).join("");
}

function renderAdminManagerRequests(container, requests) {
    if (!container) {
        return;
    }

    if (!requests.length) {
        setContainerMessage(container, "No manager requests are waiting in the moderation queue.", "soft");
        return;
    }

    container.innerHTML = requests.map(request => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(request.userName || request.userEmail)}</h4>
                    <p class="list-card__meta">${escapeHtml(request.userEmail)}</p>
                </div>
                ${renderStatusBadge(request.status)}
            </div>
            <p>${escapeHtml(`Requested center: ${request.centerName} (${request.centerCity}, ${request.centerCountry})`)}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">Submitted ${escapeHtml(formatDateTime(request.createdAt))}</span>
            </div>
            <div class="button-row">
                <button class="button button--primary" type="button" data-admin-manager-request-approve="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>Approve</button>
                <button class="button button--danger" type="button" data-admin-manager-request-reject="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>Reject</button>
            </div>
        </article>
    `).join("");
}

function renderAdminLanguageSuggestions(container, suggestions) {
    if (!container) {
        return;
    }

    if (!suggestions.length) {
        setContainerMessage(container, "No center language suggestions are waiting for review.", "soft");
        return;
    }

    container.innerHTML = suggestions.map(suggestion => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(suggestion.centerName)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${suggestion.languageName} (${suggestion.languageCode})`)}</p>
                </div>
                ${renderStatusBadge(suggestion.status)}
            </div>
            <p>${escapeHtml(`Suggested by ${suggestion.suggestedByUserName || suggestion.suggestedByUserEmail}`)}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(suggestion.suggestedByUserEmail)}</span>
            </div>
            <div class="button-row">
                <button class="button button--primary" type="button" data-admin-language-suggestion-approve="${escapeHtml(suggestion.id)}"${String(suggestion.status) !== "Pending" ? " disabled" : ""}>Approve</button>
                <button class="button button--danger" type="button" data-admin-language-suggestion-reject="${escapeHtml(suggestion.id)}"${String(suggestion.status) !== "Pending" ? " disabled" : ""}>Reject</button>
            </div>
        </article>
    `).join("");
}

function renderAdminSuggestions(container, suggestions) {
    if (!container) {
        return;
    }

    if (!suggestions.length) {
        setContainerMessage(container, "No app suggestions are waiting for review.", "soft");
        return;
    }

    container.innerHTML = suggestions.map(suggestion => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(suggestion.userName || suggestion.userEmail)}</h4>
                    <p class="list-card__meta">${escapeHtml(suggestion.userEmail)}</p>
                </div>
                ${renderStatusBadge(suggestion.status)}
            </div>
            <p>${escapeHtml(truncateText(suggestion.message, 220))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(String(suggestion.type))}</span>
                <span class="card__meta">Submitted ${escapeHtml(formatDateTime(suggestion.createdAt))}</span>
            </div>
            <div class="button-row">
                <button class="button button--secondary" type="button" data-admin-suggestion-review="${escapeHtml(suggestion.id)}"${String(suggestion.status) !== "Pending" ? " disabled" : ""}>Mark reviewed</button>
            </div>
        </article>
    `).join("");
}

function renderAdminCenters(container, centers) {
    if (!container) {
        return;
    }

    if (!centers.length) {
        setContainerMessage(container, "No published centers are available to manage.", "soft");
        return;
    }

    container.innerHTML = centers.map(center => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(center.name)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${center.city}, ${center.country}`)}</p>
                </div>
                <span class="status-pill status-pill--muted">${escapeHtml(`${center.managerCount} manager${center.managerCount === 1 ? "" : "s"}`)}</span>
            </div>
            <p>${escapeHtml(center.address)}</p>
            <p>${escapeHtml(truncateText(center.description || "No public description is available for this center.", 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(`${center.majlisCount} majlis${center.majlisCount === 1 ? "" : "es"}`)}</span>
                <span class="card__meta">${escapeHtml(`${center.languageCount} language${center.languageCount === 1 ? "" : "s"}`)}</span>
            </div>
            <div class="button-row">
                <button class="button button--secondary" type="button" data-admin-center-edit="${escapeHtml(center.id)}">Edit</button>
                <button class="button button--danger" type="button" data-admin-center-delete="${escapeHtml(center.id)}" data-admin-center-name="${escapeHtml(center.name)}">Delete</button>
            </div>
        </article>
    `).join("");
}

function renderAdminUsersTable(container, users) {
    if (!container) {
        return;
    }

    if (!users.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">No users are available.</div>`;
        return;
    }

    container.innerHTML = `
        <table class="data-table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Role</th>
                    <th>Assigned Centers</th>
                    <th>Created</th>
                </tr>
            </thead>
            <tbody>
                ${users.map(user => `
                    <tr>
                        <td>${escapeHtml(user.name)}</td>
                        <td>${escapeHtml(user.email)}</td>
                        <td>${escapeHtml(String(user.role))}</td>
                        <td>${escapeHtml(String(user.assignedCenterCount))}</td>
                        <td>${escapeHtml(formatDateTime(user.createdAt))}</td>
                    </tr>
                `).join("")}
            </tbody>
        </table>
    `;
}

function renderAdminAuditLogsTable(container, auditLogs) {
    if (!container) {
        return;
    }

    if (!auditLogs.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">No audit log entries are available.</div>`;
        return;
    }

    container.innerHTML = `
        <table class="data-table">
            <thead>
                <tr>
                    <th>When</th>
                    <th>Action</th>
                    <th>Entity</th>
                    <th>User</th>
                    <th>IP</th>
                    <th>Metadata</th>
                </tr>
            </thead>
            <tbody>
                ${auditLogs.map(log => `
                    <tr>
                        <td>${escapeHtml(formatDateTime(log.createdAt))}</td>
                        <td>${escapeHtml(log.action)}</td>
                        <td>${escapeHtml(log.entityName)}${log.entityId ? ` <span class="data-table__mono">#${escapeHtml(log.entityId)}</span>` : ""}</td>
                        <td>${escapeHtml(log.userName || log.userEmail || "System")}</td>
                        <td class="data-table__mono">${escapeHtml(log.ipAddress || "-")}</td>
                        <td class="data-table__mono">${escapeHtml(truncateText(log.metadata || "-", 140))}</td>
                    </tr>
                `).join("")}
            </tbody>
        </table>
    `;
}

function initAdminPage() {
    if (!window.NoorLocatorAuth.requireAuth(["Admin"])) {
        return;
    }

    const pageMessage = document.getElementById("admin-page-message");
    const pendingCount = document.getElementById("admin-pending-count");
    const auditCount = document.getElementById("admin-audit-count");
    const cardsContainer = document.getElementById("admin-cards");
    const centerRequestsContainer = document.getElementById("admin-center-requests");
    const managerRequestsContainer = document.getElementById("admin-manager-requests");
    const languageSuggestionsContainer = document.getElementById("admin-language-suggestions");
    const suggestionsContainer = document.getElementById("admin-suggestions");
    const centersContainer = document.getElementById("admin-centers");
    const adminCenterImagesMessage = document.getElementById("admin-center-images-message");
    const adminCenterImagesContainer = document.getElementById("admin-center-images");
    const adminCenterImageFilterCenter = document.getElementById("admin-center-image-filter-center");
    const refreshAdminCenterImagesButton = document.getElementById("refresh-admin-center-images-button");
    const usersTableContainer = document.getElementById("admin-users-table");
    const auditLogsTableContainer = document.getElementById("admin-audit-logs-table");
    const centerForm = document.getElementById("admin-center-form");
    const centerFormHeading = document.getElementById("admin-center-form-heading");
    const centerFormMessage = document.querySelector('[data-form-message="admin-center-form"]');
    const centerSubmitButton = document.getElementById("admin-center-submit-button");
    const centerCancelButton = document.getElementById("admin-center-cancel-button");
    const state = {
        user: window.NoorLocatorAuth.getSessionUser(),
        dashboard: null,
        centerRequests: [],
        managerRequests: [],
        languageSuggestions: [],
        suggestions: [],
        users: [],
        centers: [],
        centerImages: [],
        auditLogs: [],
        editingCenterId: null,
        selectedImageCenterId: null
    };

    if (!pageMessage || !pendingCount || !auditCount || !cardsContainer || !centerRequestsContainer || !managerRequestsContainer || !languageSuggestionsContainer || !suggestionsContainer || !centersContainer || !adminCenterImagesMessage || !adminCenterImagesContainer || !adminCenterImageFilterCenter || !refreshAdminCenterImagesButton || !usersTableContainer || !auditLogsTableContainer || !centerForm || !centerFormHeading || !centerFormMessage || !centerSubmitButton || !centerCancelButton) {
        return;
    }

    function updateCounters() {
        const pendingTotal = (state.dashboard?.pendingCenterRequests || 0)
            + (state.dashboard?.pendingManagerRequests || 0)
            + (state.dashboard?.pendingCenterLanguageSuggestions || 0)
            + (state.dashboard?.pendingSuggestions || 0);

        pendingCount.textContent = String(pendingTotal);
        auditCount.textContent = String(state.dashboard?.totalAuditLogs || 0);
    }

    function refreshOverviewCards() {
        const currentUser = state.user || window.NoorLocatorAuth.getSessionUser() || { name: "Admin" };
        populateCards("admin-cards", [
            {
                title: "Moderation queue",
                body: `${currentUser.name} currently has ${(state.dashboard?.pendingCenterRequests || 0) + (state.dashboard?.pendingManagerRequests || 0) + (state.dashboard?.pendingCenterLanguageSuggestions || 0) + (state.dashboard?.pendingSuggestions || 0)} pending items to review.`
            },
            {
                title: "Platform scale",
                body: `${state.dashboard?.totalUsers || 0} users, ${state.dashboard?.totalCenters || 0} centers, and ${state.dashboard?.totalMajalis || 0} majalis are currently stored in NoorLocator.`
            },
            {
                title: "Center moderation",
                body: `${state.dashboard?.pendingCenterRequests || 0} center requests, ${state.dashboard?.pendingManagerRequests || 0} manager requests, and ${state.dashboard?.pendingCenterLanguageSuggestions || 0} language suggestions are awaiting action.`
            },
            {
                title: "Audit coverage",
                body: `${state.dashboard?.totalAuditLogs || 0} audit log entries are available for admin traceability and change review.`
            }
        ]);
    }

    window.addEventListener("noorlocator:auth-changed", event => {
        if (!event.detail) {
            return;
        }

        state.user = event.detail;
        refreshOverviewCards();
    });

    function syncAdminImageCenterSelection(centerId) {
        const validCenterId = state.centers.some(center => center.id === centerId)
            ? centerId
            : (state.centers[0]?.id || null);

        state.selectedImageCenterId = validCenterId;
        adminCenterImageFilterCenter.value = validCenterId ? String(validCenterId) : "";
    }

    function populateAdminImageControls() {
        populateSelectOptions(
            [adminCenterImageFilterCenter],
            state.centers,
            {
                placeholder: state.centers.length ? "Select a center" : "No centers available",
                getValue: center => String(center.id),
                getLabel: center => `${center.name} (${center.city}, ${center.country})`
            });

        if (!state.centers.length) {
            state.selectedImageCenterId = null;
            return;
        }

        syncAdminImageCenterSelection(state.selectedImageCenterId || state.editingCenterId || state.centers[0].id);
    }

    async function loadAdminCenterImages() {
        populateAdminImageControls();

        if (!state.selectedImageCenterId) {
            state.centerImages = [];
            setMessage(adminCenterImagesMessage, "Choose a center to review its gallery and moderate images if needed.");
            setContainerMessage(adminCenterImagesContainer, "No center images can be reviewed until a center is selected.", "soft");
            return;
        }

        setMessage(adminCenterImagesMessage, "Loading the selected center gallery...");
        setContainerMessage(adminCenterImagesContainer, "Loading center gallery...", "soft");

        try {
            const response = await window.NoorLocatorApi.getCenterImages(state.selectedImageCenterId);
            state.centerImages = response.data || [];
            renderAdminCenterImages(adminCenterImagesContainer, state.centerImages);
            bindAdminImageActions();
            setMessage(adminCenterImagesMessage, "Admin gallery moderation tools are ready.", "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Center images could not be loaded for moderation.");
            state.centerImages = [];
            setMessage(adminCenterImagesMessage, message, "error");
            setContainerMessage(adminCenterImagesContainer, message, "error");
        }
    }

    function bindAdminImageActions() {
        bindImageGalleryActions(adminCenterImagesContainer, {
            onSetPrimary: async imageId => {
                try {
                    const response = await window.NoorLocatorApi.setPrimaryCenterImage(imageId);
                    await loadAdminCenterImages();
                    setMessage(adminCenterImagesMessage, response.message || "Primary image updated successfully.", "success");
                    setMessage(pageMessage, response.message || "Primary image updated successfully.", "success");
                    showToast(response.message || "Primary image updated successfully.", "success");
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Primary image could not be updated.");
                    setMessage(adminCenterImagesMessage, message, "error");
                    showToast(message, "error");
                }
            },
            onDelete: async imageId => {
                if (!window.confirm("Delete this center image from the public gallery?")) {
                    return;
                }

                try {
                    const response = await window.NoorLocatorApi.deleteCenterImage(imageId);
                    await loadAdminCenterImages();
                    setMessage(adminCenterImagesMessage, response.message || "Center image deleted successfully.", "success");
                    setMessage(pageMessage, response.message || "Center image deleted successfully.", "success");
                    showToast(response.message || "Center image deleted successfully.", "success");
                } catch (error) {
                    const message = normalizeErrorMessage(error, "Center image could not be deleted.");
                    setMessage(adminCenterImagesMessage, message, "error");
                    showToast(message, "error");
                }
            }
        });
    }

    function resetCenterForm() {
        state.editingCenterId = null;
        centerForm.reset();
        centerForm.elements.namedItem("centerId").value = "";
        centerFormHeading.textContent = "Edit a published center";
        centerSubmitButton.textContent = "Save center";
        centerSubmitButton.dataset.defaultLabel = "Save center";
        centerSubmitButton.disabled = true;
        centerCancelButton.hidden = true;
        setMessage(centerFormMessage, "Choose a center from the list to edit.");
    }

    function populateCenterForm(center) {
        state.editingCenterId = center.id;
        centerForm.elements.namedItem("centerId").value = String(center.id);
        centerForm.elements.namedItem("name").value = center.name || "";
        centerForm.elements.namedItem("address").value = center.address || "";
        centerForm.elements.namedItem("city").value = center.city || "";
        centerForm.elements.namedItem("country").value = center.country || "";
        centerForm.elements.namedItem("latitude").value = String(center.latitude);
        centerForm.elements.namedItem("longitude").value = String(center.longitude);
        centerForm.elements.namedItem("description").value = center.description || "";
        centerFormHeading.textContent = `Editing ${center.name}`;
        centerSubmitButton.textContent = "Save changes";
        centerSubmitButton.dataset.defaultLabel = "Save changes";
        centerSubmitButton.disabled = false;
        centerCancelButton.hidden = false;
        setMessage(centerFormMessage, `Editing "${center.name}".`, "success");
        syncAdminImageCenterSelection(center.id);
        loadAdminCenterImages().catch(() => {
            setMessage(adminCenterImagesMessage, "Center images could not be loaded for moderation.", "error");
        });
        document.getElementById("center-management")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function renderAllSections() {
        renderAdminCenterRequests(centerRequestsContainer, state.centerRequests);
        renderAdminManagerRequests(managerRequestsContainer, state.managerRequests);
        renderAdminLanguageSuggestions(languageSuggestionsContainer, state.languageSuggestions);
        renderAdminSuggestions(suggestionsContainer, state.suggestions);
        renderAdminCenters(centersContainer, state.centers);
        populateAdminImageControls();
        renderAdminUsersTable(usersTableContainer, state.users);
        renderAdminAuditLogsTable(auditLogsTableContainer, state.auditLogs);
        updateCounters();
        refreshOverviewCards();

        if (state.editingCenterId) {
            const editedCenter = state.centers.find(center => center.id === state.editingCenterId);
            if (editedCenter) {
                populateCenterForm(editedCenter);
                return;
            }
        }

        resetCenterForm();
    }

    async function loadAdminWorkspace(showLoading = false) {
        if (showLoading) {
            setCardLoadingState(cardsContainer, 4);
            setContainerMessage(centerRequestsContainer, "Loading center requests...", "soft");
            setContainerMessage(managerRequestsContainer, "Loading manager requests...", "soft");
            setContainerMessage(languageSuggestionsContainer, "Loading language suggestions...", "soft");
            setContainerMessage(suggestionsContainer, "Loading app suggestions...", "soft");
            setContainerMessage(centersContainer, "Loading centers...", "soft");
            setMessage(adminCenterImagesMessage, "Loading center gallery moderation tools...");
            setContainerMessage(adminCenterImagesContainer, "Loading center gallery...", "soft");
            usersTableContainer.innerHTML = `<div class="empty-state empty-state--soft">Loading users...</div>`;
            auditLogsTableContainer.innerHTML = `<div class="empty-state empty-state--soft">Loading audit logs...</div>`;
        }

        const [
            user,
            dashboardResponse,
            centerRequestsResponse,
            managerRequestsResponse,
            languageSuggestionsResponse,
            suggestionsResponse,
            usersResponse,
            centersResponse,
            auditLogsResponse
        ] = await Promise.all([
            window.NoorLocatorAuth.syncCurrentUser(),
            window.NoorLocatorApi.getAdminDashboard(),
            window.NoorLocatorApi.getAdminCenterRequests(),
            window.NoorLocatorApi.getAdminManagerRequests(),
            window.NoorLocatorApi.getAdminCenterLanguageSuggestions(),
            window.NoorLocatorApi.getAdminSuggestions(),
            window.NoorLocatorApi.getAdminUsers(),
            window.NoorLocatorApi.getAdminCenters(),
            window.NoorLocatorApi.getAdminAuditLogs()
        ]);

        state.user = user || state.user;
        state.dashboard = dashboardResponse.data;
        state.centerRequests = centerRequestsResponse.data || [];
        state.managerRequests = managerRequestsResponse.data || [];
        state.languageSuggestions = languageSuggestionsResponse.data || [];
        state.suggestions = suggestionsResponse.data || [];
        state.users = usersResponse.data || [];
        state.centers = centersResponse.data || [];
        state.auditLogs = auditLogsResponse.data || [];

        renderAllSections();
        await loadAdminCenterImages();
        setMessage(pageMessage, "Admin workspace loaded from the secured API.", "success");
    }

    async function runAdminAction(confirmMessage, action) {
        if (confirmMessage && !window.confirm(confirmMessage)) {
            return;
        }

        try {
            const response = await action();
            await loadAdminWorkspace();
            setMessage(pageMessage, response.message, "success");
            showToast(response.message, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "The admin action could not be completed.");
            setMessage(pageMessage, message, "error");
            showToast(message, "error");
        }
    }

    function bindModerationActions() {
        centerRequestsContainer.querySelectorAll("[data-admin-center-request-approve]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-center-request-approve"));
                await runAdminAction(
                    "Approve this center request and publish it as a live center?",
                    () => window.NoorLocatorApi.approveAdminCenterRequest(id));
            });
        });

        centerRequestsContainer.querySelectorAll("[data-admin-center-request-reject]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-center-request-reject"));
                await runAdminAction(
                    "Reject this center request?",
                    () => window.NoorLocatorApi.rejectAdminCenterRequest(id));
            });
        });

        managerRequestsContainer.querySelectorAll("[data-admin-manager-request-approve]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-manager-request-approve"));
                await runAdminAction(
                    "Approve this manager request and grant center access?",
                    () => window.NoorLocatorApi.approveAdminManagerRequest(id));
            });
        });

        managerRequestsContainer.querySelectorAll("[data-admin-manager-request-reject]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-manager-request-reject"));
                await runAdminAction(
                    "Reject this manager request?",
                    () => window.NoorLocatorApi.rejectAdminManagerRequest(id));
            });
        });

        languageSuggestionsContainer.querySelectorAll("[data-admin-language-suggestion-approve]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-language-suggestion-approve"));
                await runAdminAction(
                    "Approve this center language suggestion?",
                    () => window.NoorLocatorApi.approveAdminCenterLanguageSuggestion(id));
            });
        });

        languageSuggestionsContainer.querySelectorAll("[data-admin-language-suggestion-reject]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-language-suggestion-reject"));
                await runAdminAction(
                    "Reject this center language suggestion?",
                    () => window.NoorLocatorApi.rejectAdminCenterLanguageSuggestion(id));
            });
        });

        suggestionsContainer.querySelectorAll("[data-admin-suggestion-review]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-suggestion-review"));
                await runAdminAction(
                    "Mark this suggestion as reviewed?",
                    () => window.NoorLocatorApi.reviewAdminSuggestion(id));
            });
        });

        centersContainer.querySelectorAll("[data-admin-center-edit]").forEach(button => {
            button.addEventListener("click", () => {
                const id = Number(button.getAttribute("data-admin-center-edit"));
                const center = state.centers.find(currentCenter => currentCenter.id === id);
                if (center) {
                    populateCenterForm(center);
                }
            });
        });

        centersContainer.querySelectorAll("[data-admin-center-delete]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-center-delete"));
                const centerName = button.getAttribute("data-admin-center-name") || "this center";
                await runAdminAction(
                    `Delete "${centerName}" and its related center data?`,
                    async () => {
                        if (state.editingCenterId === id) {
                            resetCenterForm();
                        }

                        return await window.NoorLocatorApi.deleteAdminCenter(id);
                    });
            });
        });
    }

    const originalRenderAllSections = renderAllSections;
    renderAllSections = () => {
        originalRenderAllSections();
        bindModerationActions();
    };

    centerForm.addEventListener("submit", async event => {
        event.preventDefault();

        if (!state.editingCenterId) {
            setMessage(centerFormMessage, "Choose a center from the list before saving changes.", "error");
            return;
        }

        setSubmitButtonState(centerForm, true, "Saving changes...");
        setMessage(centerFormMessage, "Saving center changes...");

        const values = getTrimmedFormValues(centerForm);
        const payload = {
            name: values.name,
            address: values.address,
            city: values.city,
            country: values.country,
            latitude: Number(values.latitude),
            longitude: Number(values.longitude),
            description: values.description
        };

        try {
            const response = await window.NoorLocatorApi.updateAdminCenter(state.editingCenterId, payload);
            await loadAdminWorkspace();
            const updatedCenter = state.centers.find(center => center.id === state.editingCenterId);
            if (updatedCenter) {
                populateCenterForm(updatedCenter);
            }
            setMessage(centerFormMessage, response.message, "success");
            setMessage(pageMessage, response.message, "success");
            showToast(response.message, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Center changes could not be saved.");
            setMessage(centerFormMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(centerForm, false, "Saving changes...");
        }
    });

    centerCancelButton.addEventListener("click", () => {
        resetCenterForm();
    });

    adminCenterImageFilterCenter.addEventListener("change", async event => {
        const selectedCenterId = Number(event.target.value);
        syncAdminImageCenterSelection(Number.isInteger(selectedCenterId) && selectedCenterId > 0 ? selectedCenterId : null);
        await loadAdminCenterImages();
    });

    refreshAdminCenterImagesButton.addEventListener("click", async () => {
        await loadAdminCenterImages();
    });

    loadAdminWorkspace(true).catch(error => {
        const message = normalizeErrorMessage(error, "The admin workspace could not be loaded right now.");
        setMessage(pageMessage, message, "error");
        setContainerMessage(centerRequestsContainer, message, "error");
        setContainerMessage(managerRequestsContainer, message, "error");
        setContainerMessage(languageSuggestionsContainer, message, "error");
        setContainerMessage(suggestionsContainer, message, "error");
        setContainerMessage(centersContainer, message, "error");
        setMessage(adminCenterImagesMessage, message, "error");
        setContainerMessage(adminCenterImagesContainer, message, "error");
        usersTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
        auditLogsTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
        showToast(message, "error");
    });
}

function populateCards(containerId, cards) {
    const container = document.getElementById(containerId);
    if (!container) {
        return;
    }

    container.innerHTML = cards.map(card => `
        <article class="card">
            <h3>${escapeHtml(card.title)}</h3>
            <p>${escapeHtml(card.body)}</p>
        </article>
    `).join("");
}
