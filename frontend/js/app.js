const DISCOVERY_LOCATION_KEY = "noorlocator.discovery.location";

document.addEventListener("DOMContentLoaded", () => {
    notifyIfLoggedOut();

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

function notifyIfLoggedOut() {
    const url = new URL(window.location.href);
    if (url.searchParams.get("loggedOut") !== "1") {
        return;
    }

    url.searchParams.delete("loggedOut");

    if (window.history?.replaceState) {
        const query = url.searchParams.toString();
        window.history.replaceState({}, document.title, `${url.pathname}${query ? `?${query}` : ""}${url.hash}`);
    }

    showToast("You have been signed out successfully.", "success");
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
    const modifier = ["approved", "rejected", "reviewed"].includes(normalized) ? normalized : "pending";
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

async function initHomePage() {
    const featuredCenters = document.getElementById("featured-centers");
    const homeStatus = document.getElementById("home-status");
    const centerCount = document.getElementById("home-center-count");
    const location = getDiscoveryLocation();

    setCardLoadingState(featuredCenters, 3);
    setMessage(homeStatus, "Connecting to the live NoorLocator directory...");

    try {
        const response = location
            ? await window.NoorLocatorApi.getNearestCenters(location)
            : await window.NoorLocatorApi.getCenters();
        const centers = response.data || [];

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
        setMessage(homeStatus, error.message || "Unable to load the public center preview.", "error");
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
    const infoGrid = document.getElementById("center-info-grid");
    const detailMessage = document.getElementById("center-detail-message");
    const mapLink = document.getElementById("center-map-link");
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
        if (infoGrid) {
            infoGrid.innerHTML = "";
        }
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

    if (infoGrid) {
        infoGrid.innerHTML = Array.from({ length: 3 }, () => `
            <div class="info-card info-card--loading">
                <span class="skeleton skeleton--line skeleton--sm"></span>
                <span class="skeleton skeleton--line"></span>
            </div>
        `).join("");
    }

    try {
        const [centerResult, languagesResult, majalisResult] = await Promise.allSettled([
            window.NoorLocatorApi.getCenter(id, appendLocationParams({}, location)),
            window.NoorLocatorApi.getCenterLanguages(id),
            window.NoorLocatorApi.getCenterMajalis(id)
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

        if (languagesResult.status !== "fulfilled" || majalisResult.status !== "fulfilled") {
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

function initManagerPage() {
    if (!window.NoorLocatorAuth.requireAuth(["Manager", "Admin"])) {
        return;
    }

    const pageMessage = document.getElementById("manager-page-message");
    const centerCount = document.getElementById("manager-center-count");
    const majlisCount = document.getElementById("manager-majlis-count");
    const cardsContainer = document.getElementById("manager-cards");
    const centersContainer = document.getElementById("manager-centers");
    const majlisListContainer = document.getElementById("manager-majalis");
    const form = document.getElementById("majlis-form");
    const formMessage = document.querySelector('[data-form-message="majlis-form"]');
    const formHeading = document.getElementById("majlis-form-heading");
    const submitButton = document.getElementById("majlis-submit-button");
    const cancelButton = document.getElementById("majlis-cancel-button");
    const refreshButton = document.getElementById("refresh-majalis-button");
    const formCenterSelect = document.getElementById("majlis-center-select");
    const filterCenterSelect = document.getElementById("majlis-filter-center");
    const languageOptions = document.getElementById("majlis-language-options");
    const state = {
        user: window.NoorLocatorAuth.getSessionUser(),
        centers: [],
        languages: [],
        majalis: [],
        selectedCenterId: null,
        editingMajlisId: null
    };

    if (!pageMessage || !centerCount || !majlisCount || !cardsContainer || !centersContainer || !majlisListContainer || !form || !formMessage || !formHeading || !submitButton || !cancelButton || !refreshButton || !formCenterSelect || !filterCenterSelect || !languageOptions) {
        return;
    }

    setCardLoadingState(cardsContainer, 3);
    setContainerMessage(centersContainer, "Loading your assigned centers...", "soft");
    setContainerMessage(majlisListContainer, "Loading majalis...", "soft");
    setMessage(pageMessage, "Loading your manager workspace...");

    function updateCounts() {
        centerCount.textContent = String(state.centers.length);
        majlisCount.textContent = String(state.majalis.length);
    }

    function refreshOverviewCards() {
        const currentUser = state.user || window.NoorLocatorAuth.getSessionUser() || { name: "Manager", role: "Manager" };
        populateCards("manager-cards", [
            {
                title: "Manager session",
                body: `${currentUser.name} is signed in as ${currentUser.role}. Majlis changes are accepted only for approved center assignments.`
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
            }
        ]);
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
        setMessage(formMessage, "Create a majlis for one of your assigned centers.");
    }

    function populateCenterControls() {
        populateSelectOptions([formCenterSelect, filterCenterSelect], state.centers, {
            placeholder: state.centers.length ? "Select a center" : "No centers available",
            getValue: center => String(center.id),
            getLabel: center => `${center.name} (${center.city}, ${center.country})`
        });

        if (!state.centers.length) {
            return;
        }

        const fallbackCenterId = state.selectedCenterId || state.centers[0].id;
        state.selectedCenterId = fallbackCenterId;

        filterCenterSelect.value = String(fallbackCenterId);
        if (!formCenterSelect.value) {
            formCenterSelect.value = String(fallbackCenterId);
        }
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
                    formCenterSelect.value = String(majlis.centerId);
                    formHeading.textContent = "Edit majlis";
                    submitButton.textContent = "Save changes";
                    submitButton.dataset.defaultLabel = "Save changes";
                    cancelButton.hidden = false;
                    renderMajlisLanguageOptions(languageOptions, state.languages, (majlis.languages || []).map(language => language.id));
                    setMessage(formMessage, `Editing "${majlis.title}".`, "success");
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

    async function loadMajalisForSelectedCenter() {
        const centerId = Number(filterCenterSelect.value);
        state.selectedCenterId = Number.isInteger(centerId) && centerId > 0 ? centerId : null;

        if (!state.selectedCenterId) {
            state.majalis = [];
            updateCounts();
            setContainerMessage(majlisListContainer, "Select one of your assigned centers to manage majalis.", "soft");
            refreshOverviewCards();
            return;
        }

        setContainerMessage(majlisListContainer, "Loading majalis for the selected center...", "soft");

        try {
            const response = await window.NoorLocatorApi.getMajalis(state.selectedCenterId);
            state.majalis = (response.data || []).filter(majlis =>
                state.centers.some(center => center.id === majlis.centerId));

            renderManagerMajalis(majlisListContainer, state.majalis);
            bindMajlisListActions();
            updateCounts();
            refreshOverviewCards();
            setMessage(pageMessage, "Manager workspace loaded from the live API.", "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Majalis could not be loaded for the selected center.");
            state.majalis = [];
            updateCounts();
            refreshOverviewCards();
            setContainerMessage(majlisListContainer, message, "error");
            setMessage(pageMessage, message, "error");
        }
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        setSubmitButtonState(form, true, state.editingMajlisId ? "Saving changes..." : "Creating majlis...");
        setMessage(formMessage, state.editingMajlisId ? "Saving majlis changes..." : "Creating majlis...");

        const values = getTrimmedFormValues(form);
        const payload = {
            title: values.title,
            description: values.description,
            date: values.date ? `${values.date}T00:00:00` : "",
            time: values.time,
            centerId: Number(values.centerId),
            languageIds: getSelectedLanguageIds(languageOptions)
        };

        try {
            const response = state.editingMajlisId
                ? await window.NoorLocatorApi.updateMajlis(state.editingMajlisId, payload)
                : await window.NoorLocatorApi.createMajlis(payload);

            filterCenterSelect.value = String(payload.centerId);
            state.selectedCenterId = payload.centerId;

            await loadMajalisForSelectedCenter();
            resetMajlisForm(payload.centerId);
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

    cancelButton.addEventListener("click", () => {
        resetMajlisForm(state.selectedCenterId);
    });

    filterCenterSelect.addEventListener("change", async () => {
        const selectedCenterId = Number(filterCenterSelect.value);
        state.selectedCenterId = Number.isInteger(selectedCenterId) && selectedCenterId > 0
            ? selectedCenterId
            : null;

        if (!state.editingMajlisId && state.selectedCenterId) {
            formCenterSelect.value = String(state.selectedCenterId);
        }

        await loadMajalisForSelectedCenter();
    });

    refreshButton.addEventListener("click", async () => {
        await loadMajalisForSelectedCenter();
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
            return;
        }

        resetMajlisForm(state.selectedCenterId);
        await loadMajalisForSelectedCenter();
    }).catch(error => {
        const message = normalizeErrorMessage(error, "The manager workspace could not be loaded right now.");
        setMessage(pageMessage, message, "error");
        setContainerMessage(centersContainer, message, "error");
        setContainerMessage(majlisListContainer, message, "error");
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
        auditLogs: [],
        editingCenterId: null
    };

    if (!pageMessage || !pendingCount || !auditCount || !cardsContainer || !centerRequestsContainer || !managerRequestsContainer || !languageSuggestionsContainer || !suggestionsContainer || !centersContainer || !usersTableContainer || !auditLogsTableContainer || !centerForm || !centerFormHeading || !centerFormMessage || !centerSubmitButton || !centerCancelButton) {
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
        document.getElementById("center-management")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function renderAllSections() {
        renderAdminCenterRequests(centerRequestsContainer, state.centerRequests);
        renderAdminManagerRequests(managerRequestsContainer, state.managerRequests);
        renderAdminLanguageSuggestions(languageSuggestionsContainer, state.languageSuggestions);
        renderAdminSuggestions(suggestionsContainer, state.suggestions);
        renderAdminCenters(centersContainer, state.centers);
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

    loadAdminWorkspace(true).catch(error => {
        const message = normalizeErrorMessage(error, "The admin workspace could not be loaded right now.");
        setMessage(pageMessage, message, "error");
        setContainerMessage(centerRequestsContainer, message, "error");
        setContainerMessage(managerRequestsContainer, message, "error");
        setContainerMessage(languageSuggestionsContainer, message, "error");
        setContainerMessage(suggestionsContainer, message, "error");
        setContainerMessage(centersContainer, message, "error");
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
