const DISCOVERY_LOCATION_KEY = "noorlocator.discovery.location";

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
    toast.textContent = message;
    toastRoot.appendChild(toast);

    window.setTimeout(() => {
        toast.classList.add("toast--leaving");

        window.setTimeout(() => {
            toast.remove();
        }, 220);
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
        user: window.NoorLocatorAuth.getUser(),
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
        const currentUser = state.user || window.NoorLocatorAuth.getUser() || { name: "Contributor", role: "User" };
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
            <h3>${escapeHtml(card.title)}</h3>
            <p>${escapeHtml(card.body)}</p>
        </article>
    `).join("");
}
