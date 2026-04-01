const DISCOVERY_LOCATION_KEY = "noorlocator.discovery.location";
const CENTER_IMAGE_MAX_SIZE_BYTES = 5 * 1024 * 1024;
const CENTER_IMAGE_ALLOWED_EXTENSIONS = new Set([".jpg", ".jpeg", ".png", ".webp"]);

document.addEventListener("DOMContentLoaded", async () => {
    await window.NoorLocatorI18n.init();
    const authReady = await window.NoorLocatorAuth.bootstrapPageAuth();
    if (!authReady && document.body?.dataset.authRequired === "true") {
        return;
    }

    window.NoorLocatorLayout.init();
    window.NoorLocatorI18n.applyTranslations(document);
    window.NoorLocatorAuth.bindLogoutControls(document);
    notifyAuthStatus();
    syncNotificationBell();

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
        case "verify-email":
            initVerifyEmailPage();
            break;
        case "forgot-password":
            initForgotPasswordPage();
            break;
        case "reset-password":
            initResetPasswordPage();
            break;
        case "profile":
            initProfilePage();
            break;
        case "notifications":
            initNotificationsPage();
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

function t(key, fallback, params = {}) {
    return window.NoorLocatorI18n?.t?.(key, params, fallback) || fallback;
}

function translateUiText(message, fallbackMessage = "") {
    return window.NoorLocatorI18n?.translateMessage?.(message, fallbackMessage) || fallbackMessage || message;
}

function notifyAuthStatus() {
    const url = new URL(window.location.href);
    let message = "";
    let type = "success";

    if (url.searchParams.get("loggedOut") === "1") {
        url.searchParams.delete("loggedOut");
        message = "You have been signed out successfully.";
    } else if (url.searchParams.get("verified") === "1") {
        url.searchParams.delete("verified");
        message = "Your email has been verified. You can now sign in.";
    } else if (url.searchParams.get("passwordReset") === "1") {
        url.searchParams.delete("passwordReset");
        message = "Your password has been reset. Please sign in with your new password.";
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

    element.textContent = translateUiText(message);
    element.className = `message${type ? ` message--${type}` : ""}`;
}

function setContainerMessage(container, message, modifier = "") {
    if (!container) {
        return;
    }

    const className = modifier ? `empty-state empty-state--${modifier}` : "empty-state";
    container.innerHTML = `<div class="${className}">${escapeHtml(translateUiText(message))}</div>`;
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

    const formattedDistance = new Intl.NumberFormat(window.NoorLocatorI18n?.getLocaleCode?.() || undefined, {
        maximumFractionDigits: 1,
        minimumFractionDigits: 1
    }).format(distanceKm);

    return t("common.distanceKmAway", "{distance} km away", { distance: formattedDistance });
}

function formatDateTime(dateValue) {
    if (!dateValue) {
        return t("common.dateToBeAnnounced", "Date to be announced");
    }

    const date = new Date(dateValue);
    if (Number.isNaN(date.getTime())) {
        return t("common.dateToBeAnnounced", "Date to be announced");
    }

    return new Intl.DateTimeFormat(window.NoorLocatorI18n?.getLocaleCode?.() || undefined, {
        dateStyle: "medium",
        timeStyle: "short"
    }).format(date);
}

function formatDate(dateValue) {
    if (!dateValue) {
        return t("common.dateToBeAnnounced", "Date to be announced");
    }

    const date = new Date(dateValue);
    if (Number.isNaN(date.getTime())) {
        return t("common.dateToBeAnnounced", "Date to be announced");
    }

    return new Intl.DateTimeFormat(window.NoorLocatorI18n?.getLocaleCode?.() || undefined, {
        dateStyle: "medium"
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
        return sanitizeUiMessage(error.errors[0], fallbackMessage);
    }

    if (typeof error?.message === "string" && error.message.trim()) {
        return sanitizeUiMessage(error.message, fallbackMessage);
    }

    return fallbackMessage;
}

function sanitizeUiMessage(message, fallbackMessage = "Something went wrong. Please try again.") {
    const text = String(message || "").trim();
    if (!text) {
        return translateUiText(fallbackMessage);
    }

    const technicalPatterns = [
        /\/api\//i,
        /\bapi\b/i,
        /\bbackend\b/i,
        /\bendpoint\b/i,
        /\bresponse\b/i,
        /\bpayload\b/i,
        /\bserver-side\b/i,
        /\bstatus\s*\d{3}\b/i,
        /\bget\s+\//i,
        /\bpost\s+\//i
    ];

    return technicalPatterns.some(pattern => pattern.test(text))
        ? translateUiText(fallbackMessage)
        : translateUiText(text);
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

    const translatedMessage = translateUiText(message);
    const toastRoot = ensureToastRoot();
    const toast = document.createElement("div");
    toast.className = `toast toast--${type}`;
    toast.innerHTML = `
        <div class="toast__content">
            <strong class="toast__title">${type === "error"
                ? escapeHtml(t("toast.actionNeeded", "Action needed"))
                : "NoorLocator"}</strong>
            <span>${escapeHtml(translatedMessage)}</span>
        </div>
        <button class="toast__close" type="button" aria-label="${escapeHtml(t("toast.dismiss", "Dismiss notification"))}">&times;</button>
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

function translateStatusValue(status) {
    const value = String(status || "Pending");
    const normalized = value.trim().toLowerCase();

    return normalized === "approved"
        ? t("status.approved", "Approved")
        : normalized === "rejected"
            ? t("status.rejected", "Rejected")
            : normalized === "reviewed"
                ? t("status.reviewed", "Reviewed")
                : normalized === "draft"
                    ? t("status.draft", "Draft")
                    : normalized === "published"
                        ? t("status.published", "Published")
                        : normalized === "archived"
                            ? t("status.archived", "Archived")
                            : normalized === "read"
                                ? t("status.read", "Read")
                                : normalized === "new"
                                    ? t("status.new", "New")
                                    : normalized === "verified"
                                        ? t("status.verified", "Verified")
                                        : normalized === "unverified"
                                            ? t("status.unverified", "Unverified")
                                            : t("status.pending", "Pending");
}

function renderStatusBadge(status) {
    const value = String(status || "Pending");
    const normalized = value.toLowerCase();
    const modifier = ["approved", "rejected", "reviewed", "draft", "published", "archived"].includes(normalized)
        ? normalized
        : "pending";
    return `<span class="status-badge status-badge--${modifier}">${escapeHtml(translateStatusValue(value))}</span>`;
}

function getLanguageDisplayLabel(code, options = {}) {
    const localeCode = window.NoorLocatorI18n?.getLocaleCode?.();
    const native = options.native !== false;

    return window.NoorLocatorI18n.getLanguageOptionLabel?.(code, {
        native,
        includeFlag: false,
        locale: localeCode
    }) || window.NoorLocatorI18n.getLanguageLabel(code, { locale: localeCode });
}

function getLanguageOptionText(code, options = {}) {
    const localeCode = window.NoorLocatorI18n?.getLocaleCode?.();
    const native = options.native !== false;

    return window.NoorLocatorI18n.getLanguageOptionLabel?.(code, {
        native,
        includeFlag: true,
        locale: localeCode
    }) || getLanguageDisplayLabel(code, options);
}

function renderLanguageBadgeMarkup(code, options = {}) {
    const metadata = window.NoorLocatorI18n.getLanguageMetadata?.(code);
    const label = options.label || getLanguageDisplayLabel(code, options);
    const modifier = options.modifier ? ` chip--${options.modifier}` : "";
    const extraClass = options.className ? ` ${options.className}` : "";

    return `
        <span class="chip chip--language${modifier}${extraClass}">
            ${metadata?.flag ? `<span class="chip__flag" aria-hidden="true">${escapeHtml(metadata.flag)}</span>` : ""}
            <span class="chip__label">${escapeHtml(label)}</span>
        </span>
    `;
}

function renderLanguageBadgeList(languages, options = {}) {
    return (languages || [])
        .map(language => renderLanguageBadgeMarkup(language.code || language, options))
        .join("");
}

function populateSelectOptions(selectElements, items, options) {
    const selects = Array.isArray(selectElements)
        ? selectElements.filter(Boolean)
        : selectElements instanceof Element
            ? [selectElements]
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
    submitButton.textContent = isBusy
        ? translateUiText(busyLabel)
        : translateUiText(submitButton.dataset.defaultLabel);
}

function updateNotificationBellCount(count) {
    const normalizedCount = Number(count || 0);

    if (window.NoorLocatorLayout?.setNotificationCount) {
        window.NoorLocatorLayout.setNotificationCount(normalizedCount);
        return;
    }

    document.querySelectorAll("[data-notification-count]").forEach(badge => {
        badge.textContent = normalizedCount > 99 ? "99+" : String(normalizedCount);
        badge.hidden = normalizedCount <= 0;
    });
}

async function syncNotificationBell() {
    if (!window.NoorLocatorAuth.isAuthenticated() || !window.NoorLocatorAuth.isEmailVerified()) {
        updateNotificationBellCount(0);
        return;
    }

    try {
        const response = await window.NoorLocatorApi.getUnreadNotificationCount();
        updateNotificationBellCount(response.data?.count || 0);
    } catch {
        updateNotificationBellCount(0);
    }
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
            <p class="card__excerpt">${escapeHtml(truncateText(center.description || t("centers.card.defaultDescription", "Public center details are available on the profile page."), 150))}</p>
            <p>${escapeHtml(center.address)}</p>
            ${(center.languages || []).length
                ? `<div class="chip-list">${renderLanguageBadgeList(center.languages, { modifier: "muted" })}</div>`
                : ""}
            <div class="button-row">
                <a class="button button--secondary" href="${buildCenterDetailsHref(center.id)}">${escapeHtml(t("actions.viewDetails", "View details"))}</a>
                <a class="button button--ghost" href="${buildMapLink(center)}" target="_blank" rel="noreferrer noopener">${escapeHtml(t("actions.openMap", "Open map"))}</a>
            </div>
        </article>
    `).join("");
}

function renderLanguageChips(container, languages) {
    if (!container) {
        return;
    }

    if (!languages.length) {
        container.innerHTML = `<span class="empty-state empty-state--soft">${escapeHtml(t("centers.languages.none", "No supported languages are published for this center yet."))}</span>`;
        return;
    }

    container.innerHTML = languages
        .map(language => renderLanguageBadgeMarkup(language.code))
        .join("");
}

function renderMajalis(container, majalis) {
    if (!container) {
        return;
    }

    if (!majalis.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("center.majalis.none", "No upcoming majalis are currently published for this center."))}</div>`;
        return;
    }

    container.innerHTML = majalis.map(majlis => `
        <article class="list-card">
            ${majlis.imageUrl ? `<img class="majlis-card__image" src="${escapeHtml(majlis.imageUrl)}" alt="${escapeHtml(`${majlis.title} image`)}" loading="lazy">` : ""}
            <div class="list-card__head">
                <h4>${escapeHtml(majlis.title)}</h4>
                <span class="status-pill">${escapeHtml(formatDateTime(majlis.date))}</span>
            </div>
            <p>${escapeHtml(majlis.description || t("center.majalis.defaultDescription", "Majlis details will appear here when available."))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(majlis.time || t("common.timeToBeConfirmed", "Time to be confirmed"))}</span>
                ${renderLanguageBadgeList(majlis.languages, { modifier: "muted" })}
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
                ? t("center.gallery.primaryShown", "The primary center image is shown above. No additional gallery images are available yet.")
                : t("center.gallery.none", "No public center photos have been uploaded yet."));
        setContainerMessage(container, emptyMessage, "soft");
        return;
    }

    container.innerHTML = galleryImages.map(image => `
        <article class="gallery-card">
            <img class="gallery-card__image" src="${escapeHtml(image.imageUrl)}" alt="${escapeHtml(options.imageAlt || "Center gallery image")}" loading="lazy">
            <div class="gallery-card__head">
                <div>
                    ${image.isPrimary ? `<span class="status-badge status-badge--published">${escapeHtml(t("center.gallery.primary", "Primary"))}</span>` : `<span class="card__meta">${escapeHtml(t("center.gallery.image", "Gallery image"))}</span>`}
                    <p class="gallery-card__meta">${escapeHtml(t("common.uploadedOn", "Uploaded {date}", { date: formatDateTime(image.createdAt) }))}</p>
                </div>
            </div>
            ${manageable ? `
                <div class="gallery-card__actions">
                    ${image.isPrimary ? "" : `<button class="button button--secondary" type="button" data-set-primary-image-id="${escapeHtml(image.id)}">${escapeHtml(t("actions.setPrimary", "Set primary"))}</button>`}
                    <button class="button button--danger" type="button" data-delete-center-image-id="${escapeHtml(image.id)}">${escapeHtml(t("actions.delete", "Delete"))}</button>
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
        const emptyMessage = options.emptyMessage || t("center.announcements.none", "No public announcements are available for this center right now.");
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
            <p>${escapeHtml(announcement.description || t("center.announcements.defaultDescription", "No additional announcement details have been provided."))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("common.publishedOn", "Published {date}", { date: formatDateTime(announcement.createdAt) }))}</span>
                ${manageable ? `<span class="card__meta">${escapeHtml(t("common.statusLabel", "Status: {status}", { status: translateStatusValue(String(announcement.status)) }))}</span>` : ""}
            </div>
            ${manageable ? `
                <div class="button-row">
                    <button class="button button--secondary" type="button" data-edit-announcement-id="${escapeHtml(announcement.id)}">${escapeHtml(t("actions.edit", "Edit"))}</button>
                    <button class="button button--danger" type="button" data-delete-announcement-id="${escapeHtml(announcement.id)}" data-announcement-title="${escapeHtml(announcement.title)}">${escapeHtml(t("actions.delete", "Delete"))}</button>
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
            t("dashboard.requests.none", "You have not submitted a center request yet. Use the form to send a new center into the moderation queue."),
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
            <p>${escapeHtml(truncateText(request.description || t("dashboard.requests.defaultDescription", "No description was submitted for this request."), 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("common.submittedOn", "Submitted {date}", { date: formatDateTime(request.createdAt) }))}</span>
                <span class="card__meta">${escapeHtml(`${Number(request.latitude).toFixed(4)}, ${Number(request.longitude).toFixed(4)}`)}</span>
            </div>
        </article>
    `).join("");
}

function requestBrowserLocation() {
    if (!("geolocation" in navigator)) {
        return Promise.reject(new Error(t("centers.location.unsupported", "Geolocation is not supported in this browser. Use the city and country filters instead.")));
    }

    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            position => {
                const location = parseLocation(position.coords.latitude, position.coords.longitude);
                if (!location) {
                    reject(new Error(t("centers.location.invalid", "Your browser returned an invalid location.")));
                    return;
                }

                resolve(location);
            },
            error => {
                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        reject(new Error(t("centers.location.denied", "Location access was denied. Use city and country search instead.")));
                        break;
                    case error.TIMEOUT:
                        reject(new Error(t("centers.location.timeout", "Location lookup timed out. Please try again or use city and country search.")));
                        break;
                    default:
                        reject(new Error(t("centers.location.unavailable", "Unable to determine your location right now.")));
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
    const submitCenterLink = document.getElementById("home-submit-center-link");
    const location = getDiscoveryLocation();

    if (submitCenterLink) {
        submitCenterLink.href = window.NoorLocatorAuth.isAuthenticated()
            ? "dashboard.html#center-request"
            : "register.html";
    }

    setCardLoadingState(featuredCenters, 3);
    setMessage(homeStatus, "Connecting to the live NoorLocator directory...");

    try {
        const centersResponse = await (
            location
                ? window.NoorLocatorApi.getNearestCenters(location)
                : window.NoorLocatorApi.getCenters());
        const centers = centersResponse.data || [];

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
                : "Showing a preview of published centers in NoorLocator.",
            "success");
    } catch (error) {
        if (centerCount) {
            centerCount.textContent = "0";
        }

        setContainerMessage(featuredCenters, "The public center preview could not be loaded right now.", "error");
        setMessage(homeStatus, error.message || "Unable to load the public center preview.", "error");
    }
}

async function initAboutPage() {
    const pageMessage = document.getElementById("about-page-message");
    const submitCenterLink = document.getElementById("about-submit-center-link");

    if (submitCenterLink) {
        submitCenterLink.href = window.NoorLocatorAuth.isAuthenticated()
            ? "dashboard.html#center-request"
            : "register.html";
    }

    setMessage(pageMessage, "About NoorLocator is ready.", "success");
}

async function initCentersPage() {
    const centersContainer = document.getElementById("centers-list");
    const nearbyContainer = document.getElementById("nearby-centers");
    const searchForm = document.getElementById("center-search-form");
    const languageSelect = document.getElementById("center-language-filter");
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

    async function loadLanguageFilter() {
        if (!languageSelect) {
            return;
        }

        try {
            const response = await window.NoorLocatorApi.getLanguages();
            const languages = (response.data || [])
                .filter(language => window.NoorLocatorI18n.getSupportedLanguages().some(item => item.code === language.code))
                .sort((left, right) => getLanguageDisplayLabel(left.code).localeCompare(getLanguageDisplayLabel(right.code), window.NoorLocatorI18n.getLocaleCode()));

            populateSelectOptions(languageSelect, languages, {
                placeholder: t("centers.filters.language.placeholder", "Any language"),
                getValue: language => language.code,
                getLabel: language => getLanguageOptionText(language.code)
            });
        } catch {
            populateSelectOptions(languageSelect, [], {
                placeholder: t("centers.filters.language.placeholder", "Any language"),
                getValue: language => language.code,
                getLabel: language => language.code
            });
        }
    }

    async function loadDirectory(filters = null) {
        setCardLoadingState(centersContainer, 4);
        const response = filters && hasSearchFilters(filters)
            ? await window.NoorLocatorApi.searchCenters(appendLocationParams(filters, state.location))
            : await window.NoorLocatorApi.getCenters(appendLocationParams({}, state.location));
        const centers = response.data || [];

        renderCenterCards(
            centersContainer,
            centers,
            filters && hasSearchFilters(filters)
                ? "No centers matched your search. Try another area or keyword."
                : "No centers are available right now.");

        if (resultsSummary) {
            resultsSummary.textContent = filters && hasSearchFilters(filters)
                ? t("centers.results.found", "{count} centers found for your search.", { count: centers.length })
                : t("centers.results.available", "{count} centers available right now.", { count: centers.length });
        }
    }

    async function loadNearbyCenters() {
        if (!state.location) {
            setContainerMessage(nearbyContainer, "Turn on location to see nearby centers, or search below.", "soft");
            return;
        }

        setCardLoadingState(nearbyContainer, 3);

        try {
            const response = await window.NoorLocatorApi.getNearestCenters(state.location);
            const centers = response.data || [];
            renderCenterCards(nearbyContainer, centers, "No centers found near your location. Try searching in another area.", { limit: 3 });
        } catch (error) {
            setContainerMessage(nearbyContainer, normalizeErrorMessage(error, "Nearby centers are unavailable right now."), "error");
        }
    }

    function updateLocationPanel(message, detail, type = "") {
        if (locationStatus) {
            locationStatus.textContent = translateUiText(message);
            locationStatus.className = type ? `text-emphasis text-emphasis--${type}` : "text-emphasis";
        }

        if (currentLocation) {
            currentLocation.textContent = translateUiText(detail);
        }
    }

    async function refreshLocation() {
        updateLocationPanel("Locating", "Finding nearby centers based on your location.");
        setMessage(pageMessage, "Finding nearby centers...");

        try {
            const location = await requestBrowserLocation();
            state.location = location;
            setDiscoveryLocation(location);

            updateLocationPanel(
                "Enabled",
                "Using your location to show nearby centers and estimated distances.",
                "success");
            setMessage(pageMessage, "Location enabled. Nearby centers are now available.", "success");

            const filters = getTrimmedFormValues(searchForm);

            try {
                await Promise.all([
                    loadDirectory(filters),
                    loadNearbyCenters()
                ]);
            } catch (loadError) {
                const message = normalizeErrorMessage(loadError, "The center list could not be refreshed right now.");
                setContainerMessage(centersContainer, message, "error");
                setMessage(pageMessage, message, "error");
            }
        } catch (error) {
            const message = normalizeErrorMessage(error, "Location access is unavailable. Use city and country search instead.");
            updateLocationPanel("Unavailable", message, "error");
            setMessage(pageMessage, message, "error");
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

        setMessage(searchMessage, "Searching centers...");

        try {
            await loadDirectory(filters);
            setMessage(
                searchMessage,
                hasSearchFilters(filters)
                    ? "Search complete."
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
    await loadLanguageFilter();

    if (state.location) {
        updateLocationPanel(
            "Enabled",
            "Using your saved location to show nearby centers.",
            "success");
    } else {
        updateLocationPanel("Checking", "No saved location was found yet. NoorLocator can use your browser location next.");
    }

    try {
        await loadDirectory();
    } catch (error) {
        const message = normalizeErrorMessage(error, "The center directory could not be loaded right now.");
        setContainerMessage(centersContainer, message, "error");
        setMessage(pageMessage, message, "error");
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
    const subscribeButton = document.getElementById("center-subscribe-button");
    const subscribeMessage = document.getElementById("center-subscribe-message");
    const heroImage = document.getElementById("center-hero-image");
    const heroFallback = document.getElementById("center-logo-fallback");
    const params = new URLSearchParams(window.location.search);
    const id = Number(params.get("id"));
    const location = getDiscoveryLocation();

    if (!Number.isInteger(id) || id <= 0) {
        if (title) {
            title.textContent = t("center.notFound.title", "Center not found");
        }

        if (description) {
            description.textContent = t("center.notFound.description", "The requested center identifier is invalid.");
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
        title.textContent = t("center.title.loading", "Loading center details...");
    }

    if (meta) {
        meta.innerHTML = `<span class="status-pill">${escapeHtml(t("center.meta.loading", "Loading profile data"))}</span>`;
    }

    if (description) {
        description.textContent = t("center.description.loadingShort", "Loading this center's details.");
    }

    if (languages) {
        languages.innerHTML = `<span class="empty-state">${escapeHtml(t("center.languages.loading", "Loading supported languages..."))}</span>`;
    }

    if (majalis) {
        majalis.innerHTML = `<div class="empty-state">${escapeHtml(t("center.majalis.loading", "Loading upcoming majalis..."))}</div>`;
    }

    if (gallery) {
        gallery.innerHTML = `<div class="empty-state">${escapeHtml(t("center.gallery.loading", "Loading center gallery..."))}</div>`;
    }

    if (announcements) {
        announcements.innerHTML = `<div class="empty-state">${escapeHtml(t("center.announcements.loading", "Loading center announcements..."))}</div>`;
    }

    if (infoGrid) {
        infoGrid.innerHTML = Array.from({ length: 3 }, () => `
            <div class="info-card info-card--loading">
                <span class="skeleton skeleton--line skeleton--sm"></span>
                <span class="skeleton skeleton--line"></span>
            </div>
        `).join("");
    }

    async function configureCenterSubscription(centerId) {
        if (!subscribeButton || !subscribeMessage) {
            return;
        }

        if (!window.NoorLocatorAuth.isAuthenticated()) {
            subscribeButton.textContent = t("center.subscribe.signIn", "Sign in to follow updates");
            subscribeButton.disabled = false;
            subscribeButton.onclick = () => {
                window.location.href = `login.html?redirect=${encodeURIComponent(window.location.pathname + window.location.search)}`;
            };
            setMessage(subscribeMessage, "Sign in to receive updates from this center.");
            return;
        }

        if (!window.NoorLocatorAuth.isEmailVerified()) {
            subscribeButton.textContent = t("center.subscribe.verify", "Verify email to follow");
            subscribeButton.disabled = false;
            subscribeButton.onclick = () => {
                window.location.href = window.NoorLocatorAuth.getVerificationRoute();
            };
            setMessage(subscribeMessage, "Verify your email to follow center updates.", "error");
            return;
        }

        let isSubscribed = false;

        try {
            const response = await window.NoorLocatorApi.getMySubscriptions();
            isSubscribed = (response.data || []).some(subscription => Number(subscription.centerId) === centerId);
        } catch {
            isSubscribed = false;
        }

        const renderState = () => {
            subscribeButton.textContent = isSubscribed
                ? t("center.subscribe.following", "Following this center")
                : t("actions.followCenter", "Follow center updates");
            subscribeButton.className = isSubscribed ? "button button--secondary" : "button button--primary";
            setMessage(
                subscribeMessage,
                isSubscribed
                    ? "You will receive majlis and event updates from this center."
                    : "Follow this center to receive majlis and event updates.");
        };

        renderState();

        subscribeButton.onclick = async () => {
            subscribeButton.disabled = true;
            setMessage(subscribeMessage, isSubscribed ? "Stopping updates..." : "Following this center...");

            try {
                const response = isSubscribed
                    ? await window.NoorLocatorApi.unsubscribeFromCenter(centerId)
                    : await window.NoorLocatorApi.subscribeToCenter(centerId);
                isSubscribed = !isSubscribed;
                renderState();
                setMessage(subscribeMessage, response.message, "success");
            } catch (error) {
                renderState();
                setMessage(subscribeMessage, normalizeErrorMessage(error, "NoorLocator could not update this center right now."), "error");
            } finally {
                subscribeButton.disabled = false;
            }
        };
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

        document.title = `${center.name} | ${t("app.name", "NoorLocator")}`;
        title.textContent = center.name;
        meta.innerHTML = `
            <span class="status-pill">${escapeHtml(`${center.city}, ${center.country}`)}</span>
            <span class="status-pill status-pill--muted">${escapeHtml(center.address)}</span>
            ${typeof center.distanceKm === "number" ? `<span class="status-pill status-pill--success">${escapeHtml(formatDistance(center.distanceKm))}</span>` : ""}
        `;
        description.textContent = center.description || t("center.description.none", "This center has not published a public description yet.");
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
                    <span class="card__meta">${escapeHtml(t("center.info.address", "Address"))}</span>
                    <strong>${escapeHtml(center.address)}</strong>
                    <p>${escapeHtml(`${center.city}, ${center.country}`)}</p>
                </article>
                <article class="info-card">
                    <span class="card__meta">${escapeHtml(t("center.info.distance", "Distance"))}</span>
                    <strong>${escapeHtml(typeof center.distanceKm === "number" ? formatDistance(center.distanceKm) : t("common.unavailable", "Unavailable"))}</strong>
                    <p>${escapeHtml(typeof center.distanceKm === "number"
                        ? t("center.info.distanceAvailable", "Estimated from your current location.")
                        : t("center.info.distanceUnavailable", "Enable location to see an estimated distance."))}</p>
                </article>
                <article class="info-card">
                    <span class="card__meta">${escapeHtml(t("center.info.coordinates", "Coordinates"))}</span>
                    <strong>${escapeHtml(center.latitude.toFixed(4))}, ${escapeHtml(center.longitude.toFixed(4))}</strong>
                    <p>${escapeHtml(t("center.info.coordinatesHelp", "Use the map button to open turn-by-turn directions."))}</p>
                </article>
            `;
        }

        if (mapLink) {
            mapLink.href = buildMapLink(center);
        }

        if (window.NoorLocatorAuth.isAuthenticated() && window.NoorLocatorAuth.isEmailVerified()) {
            window.NoorLocatorApi.trackCenterVisit(center.id, { source: "page_view" }).catch(() => null);
        }

        await configureCenterSubscription(center.id);

        if (
            languagesResult.status !== "fulfilled" ||
            majalisResult.status !== "fulfilled" ||
            imagesResult.status !== "fulfilled" ||
            announcementsResult.status !== "fulfilled"
        ) {
            setMessage(detailMessage, "Some details are temporarily unavailable, but the main center profile is ready.", "error");
            return;
        }

        setMessage(detailMessage, "Center details are ready.", "success");
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
            infoGrid.innerHTML = `<div class="empty-state empty-state--error">Center details are unavailable right now.</div>`;
        }

        setMessage(detailMessage, error.message || "Center details could not be loaded.", "error");
        if (subscribeMessage) {
            setMessage(subscribeMessage, "Center updates are unavailable right now.", "error");
        }
    }
}

function initLoginPage() {
    if (window.NoorLocatorAuth.isAuthenticated()) {
        window.location.href = window.NoorLocatorAuth.getDefaultRoute();
        return;
    }

    const resendContainer = document.getElementById("login-resend-verification");
    const resendButton = document.getElementById("login-resend-verification-button");
    const resendMessage = document.getElementById("login-resend-verification-message");

    async function resendVerification(email) {
        if (!email) {
            setMessage(resendMessage, "Enter your email address so NoorLocator can resend the verification link.", "error");
            return;
        }

        resendButton.disabled = true;
        setMessage(resendMessage, "Sending a new verification email...");

        try {
            const response = await window.NoorLocatorApi.resendVerificationEmail({ email });
            setMessage(resendMessage, response.message || "A new verification email is on its way.", "success");
        } catch (error) {
            setMessage(resendMessage, normalizeErrorMessage(error, "NoorLocator could not resend the verification email right now."), "error");
        } finally {
            resendButton.disabled = false;
        }
    }

    resendButton?.addEventListener("click", () => {
        const email = document.getElementById("login-email")?.value.trim() || "";
        resendVerification(email);
    });

    bindAuthForm("login-form", async formData => window.NoorLocatorApi.login(formData), {
        onSuccess(response, formData, form, message) {
            resendContainer.hidden = true;
            setMessage(message, response.message || "Signed in successfully.", "success");
            window.setTimeout(() => {
                window.location.href = window.NoorLocatorAuth.getDefaultRoute();
            }, 500);
        },
        onError(error, formData, form, message) {
            const normalizedMessage = normalizeErrorMessage(error, "Sign-in could not be completed right now.");
            setMessage(message, normalizedMessage, "error");

            if (Number(error?.status) === 403) {
                resendContainer.hidden = false;
                setMessage(resendMessage, "Verify your email to unlock full access, then sign in again.");
                return;
            }

            resendContainer.hidden = true;
        }
    });
}

function initRegisterPage() {
    if (window.NoorLocatorAuth.isAuthenticated()) {
        window.location.href = window.NoorLocatorAuth.getDefaultRoute();
        return;
    }

    bindAuthForm("register-form", async formData => window.NoorLocatorApi.register(formData), {
        onSuccess(response, formData, form, message) {
            const email = formData.email || "";
            const successMessage = response.message || "Please check your email to verify your account.";
            setMessage(message, successMessage, "success");
            showToast(successMessage, "success");
            window.setTimeout(() => {
                window.location.href = `verify-email.html?sent=1${email ? `&email=${encodeURIComponent(email)}` : ""}`;
            }, 700);
        },
        onError(error, formData, form, message) {
            setMessage(message, normalizeErrorMessage(error, "Registration could not be completed right now."), "error");
        }
    });
}

function initVerifyEmailPage() {
    const statusTitle = document.getElementById("verify-email-status-title");
    const statusText = document.getElementById("verify-email-status-text");
    const pageMessage = document.getElementById("verify-email-page-message");
    const resendForm = document.getElementById("verify-email-resend-form");
    const resendMessage = document.querySelector('[data-form-message="verify-email-resend-form"]');
    const resendEmailInput = document.getElementById("verify-email-resend-email");
    const url = new URL(window.location.href);
    const token = url.searchParams.get("token") || "";
    const email = url.searchParams.get("email") || "";
    const sent = url.searchParams.get("sent") === "1";

    if (resendEmailInput && email) {
        resendEmailInput.value = email;
    }

    if (sent) {
        setMessage(pageMessage, "Please check your email to verify your account.", "success");
    }

    if (token) {
        setMessage(pageMessage, "Verifying your email...");

        window.NoorLocatorApi.verifyEmail(token)
            .then(response => {
                if (statusTitle) {
                    statusTitle.textContent = "Email verified";
                }

                if (statusText) {
                    statusText.textContent = "Your NoorLocator account is now verified and ready to use.";
                }

                if (resendEmailInput && response.data?.email) {
                    resendEmailInput.value = response.data.email;
                }

                setMessage(pageMessage, response.message || "Your email has been verified.", "success");
                showToast(response.message || "Your email has been verified.", "success");

                window.setTimeout(() => {
                    window.location.href = "login.html?verified=1";
                }, 900);
            })
            .catch(error => {
                const status = error?.data?.status || "";

                if (statusTitle) {
                    statusTitle.textContent = status === "expired" ? "Verification link expired" : "Verification link invalid";
                }

                if (statusText) {
                    statusText.textContent = status === "expired"
                        ? "Request a new verification email to continue."
                        : "Use the resend form below to request a fresh verification email.";
                }

                if (resendEmailInput && error?.data?.email) {
                    resendEmailInput.value = error.data.email;
                }

                setMessage(pageMessage, normalizeErrorMessage(error, "This verification link could not be used."), "error");
            });
    }

    resendForm?.addEventListener("submit", async event => {
        event.preventDefault();
        const values = getTrimmedFormValues(resendForm);
        setSubmitButtonState(resendForm, true, "Sending...");
        setMessage(resendMessage, "Sending a fresh verification email...");

        try {
            const response = await window.NoorLocatorApi.resendVerificationEmail({ email: values.email });
            setMessage(resendMessage, response.message || "A new verification email has been sent.", "success");
        } catch (error) {
            setMessage(resendMessage, normalizeErrorMessage(error, "NoorLocator could not resend the verification email right now."), "error");
        } finally {
            setSubmitButtonState(resendForm, false, "Sending...");
        }
    });
}

function initForgotPasswordPage() {
    const form = document.getElementById("forgot-password-form");
    const message = document.querySelector('[data-form-message="forgot-password-form"]');

    if (!form) {
        return;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const values = getTrimmedFormValues(form);
        setSubmitButtonState(form, true, "Sending link...");
        setMessage(message, "Sending reset instructions...");

        try {
            const response = await window.NoorLocatorApi.forgotPassword(values);
            setMessage(message, response.message || "If an account exists for this email, a reset link has been sent.", "success");
        } catch (error) {
            setMessage(message, normalizeErrorMessage(error, "NoorLocator could not start the reset flow right now."), "error");
        } finally {
            setSubmitButtonState(form, false, "Sending link...");
        }
    });
}

function initResetPasswordPage() {
    const form = document.getElementById("reset-password-form");
    const message = document.querySelector('[data-form-message="reset-password-form"]');
    const tokenInput = document.getElementById("reset-password-token");
    const params = new URLSearchParams(window.location.search);
    const token = params.get("token") || "";

    if (!form || !tokenInput) {
        return;
    }

    tokenInput.value = token;

    if (!token) {
        setMessage(message, "This reset link is incomplete. Request a new one to continue.", "error");
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();

        if (!tokenInput.value.trim()) {
            setMessage(message, "This reset link is missing its token. Request a new one to continue.", "error");
            return;
        }

        const values = getTrimmedFormValues(form);
        setSubmitButtonState(form, true, "Updating password...");
        setMessage(message, "Updating your password...");

        try {
            const response = await window.NoorLocatorApi.resetPassword(values);
            setMessage(message, response.message || "Your password has been reset successfully.", "success");
            showToast(response.message || "Your password has been reset successfully.", "success");
            window.setTimeout(() => {
                window.location.href = "login.html?passwordReset=1";
            }, 900);
        } catch (error) {
            setMessage(message, normalizeErrorMessage(error, "This reset link could not be used."), "error");
        } finally {
            setSubmitButtonState(form, false, "Updating password...");
        }
    });
}

function initNotificationsPage() {
    if (!window.NoorLocatorAuth.requireAuth()) {
        return;
    }

    const list = document.getElementById("notifications-list");
    const pageMessage = document.getElementById("notifications-page-message");
    const markAllButton = document.getElementById("notifications-mark-all");

    if (!list || !pageMessage || !markAllButton) {
        return;
    }

    async function loadNotifications(successMessage = "Your notifications are ready.") {
        setContainerMessage(list, "Loading notifications...", "soft");

        try {
            const response = await window.NoorLocatorApi.getNotifications();
            const notifications = response.data || [];

            if (!notifications.length) {
                setContainerMessage(list, "You do not have any notifications yet.", "soft");
            } else {
                list.innerHTML = notifications.map(notification => `
                    <article class="list-card${notification.isRead ? "" : " list-card--unread"}" data-notification-id="${notification.id}">
                        <div class="list-card__head">
                            <div>
                                <h4>${escapeHtml(notification.title)}</h4>
                                <p class="list-card__meta">${escapeHtml(formatDateTime(notification.createdAt))}</p>
                            </div>
                            <span class="status-pill${notification.isRead ? " status-pill--muted" : " status-pill--success"}">${escapeHtml(notification.isRead ? t("status.read", "Read") : t("status.new", "New"))}</span>
                        </div>
                        <p>${escapeHtml(notification.message)}</p>
                        <div class="button-row">
                            ${notification.linkUrl ? `<a class="button button--ghost" href="${escapeHtml(notification.linkUrl)}">${escapeHtml(t("actions.open", "Open"))}</a>` : ""}
                            ${notification.isRead ? "" : `<button class="button button--secondary" type="button" data-notification-read="${notification.id}">${escapeHtml(t("actions.markAsRead", "Mark as read"))}</button>`}
                        </div>
                    </article>
                `).join("");
            }

            setMessage(pageMessage, successMessage, "success");
            syncNotificationBell();
        } catch (error) {
            setContainerMessage(list, normalizeErrorMessage(error, "Notifications are unavailable right now."), "error");
            setMessage(pageMessage, normalizeErrorMessage(error, "Notifications are unavailable right now."), "error");
        }
    }

    markAllButton.addEventListener("click", async () => {
        markAllButton.disabled = true;
        setMessage(pageMessage, "Marking notifications as read...");

        try {
            const response = await window.NoorLocatorApi.markAllNotificationsRead();
            await loadNotifications(response.message || "All notifications marked as read.");
        } catch (error) {
            setMessage(pageMessage, normalizeErrorMessage(error, "NoorLocator could not update notifications right now."), "error");
        } finally {
            markAllButton.disabled = false;
        }
    });

    list.addEventListener("click", async event => {
        const target = event.target;
        if (!(target instanceof HTMLElement) || !target.matches("[data-notification-read]")) {
            return;
        }

        const notificationId = Number(target.dataset.notificationRead);
        if (!notificationId) {
            return;
        }

        target.disabled = true;

        try {
            await window.NoorLocatorApi.markNotificationRead(notificationId);
            await loadNotifications("Notification marked as read.");
        } catch (error) {
            setMessage(pageMessage, normalizeErrorMessage(error, "NoorLocator could not update this notification right now."), "error");
            target.disabled = false;
        }
    });

    loadNotifications();
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
    const displayName = document.getElementById("profile-display-name");
    const roleBadge = document.getElementById("profile-role-badge");
    const createdAt = document.getElementById("profile-created-at");
    const lastLoginAt = document.getElementById("profile-last-login");
    const roleDisplay = document.getElementById("profile-role-display");
    const centerCount = document.getElementById("profile-center-count");
    const workspaceLink = document.getElementById("profile-workspace-link");
    const verificationStatus = document.getElementById("profile-verification-status");
    const verificationNote = document.getElementById("profile-verification-note");
    const resendVerificationButton = document.getElementById("profile-resend-verification");
    const preferencesForm = document.getElementById("notification-preferences-form");
    const preferencesMessage = document.querySelector('[data-form-message="notification-preferences-form"]');
    const preferredLanguageForm = document.getElementById("preferred-language-form");
    const preferredLanguageSelect = document.getElementById("preferred-language-select");
    const preferredLanguageMessage = document.querySelector('[data-form-message="preferred-language-form"]');
    const state = {
        profile: window.NoorLocatorAuth.getSessionUser(),
        preferences: null
    };

    if (!pageMessage || !cardsContainer || !form || !formMessage || !nameInput || !emailInput || !displayName || !roleBadge || !createdAt || !roleDisplay || !centerCount || !workspaceLink) {
        return;
    }

    setCardLoadingState(cardsContainer, 3);
    setMessage(pageMessage, "Loading your profile...");

    function getWorkspaceTarget(profile) {
        if (!profile?.isEmailVerified) {
            return {
                href: window.NoorLocatorAuth.getVerificationRoute(profile),
                label: t("nav.verifyEmail", "Verify your email")
            };
        }

        if (profile?.role === "Admin") {
            return {
                href: "admin.html",
                label: t("profile.actions.backToAdmin", "Back to admin workspace")
            };
        }

        if (profile?.role === "Manager") {
            return {
                href: "manager.html",
                label: t("profile.actions.backToManager", "Back to manager workspace")
            };
        }

        return {
            href: "dashboard.html",
            label: t("profile.actions.backToDashboard", "Back to dashboard")
        };
    }

    function renderProfile(profile) {
        if (!profile) {
            return;
        }

        nameInput.value = profile.name || "";
        emailInput.value = profile.email || "";
        displayName.textContent = window.NoorLocatorAuth.formatUserDisplayName(profile);
        roleBadge.textContent = window.NoorLocatorAuth.getLocalizedRoleLabel(profile.role);
        roleDisplay.textContent = window.NoorLocatorAuth.getLocalizedRoleLabel(profile.role);
        createdAt.textContent = profile.createdAt ? formatDateTime(profile.createdAt) : t("common.unknown", "Unknown");

        const assignedCenterCount = (profile.assignedCenterIds || []).length;
        centerCount.textContent = t("profile.assignedCenters.count", "{count} centers", { count: assignedCenterCount });
        if (lastLoginAt) {
            lastLoginAt.textContent = profile.lastLoginAtUtc ? formatDateTime(profile.lastLoginAtUtc) : t("profile.lastLogin.none", "No completed sign-in yet");
        }
        if (verificationStatus) {
            verificationStatus.textContent = profile.isEmailVerified ? t("status.verified", "Verified") : t("status.unverified", "Unverified");
            verificationStatus.className = profile.isEmailVerified ? "status-pill status-pill--success" : "status-pill";
        }
        if (verificationNote) {
            verificationNote.textContent = profile.isEmailVerified
                ? t("profile.emailStatus.verifiedNote", "Your email address is verified and can receive account and center updates.")
                : t("profile.emailStatus.unverifiedNote", "Verify this email address to unlock full NoorLocator access and email notifications.");
        }
        if (resendVerificationButton) {
            resendVerificationButton.hidden = Boolean(profile.isEmailVerified);
        }

        const workspaceTarget = getWorkspaceTarget(profile);
        workspaceLink.href = workspaceTarget.href;
        workspaceLink.textContent = workspaceTarget.label;

        if (preferredLanguageSelect instanceof HTMLSelectElement) {
            preferredLanguageSelect.value = profile.preferredLanguageCode || window.NoorLocatorI18n.getLocaleCode();
        }
    }

    function renderPreferences(preferences) {
        if (!preferencesForm || !preferences) {
            return;
        }

        const emailNotifications = preferencesForm.elements.namedItem("emailNotificationsEnabled");
        const appNotifications = preferencesForm.elements.namedItem("appNotificationsEnabled");
        const majlisNotifications = preferencesForm.elements.namedItem("majlisNotificationsEnabled");
        const eventNotifications = preferencesForm.elements.namedItem("eventNotificationsEnabled");
        const centerUpdates = preferencesForm.elements.namedItem("centerUpdatesEnabled");

        if (emailNotifications instanceof HTMLInputElement) {
            emailNotifications.checked = Boolean(preferences.emailNotificationsEnabled);
        }

        if (appNotifications instanceof HTMLInputElement) {
            appNotifications.checked = Boolean(preferences.appNotificationsEnabled);
        }

        if (majlisNotifications instanceof HTMLInputElement) {
            majlisNotifications.checked = Boolean(preferences.majlisNotificationsEnabled);
        }

        if (eventNotifications instanceof HTMLInputElement) {
            eventNotifications.checked = Boolean(preferences.eventNotificationsEnabled);
        }

        if (centerUpdates instanceof HTMLInputElement) {
            centerUpdates.checked = Boolean(preferences.centerUpdatesEnabled);
        }
    }

    function refreshOverviewCards() {
        const profile = state.profile || window.NoorLocatorAuth.getSessionUser() || { name: "Member", role: "User", email: "" };
        const assignedCenterCount = (profile.assignedCenterIds || []).length;
        const displayLabel = window.NoorLocatorAuth.formatUserDisplayName(profile);

        populateCards("profile-cards", [
            {
                title: t("profile.cards.identity.title", "Current profile identity"),
                body: t(
                    "profile.cards.identity.body",
                    "{name} is the profile identity NoorLocator shows in navigation. Profile edits update personal details only and do not change permissions.",
                    { name: displayLabel })
            },
            {
                title: t("profile.cards.email.title", "Email on file"),
                body: profile.email
                    ? t(
                        "profile.cards.email.body",
                        "{email} is the account email NoorLocator currently uses for sign-in and account contact.",
                        { email: profile.email })
                    : t("profile.cards.email.empty", "No account email is available right now.")
            },
            {
                title: t("profile.cards.verification.title", "Verification status"),
                body: profile.isEmailVerified
                    ? t("profile.cards.verification.verified", "This email address is verified and trusted for protected features and email delivery.")
                    : t("profile.cards.verification.unverified", "This email address still needs verification before NoorLocator unlocks trusted features.")
            },
            {
                title: t("profile.cards.centers.title", "Assigned centers"),
                body: assignedCenterCount
                    ? t("profile.cards.centers.body", "{count} approved center assignments are linked to this account.", { count: assignedCenterCount })
                    : t("profile.cards.centers.empty", "No approved center assignments are currently linked to this account.")
            }
        ]);
    }

    async function loadProfile(successMessage = "Your profile is ready to edit.") {
        try {
            if (preferredLanguageSelect instanceof HTMLSelectElement) {
                populateSelectOptions(preferredLanguageSelect, window.NoorLocatorI18n.getSupportedLanguages(), {
                    placeholder: t("profile.language.placeholder", "Choose language"),
                    getValue: language => language.code,
                    getLabel: language => window.NoorLocatorI18n.getLanguageOptionLabel?.(language.code, {
                        native: true
                    }) || language.nativeName
                });
            }

            const requests = [
                window.NoorLocatorAuth.syncCurrentUser(),
                window.NoorLocatorApi.getMyProfile()
            ];

            if (preferencesForm) {
                requests.push(window.NoorLocatorApi.getMyNotificationPreferences());
            }

            const [userResponse, profileResponse, preferencesResponse] = await Promise.all(requests);

            state.profile = profileResponse.data || userResponse || window.NoorLocatorAuth.getSessionUser();
            state.preferences = preferencesResponse?.data || state.preferences;
            renderProfile(state.profile);
            renderPreferences(state.preferences);
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

    resendVerificationButton?.addEventListener("click", async () => {
        resendVerificationButton.disabled = true;
        setMessage(pageMessage, "Sending a fresh verification email...");

        try {
            const response = await window.NoorLocatorApi.resendVerificationEmail({ email: state.profile?.email || "" });
            setMessage(pageMessage, response.message || "A new verification email has been sent.", "success");
        } catch (error) {
            setMessage(pageMessage, normalizeErrorMessage(error, "NoorLocator could not resend the verification email right now."), "error");
        } finally {
            resendVerificationButton.disabled = false;
        }
    });

    preferencesForm?.addEventListener("submit", async event => {
        event.preventDefault();
        const values = Object.fromEntries(new FormData(preferencesForm).entries());
        const payload = {
            emailNotificationsEnabled: values.emailNotificationsEnabled === "on",
            appNotificationsEnabled: values.appNotificationsEnabled === "on",
            majlisNotificationsEnabled: values.majlisNotificationsEnabled === "on",
            eventNotificationsEnabled: values.eventNotificationsEnabled === "on",
            centerUpdatesEnabled: values.centerUpdatesEnabled === "on"
        };

        setSubmitButtonState(preferencesForm, true, "Saving preferences...");
        setMessage(preferencesMessage, "Saving your notification settings...");

        try {
            const response = await window.NoorLocatorApi.updateMyNotificationPreferences(payload);
            state.preferences = response.data || payload;
            renderPreferences(state.preferences);
            setMessage(preferencesMessage, response.message || "Notification settings updated successfully.", "success");
        } catch (error) {
            setMessage(preferencesMessage, normalizeErrorMessage(error, "Notification settings could not be updated right now."), "error");
        } finally {
            setSubmitButtonState(preferencesForm, false, "Saving preferences...");
        }
    });

    preferredLanguageForm?.addEventListener("submit", async event => {
        event.preventDefault();
        if (!(preferredLanguageSelect instanceof HTMLSelectElement)) {
            return;
        }

        setSubmitButtonState(preferredLanguageForm, true, "Saving language preference...");
        setMessage(preferredLanguageMessage, "Saving your language preference...");

        try {
            const response = await window.NoorLocatorApi.updateMyPreferredLanguage({
                preferredLanguageCode: preferredLanguageSelect.value
            });

            state.profile = response.data || state.profile;
            if (state.profile) {
                window.NoorLocatorAuth.updateSessionUser(state.profile);
            }

            setMessage(preferredLanguageMessage, response.message || "Preferred language updated successfully.", "success");
            await window.NoorLocatorI18n.setLanguage(preferredLanguageSelect.value, {
                reload: true,
                savePreference: false
            });
        } catch (error) {
            setMessage(preferredLanguageMessage, normalizeErrorMessage(error, "Preferred language could not be updated right now."), "error");
        } finally {
            setSubmitButtonState(preferredLanguageForm, false, "Saving language preference...");
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

function bindAuthForm(formId, submitAction, options = {}) {
    const form = document.getElementById(formId);
    const message = document.querySelector(`[data-form-message="${formId}"]`);

    if (!form) {
        return;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const formData = Object.fromEntries(new FormData(form).entries());
        const busyMessage = formId === "login-form"
            ? "Signing you in..."
            : formId === "register-form"
                ? "Creating your account..."
                : "Submitting...";
        setMessage(message, busyMessage);

        try {
            const response = await submitAction(formData);
            if (response.data) {
                window.NoorLocatorAuth.setSession(response.data);
            }

            setMessage(message, response.message, "success");

            if (typeof options.onSuccess === "function") {
                options.onSuccess(response, formData, form, message);
                return;
            }

            window.setTimeout(() => {
                window.location.href = window.NoorLocatorAuth.getDefaultRoute();
            }, 500);
        } catch (error) {
            if (typeof options.onError === "function") {
                options.onError(error, formData, form, message);
                return;
            }

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
        const displayLabel = window.NoorLocatorAuth.formatUserDisplayName(currentUser);
        populateCards("dashboard-cards", [
            {
                title: t("dashboard.cards.account.title", "Signed in"),
                body: t(
                    "dashboard.cards.account.body",
                    "{name} is the current NoorLocator account for this workspace. Every submission enters moderation before it reaches the public directory.",
                    { name: displayLabel })
            },
            {
                title: t("dashboard.cards.requests.title", "My center requests"),
                body: state.requests.length
                    ? t(
                        "dashboard.cards.requests.body",
                        "You have {count} center requests on file, with statuses such as Pending, Approved, and Rejected.",
                        { count: state.requests.length })
                    : t("dashboard.cards.requests.empty", "You have not submitted a center request yet.")
            },
            {
                title: t("dashboard.cards.centers.title", "Published centers"),
                body: state.centers.length
                    ? t(
                        "dashboard.cards.centers.body",
                        "{count} published centers are available for manager requests and language suggestions.",
                        { count: state.centers.length })
                    : t("dashboard.cards.centers.empty", "Published centers could not be loaded right now.")
            },
            {
                title: t("dashboard.cards.languages.title", "Predefined languages"),
                body: state.languages.length
                    ? t(
                        "dashboard.cards.languages.body",
                        "{count} predefined language options are ready to use in your forms.",
                        { count: state.languages.length })
                    : t("dashboard.cards.languages.empty", "Language lookup data is currently unavailable.")
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
            placeholder: state.centers.length
                ? t("common.selectCenter", "Select a center")
                : t("dashboard.selects.noCenters", "No published centers available"),
            getValue: center => String(center.id),
            getLabel: center => `${center.name} (${center.city}, ${center.country})`
        });

        populateSelectOptions([languageSelect], state.languages, {
            placeholder: state.languages.length
                ? t("common.selectLanguage", "Select a language")
                : t("common.noLanguagesAvailable", "No languages available"),
            getValue: language => String(language.id),
            getLabel: language => `${getLanguageOptionText(language.code)} (${language.code})`
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
            busyMessage: t("dashboard.messages.submittingCenterRequest", "Submitting center request..."),
            fallbackSuccessMessage: t("dashboard.messages.centerRequestSubmitted", "Center request submitted for review."),
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
            busyMessage: t("dashboard.messages.submittingSuggestion", "Submitting suggestion..."),
            fallbackSuccessMessage: t("dashboard.messages.suggestionSubmitted", "Suggestion submitted for review.")
        });

    bindContributionForm(
        languageSuggestionForm,
        payload => window.NoorLocatorApi.createCenterLanguageSuggestion(payload),
        values => ({
            centerId: Number(values.centerId),
            languageId: Number(values.languageId)
        }),
        {
            busyMessage: t("dashboard.messages.submittingLanguageSuggestion", "Submitting language suggestion..."),
            fallbackSuccessMessage: t("dashboard.messages.languageSuggestionSubmitted", "Language suggestion submitted for review.")
        });

    bindContributionForm(
        managerRequestForm,
        payload => window.NoorLocatorApi.requestManagerAccess(payload),
        values => ({
            centerId: Number(values.centerId)
        }),
        {
            busyMessage: t("dashboard.messages.submittingManagerRequest", "Submitting manager request..."),
            fallbackSuccessMessage: t("dashboard.messages.managerRequestSubmitted", "Manager request submitted for review.")
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
            t("dashboard.page.ready", "Your contribution tools are ready. New submissions are stored as pending until a moderator reviews them."),
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
        setContainerMessage(container, t("manager.centers.empty", "No approved centers are currently assigned to this manager account."), "soft");
        return;
    }

    container.innerHTML = centers.map(center => `
        <article class="card">
            <div class="card__header">
                <span class="card__meta">${escapeHtml(`${center.city}, ${center.country}`)}</span>
                <span class="status-pill status-pill--success">${escapeHtml(t("status.assigned", "Assigned"))}</span>
            </div>
            <h3>${escapeHtml(center.name)}</h3>
            <p class="card__excerpt">${escapeHtml(truncateText(center.description || t("manager.centers.defaultDescription", "This center is ready for majlis publishing through the manager workspace."), 150))}</p>
            <p>${escapeHtml(center.address)}</p>
        </article>
    `).join("");
}

function renderMajlisLanguageOptions(container, languages, selectedIds = []) {
    if (!container) {
        return;
    }

    if (!languages.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("manager.form.languagesEmpty", "No predefined languages are available."))}</div>`;
        return;
    }

    const selected = new Set(selectedIds.map(Number));
    container.innerHTML = languages.map(language => `
        <label class="checkbox-card">
            <input type="checkbox" name="languageIds" value="${escapeHtml(language.id)}"${selected.has(Number(language.id)) ? " checked" : ""}>
            <span class="checkbox-card__copy">
                <strong class="checkbox-card__title">
                    ${window.NoorLocatorI18n.getLanguageMetadata?.(language.code)?.flag ? `<span class="checkbox-card__flag" aria-hidden="true">${escapeHtml(window.NoorLocatorI18n.getLanguageMetadata(language.code).flag)}</span>` : ""}
                    <span>${escapeHtml(getLanguageDisplayLabel(language.code))}</span>
                </strong>
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
        setContainerMessage(container, t("manager.majlisList.empty", "No majalis are published for the selected center yet."), "soft");
        return;
    }

    container.innerHTML = majalis.map(majlis => `
        <article class="list-card">
            ${majlis.imageUrl ? `<img class="majlis-card__image" src="${escapeHtml(majlis.imageUrl)}" alt="${escapeHtml(t("manager.majlisList.imageAlt", "{title} image", { title: majlis.title }))}" loading="lazy">` : ""}
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(majlis.title)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${majlis.centerName} (${majlis.centerCity}, ${majlis.centerCountry})`)}</p>
                </div>
                <span class="status-pill">${escapeHtml(formatDateTime(majlis.date))}</span>
            </div>
            <p>${escapeHtml(truncateText(majlis.description || t("manager.majlisList.defaultDescription", "No public description is available for this majlis yet."), 190))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("manager.majlisList.time", "Time: {time}", { time: majlis.time || t("common.timeToBeConfirmed", "To be announced") }))}</span>
                ${renderLanguageBadgeList(majlis.languages, { modifier: "muted" })}
            </div>
            <div class="button-row">
                <button class="button button--secondary" type="button" data-edit-majlis-id="${escapeHtml(majlis.id)}">${escapeHtml(t("actions.edit", "Edit"))}</button>
                <button class="button button--danger" type="button" data-delete-majlis-id="${escapeHtml(majlis.id)}" data-majlis-title="${escapeHtml(majlis.title)}">${escapeHtml(t("actions.delete", "Delete"))}</button>
            </div>
        </article>
    `).join("");
}

function renderManagerAnnouncements(container, announcements) {
    renderCenterAnnouncements(container, announcements, {
        manageable: true,
        emptyMessage: t("manager.announcementList.empty", "No announcements exist for the selected center yet.")
    });
}

function renderManagerCenterImages(container, images) {
    renderCenterGallery(container, images, {
        manageable: true,
        emptyMessage: t("manager.gallery.empty", "No gallery images have been uploaded for the selected center yet."),
        imageAlt: t("manager.gallery.imageAlt", "Managed center gallery image")
    });
}

function renderAdminCenterImages(container, images) {
    renderCenterGallery(container, images, {
        manageable: true,
        emptyMessage: t("admin.gallery.empty", "No gallery images are currently stored for the selected center."),
        imageAlt: t("admin.gallery.imageAlt", "Admin moderated center gallery image")
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
    setContainerMessage(centersContainer, t("manager.centers.loading", "Loading your assigned centers..."), "soft");
    setContainerMessage(majlisListContainer, t("manager.majlisList.loading", "Loading majalis..."), "soft");
    setContainerMessage(announcementsContainer, t("manager.announcementList.loading", "Loading announcements..."), "soft");
    setContainerMessage(centerImagesContainer, t("manager.gallery.loading", "Loading center gallery..."), "soft");
    setMessage(pageMessage, t("manager.page.loading", "Loading your manager workspace..."));

    function updateCounts() {
        centerCount.textContent = String(state.centers.length);
        majlisCount.textContent = String(state.majalis.length);
        announcementCount.textContent = String(state.announcements.length);
        imageCount.textContent = String(state.centerImages.length);
    }

    function refreshOverviewCards() {
        const currentUser = state.user || window.NoorLocatorAuth.getSessionUser() || { name: "Manager", role: "Manager" };
        const displayLabel = window.NoorLocatorAuth.formatUserDisplayName(currentUser);
        populateCards("manager-cards", [
            {
                title: t("manager.cards.session.title", "Manager session"),
                body: t(
                    "manager.cards.session.body",
                    "{name} is the current NoorLocator account for this workspace. Majalis, announcements, and gallery changes are accepted only for approved center assignments.",
                    { name: displayLabel })
            },
            {
                title: t("manager.cards.centers.title", "Assigned centers"),
                body: state.centers.length
                    ? t("manager.cards.centers.body", "{count} centers are available in this workspace.", { count: state.centers.length })
                    : t("manager.cards.centers.empty", "No assigned centers are available for this account.")
            },
            {
                title: t("manager.cards.majalis.title", "Majalis in view"),
                body: state.majalis.length
                    ? t("manager.cards.majalis.body", "{count} majlis records are loaded for the selected center.", { count: state.majalis.length })
                    : t("manager.cards.majalis.empty", "No majalis are currently loaded for the selected center.")
            },
            {
                title: t("manager.cards.announcements.title", "Direct announcements"),
                body: state.announcements.length
                    ? t("manager.cards.announcements.body", "{count} announcements are loaded for the selected center.", { count: state.announcements.length })
                    : t("manager.cards.announcements.empty", "No announcements are currently loaded for the selected center.")
            },
            {
                title: t("manager.cards.gallery.title", "Center gallery"),
                body: state.centerImages.length
                    ? t("manager.cards.gallery.body", "{count} images are available for the selected center.", { count: state.centerImages.length })
                    : t("manager.cards.gallery.empty", "No gallery images are currently loaded for the selected center.")
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
        formHeading.textContent = t("manager.majlisEditor.title", "Create a new majlis");
        submitButton.textContent = t("manager.majlisEditor.submit", "Create majlis");
        submitButton.dataset.defaultLabel = t("manager.majlisEditor.submit", "Create majlis");
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

        setMessage(formMessage, t("manager.messages.majlisReady", "Create a majlis for one of your assigned centers. You can optionally add a poster image."));
    }

    function resetAnnouncementForm(preferredCenterId = null) {
        state.editingAnnouncementId = null;
        announcementForm.reset();
        announcementForm.elements.namedItem("announcementId").value = "";
        announcementFormHeading.textContent = t("manager.announcements.title", "Create a center announcement");
        announcementSubmitButton.textContent = t("manager.announcements.submit", "Publish announcement");
        announcementSubmitButton.dataset.defaultLabel = t("manager.announcements.submit", "Publish announcement");
        announcementCancelButton.hidden = true;

        const fallbackCenterId = preferredCenterId || state.selectedCenterId || state.centers[0]?.id || null;
        if (fallbackCenterId) {
            announcementFormCenterSelect.value = String(fallbackCenterId);
        }

        setMessage(announcementFormMessage, t("manager.messages.announcementReady", "Announcements publish directly for your assigned centers."));
    }

    function resetImageUploadForm(preferredCenterId = null) {
        imageUploadForm.reset();
        resetUploadProgress(imageUploadProgress, imageUploadProgressMeta);

        const fallbackCenterId = preferredCenterId || state.selectedCenterId || state.centers[0]?.id || null;
        if (fallbackCenterId) {
            imageFormCenterSelect.value = String(fallbackCenterId);
        }

        setMessage(imageUploadFormMessage, t("manager.messages.imageReady", "Upload JPG, PNG, or WEBP files up to 5MB."));
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
            placeholder: state.centers.length
                ? t("common.selectCenter", "Select a center")
                : t("common.noCentersAvailable", "No centers available"),
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

                setMessage(formMessage, t("manager.messages.loadingMajlis", "Loading majlis details..."));

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
                    formHeading.textContent = t("manager.majlisEditor.editTitle", "Edit majlis");
                    submitButton.textContent = t("actions.saveChanges", "Save changes");
                    submitButton.dataset.defaultLabel = t("actions.saveChanges", "Save changes");
                    cancelButton.hidden = false;
                    renderMajlisLanguageOptions(languageOptions, state.languages, (majlis.languages || []).map(language => language.id));
                    setMessage(
                        formMessage,
                        majlis.imageUrl
                            ? t("manager.messages.editingMajlisWithImage", "Editing \"{title}\". Leave the image empty to keep the current one, or check remove to clear it.", { title: majlis.title })
                            : t("manager.messages.editingMajlis", "Editing \"{title}\". You can add an optional image before saving.", { title: majlis.title }),
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

                if (!window.confirm(t("manager.confirm.deleteMajlis", "Delete \"{title}\" from NoorLocator?", { title }))) {
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

                setMessage(announcementFormMessage, t("manager.messages.loadingAnnouncement", "Loading announcement details..."));

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
                    announcementFormHeading.textContent = t("manager.announcements.editTitle", "Edit center announcement");
                    announcementSubmitButton.textContent = t("manager.announcements.save", "Save announcement");
                    announcementSubmitButton.dataset.defaultLabel = t("manager.announcements.save", "Save announcement");
                    announcementCancelButton.hidden = false;
                    setMessage(
                        announcementFormMessage,
                        announcement.imageUrl
                            ? t("manager.messages.editingAnnouncementWithImage", "Editing \"{title}\". Leave the image empty to keep the current one.", { title: announcement.title })
                            : t("manager.messages.editingAnnouncement", "Editing \"{title}\".", { title: announcement.title }),
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

                if (!window.confirm(t("manager.confirm.deleteAnnouncement", "Delete \"{title}\" from NoorLocator?", { title }))) {
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
                if (!window.confirm(t("manager.confirm.deleteImage", "Delete this center image from the gallery?"))) {
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
            setContainerMessage(majlisListContainer, t("manager.majlisList.selectCenter", "Select one of your assigned centers to manage majalis."), "soft");
            return;
        }

        setContainerMessage(majlisListContainer, t("manager.majlisList.loadingSelected", "Loading majalis for the selected center..."), "soft");

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
            setContainerMessage(announcementsContainer, t("manager.announcementList.selectCenter", "Select one of your assigned centers to manage announcements."), "soft");
            return;
        }

        setContainerMessage(announcementsContainer, t("manager.announcementList.loadingSelected", "Loading announcements for the selected center..."), "soft");

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
            setContainerMessage(centerImagesContainer, t("manager.gallery.selectCenter", "Select one of your assigned centers to manage the gallery."), "soft");
            return;
        }

        setContainerMessage(centerImagesContainer, t("manager.gallery.loading", "Loading center gallery..."), "soft");

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

    async function refreshSelectedCenterWorkspace(successMessage = t("manager.page.ready", "Manager workspace is ready.")) {
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
        setSubmitButtonState(form, true, state.editingMajlisId ? t("actions.saveChanges", "Saving changes...") : t("manager.majlisEditor.submitBusy", "Creating majlis..."));
        setMessage(formMessage, state.editingMajlisId ? t("manager.messages.savingMajlis", "Saving majlis changes...") : t("manager.majlisEditor.submitBusy", "Creating majlis..."));

        const selectedFile = majlisImageInput instanceof HTMLInputElement
            ? majlisImageInput.files?.[0]
            : null;
        const imageValidationError = getMajlisImageValidationError(selectedFile);
        if (imageValidationError) {
            setMessage(formMessage, imageValidationError, "error");
            setSubmitButtonState(form, false, state.editingMajlisId ? t("actions.saveChanges", "Saving changes...") : t("manager.majlisEditor.submitBusy", "Creating majlis..."));
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
            setSubmitButtonState(form, false, state.editingMajlisId ? t("actions.saveChanges", "Saving changes...") : t("manager.majlisEditor.submitBusy", "Creating majlis..."));
        }
    });

    if (majlisImageInput instanceof HTMLInputElement) {
        majlisImageInput.addEventListener("change", () => {
            const file = majlisImageInput.files?.[0];

            if (!file) {
                setMessage(
                    formMessage,
                    state.editingMajlisId
                        ? t("manager.messages.majlisImageKeepCurrent", "Leave the image empty to keep the current one, or choose a new file to replace it.")
                        : t("manager.messages.majlisReady", "Create a majlis for one of your assigned centers. You can optionally add a poster image."));
                return;
            }

            const validationError = getMajlisImageValidationError(file);
            if (validationError) {
                majlisImageInput.value = "";
                setMessage(formMessage, validationError, "error");
                return;
            }

            setMessage(formMessage, t("manager.messages.majlisImageSelected", "{name} is ready to upload with this majlis ({size}).", { name: file.name, size: formatFileSize(file.size) }));
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
            state.editingAnnouncementId ? t("manager.announcements.saveBusy", "Saving announcement...") : t("manager.announcements.submitBusy", "Publishing announcement..."));
        setMessage(
            announcementFormMessage,
            state.editingAnnouncementId ? t("manager.messages.savingAnnouncement", "Saving announcement changes...") : t("manager.announcements.submitBusy", "Publishing announcement..."));

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
                state.editingAnnouncementId ? t("manager.announcements.saveBusy", "Saving announcement...") : t("manager.announcements.submitBusy", "Publishing announcement..."));
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
                setMessage(imageUploadFormMessage, t("manager.messages.imageReady", "Upload JPG, PNG, or WEBP files up to 5MB."));
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
                t("manager.messages.imageSelected", "{name} is ready to upload ({size}).", { name: file.name, size: formatFileSize(file.size) }));
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

        setSubmitButtonState(imageUploadForm, true, t("manager.gallery.uploadBusy", "Uploading image..."));
        setMessage(imageUploadFormMessage, t("manager.gallery.uploading", "Uploading center image..."));
        setUploadProgress(imageUploadProgress, imageUploadProgressMeta, 0, t("manager.gallery.uploadStarting", "Starting upload..."));

        const formData = new FormData(imageUploadForm);
        const centerId = Number(formData.get("centerId"));

        try {
            const response = await window.NoorLocatorApi.uploadCenterImage(formData, {
                onProgress: percent => {
                    if (typeof percent === "number") {
                        setUploadProgress(imageUploadProgress, imageUploadProgressMeta, percent, t("manager.gallery.uploadProgress", "Uploading image... {percent}%", { percent }));
                        return;
                    }

                    setUploadProgress(imageUploadProgress, imageUploadProgressMeta, null, t("manager.gallery.uploading", "Uploading center image..."));
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
            setSubmitButtonState(imageUploadForm, false, t("manager.gallery.uploadBusy", "Uploading image..."));
        }
    });

    const handleCenterFilterChange = async event => {
        const selectedCenterId = Number(event.target.value);
        syncCenterSelection(Number.isInteger(selectedCenterId) && selectedCenterId > 0 ? selectedCenterId : null);
        await refreshSelectedCenterWorkspace(t("manager.messages.centerRefreshed", "Manager content refreshed for the selected center."));
    };

    filterCenterSelect.addEventListener("change", handleCenterFilterChange);
    announcementFilterCenterSelect.addEventListener("change", handleCenterFilterChange);
    imageFilterCenterSelect.addEventListener("change", handleCenterFilterChange);

    refreshButton.addEventListener("click", async () => {
        await refreshSelectedCenterWorkspace(t("manager.messages.majalisRefreshed", "Majalis refreshed for the selected center."));
    });

    refreshAnnouncementsButton.addEventListener("click", async () => {
        await refreshSelectedCenterWorkspace(t("manager.messages.announcementsRefreshed", "Announcements refreshed for the selected center."));
    });

    refreshCenterImagesButton.addEventListener("click", async () => {
        await refreshSelectedCenterWorkspace(t("manager.messages.galleryRefreshed", "Center gallery refreshed for the selected center."));
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
            setMessage(pageMessage, t("manager.page.noCenters", "No assigned centers were found for this account."), "error");
            setContainerMessage(majlisListContainer, t("manager.majlisList.noCenters", "No majalis can be managed until a center assignment exists."), "soft");
            setContainerMessage(announcementsContainer, t("manager.announcementList.noCenters", "No announcements can be managed until a center assignment exists."), "soft");
            setContainerMessage(centerImagesContainer, t("manager.gallery.noCenters", "No center images can be managed until a center assignment exists."), "soft");
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
        setContainerMessage(container, t("admin.centerRequests.empty", "No center requests are waiting in the moderation queue."), "soft");
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
            <p>${escapeHtml(truncateText(request.description || t("admin.centerRequests.defaultDescription", "No description was submitted for this center request."), 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("admin.centerRequests.by", "By {name}", { name: request.requestedByUserName || request.requestedByUserEmail }))}</span>
                <span class="card__meta">${escapeHtml(request.requestedByUserEmail)}</span>
                <span class="card__meta">${escapeHtml(t("common.submittedOn", "Submitted {date}", { date: formatDateTime(request.createdAt) }))}</span>
            </div>
            <div class="button-row">
                <button class="button button--primary" type="button" data-admin-center-request-approve="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.approve", "Approve"))}</button>
                <button class="button button--danger" type="button" data-admin-center-request-reject="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.reject", "Reject"))}</button>
            </div>
        </article>
    `).join("");
}

function renderAdminManagerRequests(container, requests) {
    if (!container) {
        return;
    }

    if (!requests.length) {
        setContainerMessage(container, t("admin.managerRequests.empty", "No manager requests are waiting in the moderation queue."), "soft");
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
            <p>${escapeHtml(t("admin.managerRequests.requestedCenter", "Requested center: {center}", { center: `${request.centerName} (${request.centerCity}, ${request.centerCountry})` }))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("common.submittedOn", "Submitted {date}", { date: formatDateTime(request.createdAt) }))}</span>
            </div>
            <div class="button-row">
                <button class="button button--primary" type="button" data-admin-manager-request-approve="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.approve", "Approve"))}</button>
                <button class="button button--danger" type="button" data-admin-manager-request-reject="${escapeHtml(request.id)}"${String(request.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.reject", "Reject"))}</button>
            </div>
        </article>
    `).join("");
}

function renderAdminLanguageSuggestions(container, suggestions) {
    if (!container) {
        return;
    }

    if (!suggestions.length) {
        setContainerMessage(container, t("admin.languageSuggestions.empty", "No center language suggestions are waiting for review."), "soft");
        return;
    }

    container.innerHTML = suggestions.map(suggestion => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(suggestion.centerName)}</h4>
                    <div class="chip-list">
                        ${renderLanguageBadgeMarkup(suggestion.languageCode, {
                            modifier: "muted",
                            label: `${getLanguageDisplayLabel(suggestion.languageCode)} (${suggestion.languageCode})`
                        })}
                    </div>
                </div>
                ${renderStatusBadge(suggestion.status)}
            </div>
            <p>${escapeHtml(t("admin.languageSuggestions.by", "Suggested by {name}", { name: suggestion.suggestedByUserName || suggestion.suggestedByUserEmail }))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(suggestion.suggestedByUserEmail)}</span>
            </div>
            <div class="button-row">
                <button class="button button--primary" type="button" data-admin-language-suggestion-approve="${escapeHtml(suggestion.id)}"${String(suggestion.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.approve", "Approve"))}</button>
                <button class="button button--danger" type="button" data-admin-language-suggestion-reject="${escapeHtml(suggestion.id)}"${String(suggestion.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.reject", "Reject"))}</button>
            </div>
        </article>
    `).join("");
}

function renderAdminSuggestions(container, suggestions) {
    if (!container) {
        return;
    }

    if (!suggestions.length) {
        setContainerMessage(container, t("admin.appSuggestions.empty", "No app suggestions are waiting for review."), "soft");
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
                <span class="card__meta">${escapeHtml(t(`dashboard.suggestion.type.${String(suggestion.type).charAt(0).toLowerCase()}${String(suggestion.type).slice(1)}`, String(suggestion.type)))}</span>
                <span class="card__meta">${escapeHtml(t("common.submittedOn", "Submitted {date}", { date: formatDateTime(suggestion.createdAt) }))}</span>
            </div>
            <div class="button-row">
                <button class="button button--secondary" type="button" data-admin-suggestion-review="${escapeHtml(suggestion.id)}"${String(suggestion.status) !== "Pending" ? " disabled" : ""}>${escapeHtml(t("actions.markReviewed", "Mark reviewed"))}</button>
            </div>
        </article>
    `).join("");
}

function renderAdminCenters(container, centers) {
    if (!container) {
        return;
    }

    if (!centers.length) {
        setContainerMessage(container, t("admin.centers.empty", "No published centers are available to manage."), "soft");
        return;
    }

    container.innerHTML = centers.map(center => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(center.name)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${center.city}, ${center.country}`)}</p>
                </div>
                <span class="status-pill status-pill--muted">${escapeHtml(t("admin.centers.managerCount", "{count} managers", { count: center.managerCount }))}</span>
            </div>
            <p>${escapeHtml(center.address)}</p>
            <p>${escapeHtml(truncateText(center.description || t("admin.centers.defaultDescription", "No public description is available for this center."), 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("admin.centers.majlisCount", "{count} majalis", { count: center.majlisCount }))}</span>
                <span class="card__meta">${escapeHtml(t("admin.centers.languageCount", "{count} languages", { count: center.languageCount }))}</span>
            </div>
            <div class="button-row">
                <button class="button button--secondary" type="button" data-admin-center-edit="${escapeHtml(center.id)}">${escapeHtml(t("actions.edit", "Edit"))}</button>
                <button class="button button--danger" type="button" data-admin-center-delete="${escapeHtml(center.id)}" data-admin-center-name="${escapeHtml(center.name)}">${escapeHtml(t("actions.delete", "Delete"))}</button>
            </div>
        </article>
    `).join("");
}

function renderAdminUsersTable(container, users, options = {}) {
    if (!container) {
        return;
    }

    const selectedUserId = Number(options.selectedUserId || 0);
    const hasFilters = Boolean(options.searchTerm || options.roleFilter || options.verificationFilter);

    if (!users.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(hasFilters
            ? t("admin.users.filteredEmpty", "No users match the current filters.")
            : t("admin.users.empty", "No users are available."))}</div>`;
        return;
    }

    container.innerHTML = `
        <div class="admin-user-list" role="list">
            <div class="admin-user-list__legend" aria-hidden="true">
                <span>${escapeHtml(t("admin.users.columns.user", "User"))}</span>
                <span>${escapeHtml(t("admin.users.columns.snapshot", "Account snapshot"))}</span>
                <span>${escapeHtml(t("admin.users.columns.actions", "Actions"))}</span>
            </div>
            ${users.map(user => `
                <article class="admin-user-row${selectedUserId === Number(user.id) ? " is-selected" : ""}" role="listitem">
                    <div class="admin-user-row__primary">
                        <div class="admin-user-row__identity">
                            <p class="admin-user-row__name">${escapeHtml(user.name)}</p>
                            <p class="admin-user-row__email">${escapeHtml(user.email)}</p>
                        </div>
                        ${!user.canDelete && user.deleteBlockedReason
                            ? `<span class="chip chip--danger chip--compact" title="${escapeHtml(user.deleteBlockedReason)}">${escapeHtml(t("admin.users.protected", "Protected"))}</span>`
                            : ""}
                    </div>
                    <div class="admin-user-row__snapshot">
                        <span class="status-pill status-pill--muted">${escapeHtml(window.NoorLocatorAuth.getLocalizedRoleLabel(user.role))}</span>
                        <span class="status-pill${user.isEmailVerified ? " status-pill--success" : ""}">${escapeHtml(user.isEmailVerified
                            ? t("status.verified", "Verified")
                            : t("status.unverified", "Unverified"))}</span>
                        ${renderLanguageBadgeMarkup(user.preferredLanguageCode || "en", {
                            modifier: "muted",
                            className: "admin-user-row__language"
                        })}
                        <span class="status-pill status-pill--muted">${escapeHtml(t("admin.users.assignedCenterCountCompact", "{count} centers", { count: user.assignedCenterCount || 0 }))}</span>
                    </div>
                    <div class="admin-user-row__actions">
                        <button class="button button--secondary button--inline" type="button" data-admin-user-edit="${escapeHtml(user.id)}">${escapeHtml(t("actions.edit", "Edit"))}</button>
                        <button class="button button--danger button--inline" type="button" data-admin-user-delete="${escapeHtml(user.id)}" data-admin-user-name="${escapeHtml(user.name)}"${user.canDelete ? "" : ` disabled title="${escapeHtml(user.deleteBlockedReason || t("admin.users.deleteBlocked", "Deletion blocked"))}"`}>${escapeHtml(t("actions.delete", "Delete"))}</button>
                    </div>
                </article>
            `).join("")}
        </div>
    `;
}

function renderAdminUserDetailSummary(container, user) {
    if (!container) {
        return;
    }

    if (!user) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.users.selectPrompt", "Choose a user from the list to load editable account details."))}</div>`;
        return;
    }

    const deleteStatusLabel = user.canDelete
        ? t("admin.users.deleteReady", "Safe to delete")
        : t("admin.users.deleteBlocked", "Deletion blocked");

    container.innerHTML = `
        <article class="admin-summary">
            <div class="admin-summary__header">
                <div>
                    <h4>${escapeHtml(user.name)}</h4>
                    <p class="admin-summary__subtle">${escapeHtml(user.email)}</p>
                </div>
                <span class="status-pill">${escapeHtml(window.NoorLocatorAuth.getLocalizedRoleLabel(user.role))}</span>
            </div>
            <div class="chip-list">
                <span class="chip${user.isEmailVerified ? " chip--success" : " chip--warning"}">${escapeHtml(user.isEmailVerified
                    ? t("status.verified", "Verified")
                    : t("status.unverified", "Unverified"))}</span>
                ${renderLanguageBadgeMarkup(user.preferredLanguageCode || "en", { modifier: "muted" })}
                <span class="chip chip--muted">${escapeHtml(t("admin.users.assignedCenterCount", "{count} assigned centers", { count: user.assignedCenterCount || 0 }))}</span>
                <span class="chip${user.canDelete ? " chip--success" : " chip--danger"}">${escapeHtml(deleteStatusLabel)}</span>
            </div>
            <div class="admin-summary__grid">
                <div class="admin-summary__item">
                    <span class="admin-summary__label">${escapeHtml(t("admin.users.summary.created", "Created"))}</span>
                    <strong>${escapeHtml(formatDateTime(user.createdAt))}</strong>
                </div>
                <div class="admin-summary__item">
                    <span class="admin-summary__label">${escapeHtml(t("admin.users.summary.lastSignIn", "Last sign-in"))}</span>
                    <strong>${escapeHtml(user.lastLoginAtUtc
                        ? formatDateTime(user.lastLoginAtUtc)
                        : t("profile.lastLogin.none", "No completed sign-in yet"))}</strong>
                </div>
                <div class="admin-summary__item">
                    <span class="admin-summary__label">${escapeHtml(t("admin.users.summary.updated", "Last updated"))}</span>
                    <strong>${escapeHtml(user.updatedAtUtc
                        ? formatDateTime(user.updatedAtUtc)
                        : t("common.unavailable", "Unavailable"))}</strong>
                </div>
                <div class="admin-summary__item">
                    <span class="admin-summary__label">${escapeHtml(t("admin.users.summary.emailStatus", "Email status"))}</span>
                    <strong>${escapeHtml(user.isEmailVerified
                        ? t("status.verified", "Verified")
                        : t("status.unverified", "Unverified"))}</strong>
                </div>
            </div>
            ${!user.canDelete && user.deleteBlockedReason
                ? `<p class="admin-summary__note">${escapeHtml(user.deleteBlockedReason)}</p>`
                : ""}
        </article>
    `;
}

function renderAdminUserNotificationStatus(container, notificationPreference) {
    if (!container) {
        return;
    }

    if (!notificationPreference) {
        container.innerHTML = "";
        return;
    }

    const preferences = [
        [t("profile.notifications.email", "Email notifications"), notificationPreference.emailNotificationsEnabled],
        [t("profile.notifications.app", "In-app notifications"), notificationPreference.appNotificationsEnabled],
        [t("profile.notifications.majlis", "Majlis updates"), notificationPreference.majlisNotificationsEnabled],
        [t("profile.notifications.events", "Event announcements"), notificationPreference.eventNotificationsEnabled],
        [t("profile.notifications.centerUpdates", "Center activity updates"), notificationPreference.centerUpdatesEnabled]
    ];

    container.innerHTML = preferences
        .map(([label, enabled]) => `<span class="chip${enabled ? " chip--success" : " chip--muted"}">${escapeHtml(`${label}: ${enabled ? t("common.enabled", "Enabled") : t("common.disabled", "Disabled")}`)}</span>`)
        .join("");
}

function renderAdminManagerAssignmentsTable(container, assignments, options = {}) {
    if (!container) {
        return;
    }

    if (!assignments.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.assignments.empty", "No approved manager assignments are available."))}</div>`;
        return;
    }

    const editingAssignmentId = Number(options.editingAssignmentId || 0);
    const selectedUserId = Number(options.selectedUserId || 0);

    container.innerHTML = `
        <table class="data-table">
            <thead>
                <tr>
                    <th>${escapeHtml(t("admin.assignments.table.manager", "Manager"))}</th>
                    <th>${escapeHtml(t("admin.assignments.table.role", "Role"))}</th>
                    <th>${escapeHtml(t("admin.assignments.table.center", "Center"))}</th>
                    <th>${escapeHtml(t("admin.assignments.table.majalis", "Majalis"))}</th>
                    <th>${escapeHtml(t("admin.assignments.table.announcements", "Announcements"))}</th>
                    <th>${escapeHtml(t("admin.assignments.table.actions", "Actions"))}</th>
                </tr>
            </thead>
            <tbody>
                ${assignments.map(assignment => `
                    <tr${editingAssignmentId === Number(assignment.id) || selectedUserId === Number(assignment.userId) ? ' class="is-selected"' : ""}>
                        <td data-label="${escapeHtml(t("admin.assignments.table.manager", "Manager"))}">
                            <strong>${escapeHtml(assignment.userName)}</strong>
                            <div class="data-table__subtle">${escapeHtml(assignment.userEmail)}</div>
                        </td>
                        <td data-label="${escapeHtml(t("admin.assignments.table.role", "Role"))}">${escapeHtml(window.NoorLocatorAuth.getLocalizedRoleLabel(assignment.userRole))}</td>
                        <td data-label="${escapeHtml(t("admin.assignments.table.center", "Center"))}">
                            <strong>${escapeHtml(assignment.centerName)}</strong>
                            <div class="data-table__subtle">${escapeHtml(`${assignment.centerCity}, ${assignment.centerCountry}`)}</div>
                        </td>
                        <td data-label="${escapeHtml(t("admin.assignments.table.majalis", "Majalis"))}">${escapeHtml(String(assignment.majlisCount || 0))}</td>
                        <td data-label="${escapeHtml(t("admin.assignments.table.announcements", "Announcements"))}">${escapeHtml(String(assignment.announcementCount || 0))}</td>
                        <td data-label="${escapeHtml(t("admin.assignments.table.actions", "Actions"))}">
                            <div class="utility-row utility-row--wrap">
                                <button class="button button--ghost button--inline" type="button" data-admin-manager-assignment-open-user="${escapeHtml(assignment.userId)}">${escapeHtml(t("actions.open", "Open"))}</button>
                                <button class="button button--secondary button--inline" type="button" data-admin-manager-assignment-edit="${escapeHtml(assignment.id)}">${escapeHtml(t("actions.edit", "Edit"))}</button>
                                <button class="button button--danger button--inline" type="button" data-admin-manager-assignment-delete="${escapeHtml(assignment.id)}" data-admin-manager-assignment-name="${escapeHtml(`${assignment.userName} -> ${assignment.centerName}`)}">${escapeHtml(t("actions.delete", "Delete"))}</button>
                            </div>
                        </td>
                    </tr>
                `).join("")}
            </tbody>
        </table>
    `;
}

function renderAdminSelectedUserCenters(container, user) {
    if (!container) {
        return;
    }

    if (!user) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.ownership.selectPrompt", "Choose a user to review their assignments."))}</div>`;
        return;
    }

    if (!(user.managedCenters || []).length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.ownership.centersEmpty", "This user does not currently manage any approved centers."))}</div>`;
        return;
    }

    container.innerHTML = user.managedCenters.map(center => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(center.centerName)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${center.centerCity}, ${center.centerCountry}`)}</p>
                </div>
                <span class="status-pill${center.approved ? " status-pill--success" : ""}">${escapeHtml(center.approved ? t("status.assigned", "Assigned") : t("status.pending", "Pending"))}</span>
            </div>
            <div class="button-row">
                <button class="button button--secondary button--inline" type="button" data-admin-managed-center-edit="${escapeHtml(center.assignmentId)}">${escapeHtml(t("actions.edit", "Edit"))}</button>
                <button class="button button--danger button--inline" type="button" data-admin-managed-center-remove="${escapeHtml(center.assignmentId)}" data-admin-managed-center-name="${escapeHtml(center.centerName)}">${escapeHtml(t("admin.assignments.removeAction", "Remove assignment"))}</button>
            </div>
        </article>
    `).join("");
}

function renderAdminSelectedUserMajalis(container, user) {
    if (!container) {
        return;
    }

    if (!user) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.ownership.selectPrompt", "Choose a user to review their assignments."))}</div>`;
        return;
    }

    if (!(user.createdMajalis || []).length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.ownership.majalisEmpty", "This user has not created any majalis."))}</div>`;
        return;
    }

    container.innerHTML = user.createdMajalis.map(majlis => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(majlis.title)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${formatDate(majlis.date)} | ${majlis.time}`)}</p>
                </div>
                <span class="status-pill status-pill--muted">${escapeHtml(majlis.centerName)}</span>
            </div>
            <p>${escapeHtml(truncateText(majlis.description || t("manager.majlisList.emptyDescription", "No description is available for this majlis."), 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(`${majlis.centerCity}, ${majlis.centerCountry}`)}</span>
            </div>
            <div class="chip-list">${renderLanguageBadgeList(majlis.languages || [], { modifier: "muted" })}</div>
        </article>
    `).join("");
}

function renderAdminSelectedUserAnnouncements(container, user) {
    if (!container) {
        return;
    }

    if (!user) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.ownership.selectPrompt", "Choose a user to review their assignments."))}</div>`;
        return;
    }

    if (!(user.createdAnnouncements || []).length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.ownership.announcementsEmpty", "This user has not created any center announcements."))}</div>`;
        return;
    }

    container.innerHTML = user.createdAnnouncements.map(announcement => `
        <article class="list-card">
            <div class="list-card__head">
                <div>
                    <h4>${escapeHtml(announcement.title)}</h4>
                    <p class="list-card__meta">${escapeHtml(`${announcement.centerName} | ${announcement.centerCity}, ${announcement.centerCountry}`)}</p>
                </div>
                ${renderStatusBadge(announcement.status)}
            </div>
            <p>${escapeHtml(truncateText(announcement.description || t("manager.announcements.emptyDescription", "No description is available for this announcement."), 180))}</p>
            <div class="utility-row utility-row--wrap">
                <span class="card__meta">${escapeHtml(t("common.publishedOn", "Published {date}", { date: formatDateTime(announcement.createdAt) }))}</span>
            </div>
        </article>
    `).join("");
}

function renderAdminAuditLogsTable(container, auditLogs) {
    if (!container) {
        return;
    }

    if (!auditLogs.length) {
        container.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.audit.empty", "No audit log entries are available."))}</div>`;
        return;
    }

    container.innerHTML = `
        <div class="table-shell">
            <table class="data-table">
                <thead>
                    <tr>
                        <th>${escapeHtml(t("admin.audit.table.when", "When"))}</th>
                        <th>${escapeHtml(t("admin.audit.table.action", "Action"))}</th>
                        <th>${escapeHtml(t("admin.audit.table.entity", "Entity"))}</th>
                        <th>${escapeHtml(t("admin.audit.table.user", "User"))}</th>
                        <th>${escapeHtml(t("admin.audit.table.ip", "IP"))}</th>
                        <th>${escapeHtml(t("admin.audit.table.metadata", "Metadata"))}</th>
                    </tr>
                </thead>
                <tbody>
                    ${auditLogs.map(log => `
                        <tr>
                            <td data-label="${escapeHtml(t("admin.audit.table.when", "When"))}">${escapeHtml(formatDateTime(log.createdAt))}</td>
                            <td data-label="${escapeHtml(t("admin.audit.table.action", "Action"))}">${escapeHtml(log.action)}</td>
                            <td data-label="${escapeHtml(t("admin.audit.table.entity", "Entity"))}">${escapeHtml(log.entityName)}${log.entityId ? ` <span class="data-table__mono">#${escapeHtml(log.entityId)}</span>` : ""}</td>
                            <td data-label="${escapeHtml(t("admin.audit.table.user", "User"))}">${escapeHtml(log.userName || log.userEmail || t("admin.audit.system", "System"))}</td>
                            <td data-label="${escapeHtml(t("admin.audit.table.ip", "IP"))}" class="data-table__mono">${escapeHtml(log.ipAddress || "-")}</td>
                            <td data-label="${escapeHtml(t("admin.audit.table.metadata", "Metadata"))}" class="data-table__mono">${escapeHtml(truncateText(log.metadata || "-", 140))}</td>
                        </tr>
                    `).join("")}
                </tbody>
            </table>
        </div>
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
    const userSearchInput = document.getElementById("admin-user-search");
    const userRoleFilterSelect = document.getElementById("admin-user-role-filter");
    const userVerificationFilterSelect = document.getElementById("admin-user-verification-filter");
    const usersRefreshButton = document.getElementById("admin-users-refresh-button");
    const usersTableContainer = document.getElementById("admin-users-table");
    const userEditorDrawer = document.getElementById("admin-user-editor-drawer");
    const userEditorCloseButtons = Array.from(document.querySelectorAll("[data-admin-user-editor-close]"));
    const userForm = document.getElementById("admin-user-form");
    const userFormHeading = document.getElementById("admin-user-form-heading");
    const userFormMessage = document.querySelector('[data-form-message="admin-user-form"]');
    const userSubmitButton = document.getElementById("admin-user-submit-button");
    const userDeleteButton = document.getElementById("admin-user-delete-button");
    const userDetailSummaryContainer = document.getElementById("admin-user-detail-summary");
    const userStatusChipsContainer = document.getElementById("admin-user-status-chips");
    const userNotificationStatusContainer = document.getElementById("admin-user-notification-status");
    const userLanguageSelect = document.getElementById("admin-user-language-select");
    const managerAssignmentForm = document.getElementById("admin-manager-assignment-form");
    const managerAssignmentFormHeading = document.getElementById("admin-manager-assignment-form-heading");
    const managerAssignmentFormMessage = document.querySelector('[data-form-message="admin-manager-assignment-form"]');
    const managerAssignmentSubmitButton = document.getElementById("admin-manager-assignment-submit-button");
    const managerAssignmentCancelButton = document.getElementById("admin-manager-assignment-cancel-button");
    const managerAssignmentUserSelect = document.getElementById("admin-manager-assignment-user-select");
    const managerAssignmentCenterSelect = document.getElementById("admin-manager-assignment-center-select");
    const managerAssignmentsTableContainer = document.getElementById("admin-manager-assignments-table");
    const ownershipDescription = document.getElementById("admin-ownership-description");
    const selectedUserCentersContainer = document.getElementById("admin-selected-user-centers");
    const selectedUserMajalisContainer = document.getElementById("admin-selected-user-majalis");
    const selectedUserAnnouncementsContainer = document.getElementById("admin-selected-user-announcements");
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
        managerAssignments: [],
        centers: [],
        centerImages: [],
        auditLogs: [],
        selectedUserId: null,
        selectedUser: null,
        userSearch: "",
        userRoleFilter: "",
        userVerificationFilter: "",
        editingManagerAssignmentId: null,
        editingCenterId: null,
        selectedImageCenterId: null
    };
    const adminAssetRecoveryKey = "noorlocator.admin.asset-recovery";

    if (!pageMessage || !pendingCount || !auditCount || !cardsContainer || !centerRequestsContainer || !managerRequestsContainer || !languageSuggestionsContainer || !suggestionsContainer || !centersContainer || !adminCenterImagesMessage || !adminCenterImagesContainer || !adminCenterImageFilterCenter || !refreshAdminCenterImagesButton || !userSearchInput || !userRoleFilterSelect || !userVerificationFilterSelect || !usersRefreshButton || !usersTableContainer || !userEditorDrawer || !userForm || !userFormHeading || !userFormMessage || !userSubmitButton || !userDeleteButton || !userDetailSummaryContainer || !userStatusChipsContainer || !userNotificationStatusContainer || !userLanguageSelect || !managerAssignmentForm || !managerAssignmentFormHeading || !managerAssignmentFormMessage || !managerAssignmentSubmitButton || !managerAssignmentCancelButton || !managerAssignmentUserSelect || !managerAssignmentCenterSelect || !managerAssignmentsTableContainer || !ownershipDescription || !selectedUserCentersContainer || !selectedUserMajalisContainer || !selectedUserAnnouncementsContainer || !auditLogsTableContainer || !centerForm || !centerFormHeading || !centerFormMessage || !centerSubmitButton || !centerCancelButton) {
        return;
    }

    async function ensureAdminApiSurface() {
        const requiredMethods = [
            "getAdminUser",
            "updateAdminUser",
            "deleteAdminUser",
            "getAdminManagerAssignments",
            "createAdminManagerAssignment",
            "updateAdminManagerAssignment",
            "deleteAdminManagerAssignment"
        ];
        const missingMethod = requiredMethods.find(methodName => typeof window.NoorLocatorApi?.[methodName] !== "function");

        if (!missingMethod) {
            try {
                window.sessionStorage.removeItem(adminAssetRecoveryKey);
            } catch {
                // Ignore session storage cleanup issues.
            }

            return true;
        }

        const alreadyRetried = (() => {
            try {
                return window.sessionStorage.getItem(adminAssetRecoveryKey) === "1";
            } catch {
                return false;
            }
        })();

        if (alreadyRetried) {
            const message = t(
                "admin.page.reloadRequired",
                "The admin page is still using outdated files. Refresh the page once to load the latest NoorLocator assets.");
            setMessage(pageMessage, message, "error");
            throw new Error(message);
        }

        setMessage(
            pageMessage,
            t(
                "admin.page.refreshingAssets",
                "Refreshing admin assets after a NoorLocator update..."));

        try {
            window.sessionStorage.setItem(adminAssetRecoveryKey, "1");
        } catch {
            // Ignore session storage failures.
        }

        try {
            if ("serviceWorker" in navigator) {
                const registrations = await navigator.serviceWorker.getRegistrations();
                await Promise.all(registrations.map(registration => registration.unregister()));
            }

            if ("caches" in window) {
                const cacheKeys = await caches.keys();
                await Promise.all(
                    cacheKeys
                        .filter(key => key.startsWith("noorlocator-"))
                        .map(key => caches.delete(key)));
            }
        } catch {
            // Ignore cache cleanup failures and continue with a reload.
        }

        window.location.reload();
        return false;
    }

    function isAdminUserEditorOpen() {
        return !userEditorDrawer.hidden;
    }

    function openAdminUserEditorDrawer() {
        if (isAdminUserEditorOpen()) {
            userEditorDrawer.classList.add("is-open");
            userEditorDrawer.setAttribute("aria-hidden", "false");
            document.body.classList.add("has-admin-user-drawer");
            return;
        }

        userEditorDrawer.hidden = false;
        userEditorDrawer.setAttribute("aria-hidden", "false");
        document.body.classList.add("has-admin-user-drawer");
        window.requestAnimationFrame(() => {
            userEditorDrawer.classList.add("is-open");
        });
    }

    function closeAdminUserEditorDrawer() {
        if (!isAdminUserEditorOpen()) {
            return;
        }

        userEditorDrawer.classList.remove("is-open");
        userEditorDrawer.setAttribute("aria-hidden", "true");
        document.body.classList.remove("has-admin-user-drawer");

        window.setTimeout(() => {
            if (!userEditorDrawer.classList.contains("is-open")) {
                userEditorDrawer.hidden = true;
            }
        }, 220);
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
        const currentUser = state.user || window.NoorLocatorAuth.getSessionUser() || { name: "Admin", role: "Admin" };
        const displayLabel = window.NoorLocatorAuth.formatUserDisplayName(currentUser);
        populateCards("admin-cards", [
            {
                title: t("admin.cards.queue.title", "Moderation queue"),
                body: t(
                    "admin.cards.queue.body",
                    "{name} currently has {count} pending items to review.",
                    {
                        name: displayLabel,
                        count: (state.dashboard?.pendingCenterRequests || 0) + (state.dashboard?.pendingManagerRequests || 0) + (state.dashboard?.pendingCenterLanguageSuggestions || 0) + (state.dashboard?.pendingSuggestions || 0)
                    })
            },
            {
                title: t("admin.cards.scale.title", "Platform scale"),
                body: t(
                    "admin.cards.scale.body",
                    "{users} users, {centers} centers, and {majalis} majalis are currently stored in NoorLocator.",
                    {
                        users: state.dashboard?.totalUsers || 0,
                        centers: state.dashboard?.totalCenters || 0,
                        majalis: state.dashboard?.totalMajalis || 0
                    })
            },
            {
                title: t("admin.cards.centers.title", "Center moderation"),
                body: t(
                    "admin.cards.centers.body",
                    "{centerRequests} center requests, {managerRequests} manager requests, and {languageSuggestions} language suggestions are awaiting action.",
                    {
                        centerRequests: state.dashboard?.pendingCenterRequests || 0,
                        managerRequests: state.dashboard?.pendingManagerRequests || 0,
                        languageSuggestions: state.dashboard?.pendingCenterLanguageSuggestions || 0
                    })
            },
            {
                title: t("admin.cards.audit.title", "Audit coverage"),
                body: t(
                    "admin.cards.audit.body",
                    "{count} audit log entries are available for admin traceability and change review.",
                    { count: state.dashboard?.totalAuditLogs || 0 })
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

    function getFilteredUsers() {
        const searchTerm = state.userSearch.trim().toLowerCase();

        return state.users.filter(user => {
            const matchesSearch = !searchTerm
                || user.name.toLowerCase().includes(searchTerm)
                || user.email.toLowerCase().includes(searchTerm);
            const matchesRole = !state.userRoleFilter || String(user.role) === state.userRoleFilter;
            const matchesVerification = !state.userVerificationFilter
                || (state.userVerificationFilter === "verified" && Boolean(user.isEmailVerified))
                || (state.userVerificationFilter === "unverified" && !user.isEmailVerified);

            return matchesSearch && matchesRole && matchesVerification;
        });
    }

    function getAssignableUsers() {
        return state.users.filter(user => String(user.role) !== "Admin");
    }

    function getEditingManagerAssignment() {
        return state.managerAssignments.find(assignment => assignment.id === state.editingManagerAssignmentId) || null;
    }

    function populateAdminUserLanguageSelect() {
        populateSelectOptions(
            [userLanguageSelect],
            window.NoorLocatorI18n.getSupportedLanguages(),
            {
                placeholder: t("profile.language.placeholder", "Choose language"),
                getValue: language => language.code,
                getLabel: language => window.NoorLocatorI18n.getLanguageOptionLabel?.(language.code, {
                    native: true
                }) || language.nativeName
            });
    }

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
                placeholder: state.centers.length
                    ? t("common.selectCenter", "Select a center")
                    : t("common.noCentersAvailable", "No centers available"),
                getValue: center => String(center.id),
                getLabel: center => `${center.name} (${center.city}, ${center.country})`
            });

        if (!state.centers.length) {
            state.selectedImageCenterId = null;
            return;
        }

        syncAdminImageCenterSelection(state.selectedImageCenterId || state.editingCenterId || state.centers[0].id);
    }

    function populateAdminAssignmentControls() {
        const assignableUsers = getAssignableUsers();
        const selectedUserCandidate = state.selectedUser && String(state.selectedUser.role) !== "Admin"
            ? String(state.selectedUser.id)
            : "";
        const editingAssignment = getEditingManagerAssignment();
        const currentUserValue = editingAssignment
            ? String(editingAssignment.userId)
            : (managerAssignmentUserSelect.value || selectedUserCandidate);
        const currentCenterValue = editingAssignment
            ? String(editingAssignment.centerId)
            : managerAssignmentCenterSelect.value;

        populateSelectOptions(
            [managerAssignmentUserSelect],
            assignableUsers,
            {
                placeholder: assignableUsers.length
                    ? t("admin.assignments.selectUser", "Select a manager account")
                    : t("admin.assignments.noUsers", "No eligible users"),
                getValue: user => String(user.id),
                getLabel: user => `${window.NoorLocatorAuth.formatUserDisplayName(user)} (${user.email})`
            });

        populateSelectOptions(
            [managerAssignmentCenterSelect],
            state.centers,
            {
                placeholder: state.centers.length
                    ? t("common.selectCenter", "Select a center")
                    : t("common.noCentersAvailable", "No centers available"),
                getValue: center => String(center.id),
                getLabel: center => `${center.name} (${center.city}, ${center.country})`
            });

        if (assignableUsers.some(user => String(user.id) === currentUserValue)) {
            managerAssignmentUserSelect.value = currentUserValue;
        } else if (selectedUserCandidate && assignableUsers.some(user => String(user.id) === selectedUserCandidate)) {
            managerAssignmentUserSelect.value = selectedUserCandidate;
        }

        if (state.centers.some(center => String(center.id) === currentCenterValue)) {
            managerAssignmentCenterSelect.value = currentCenterValue;
        }
    }

    async function loadAdminCenterImages() {
        populateAdminImageControls();

        if (!state.selectedImageCenterId) {
            state.centerImages = [];
            setMessage(adminCenterImagesMessage, t("admin.images.message", "Choose a center to review its gallery and moderate images if needed."));
            setContainerMessage(adminCenterImagesContainer, t("admin.gallery.selectCenter", "No center images can be reviewed until a center is selected."), "soft");
            return;
        }

        setMessage(adminCenterImagesMessage, t("admin.images.loadingSelected", "Loading the selected center gallery..."));
        setContainerMessage(adminCenterImagesContainer, t("admin.gallery.loading", "Loading center gallery..."), "soft");

        try {
            const response = await window.NoorLocatorApi.getCenterImages(state.selectedImageCenterId);
            state.centerImages = response.data || [];
            renderAdminCenterImages(adminCenterImagesContainer, state.centerImages);
            bindAdminImageActions();
            setMessage(adminCenterImagesMessage, t("admin.images.ready", "Admin gallery moderation tools are ready."), "success");
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
                if (!window.confirm(t("admin.confirm.deleteImage", "Delete this center image from the public gallery?"))) {
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

    function getAdminUserStatusChipsMarkup(user) {
        if (!user) {
            return "";
        }

        return [
            `<span class="chip${user.isEmailVerified ? " chip--success" : " chip--warning"}">${escapeHtml(user.isEmailVerified ? t("status.verified", "Verified") : t("status.unverified", "Unverified"))}</span>`,
            renderLanguageBadgeMarkup(user.preferredLanguageCode || "en", { modifier: "muted" }),
            `<span class="chip chip--muted">${escapeHtml(t("admin.users.assignedCenterCount", "{count} assigned centers", { count: user.assignedCenterCount || 0 }))}</span>`,
            `<span class="chip${user.canDelete ? " chip--success" : " chip--danger"}">${escapeHtml(user.canDelete ? t("admin.users.deleteReady", "Safe to delete") : t("admin.users.deleteBlocked", "Deletion blocked"))}</span>`
        ].join("");
    }

    function setAdminUserFormEnabled(enabled) {
        ["name", "email", "role", "preferredLanguageCode"].forEach(fieldName => {
            const field = userForm.elements.namedItem(fieldName);
            if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement) {
                field.disabled = !enabled;
            }
        });

        userSubmitButton.disabled = !enabled;
        userDeleteButton.disabled = !enabled;
    }

    function syncSelectedUserPresentation() {
        renderAdminUserDetailSummary(userDetailSummaryContainer, state.selectedUser);
        userStatusChipsContainer.innerHTML = getAdminUserStatusChipsMarkup(state.selectedUser);
        renderAdminUserNotificationStatus(userNotificationStatusContainer, state.selectedUser?.notificationPreference);
        renderAdminSelectedUserCenters(selectedUserCentersContainer, state.selectedUser);
        renderAdminSelectedUserMajalis(selectedUserMajalisContainer, state.selectedUser);
        renderAdminSelectedUserAnnouncements(selectedUserAnnouncementsContainer, state.selectedUser);
        ownershipDescription.textContent = state.selectedUser
            ? t("admin.ownership.selectedDescription", "Showing assignment and content ownership details for {name}.", { name: state.selectedUser.name })
            : t("admin.ownership.description", "Select a user to inspect their approved center assignments, created majalis, and center announcements.");
    }

    function resetAdminUserForm(options = {}) {
        state.selectedUser = null;
        state.selectedUserId = null;
        userForm.reset();
        userForm.elements.namedItem("userId").value = "";
        populateAdminUserLanguageSelect();
        userFormHeading.textContent = t("admin.users.detailsTitle", "Select a user to manage");
        userSubmitButton.textContent = t("actions.saveChanges", "Save changes");
        userSubmitButton.dataset.defaultLabel = t("actions.saveChanges", "Save changes");
        userDeleteButton.textContent = t("admin.users.deleteAction", "Delete user");
        userDeleteButton.title = "";
        setAdminUserFormEnabled(false);
        syncSelectedUserPresentation();

        if (!options.preserveMessage) {
            setMessage(userFormMessage, t("admin.users.selectPrompt", "Choose a user from the list to load editable account details."));
        }
    }

    function populateAdminUserForm(user, options = {}) {
        if (!user) {
            resetAdminUserForm(options);
            return;
        }

        populateAdminUserLanguageSelect();
        setAdminUserFormEnabled(true);
        userForm.elements.namedItem("userId").value = String(user.id);
        userForm.elements.namedItem("name").value = user.name || "";
        userForm.elements.namedItem("email").value = user.email || "";
        userForm.elements.namedItem("role").value = String(user.role || "User");
        userForm.elements.namedItem("preferredLanguageCode").value = user.preferredLanguageCode || "en";
        userFormHeading.textContent = t("admin.users.editingTitle", "Editing {name}", { name: user.name });
        userSubmitButton.textContent = t("actions.saveChanges", "Save changes");
        userSubmitButton.dataset.defaultLabel = t("actions.saveChanges", "Save changes");
        userDeleteButton.textContent = t("admin.users.deleteAction", "Delete user");
        userDeleteButton.disabled = !user.canDelete;
        userDeleteButton.title = user.canDelete ? "" : (user.deleteBlockedReason || "");
        syncSelectedUserPresentation();

        if (!options.preserveMessage) {
            setMessage(userFormMessage, t("admin.users.editingMessage", "Editing {name}.", { name: user.name }), "success");
        }
    }

    function resetManagerAssignmentForm(options = {}) {
        state.editingManagerAssignmentId = null;
        managerAssignmentForm.reset();
        managerAssignmentForm.elements.namedItem("assignmentId").value = "";
        managerAssignmentFormHeading.textContent = t("admin.assignments.editorTitle", "Create or update a manager assignment");
        managerAssignmentSubmitButton.textContent = t("admin.assignments.submit", "Save assignment");
        managerAssignmentSubmitButton.dataset.defaultLabel = t("admin.assignments.submit", "Save assignment");
        managerAssignmentCancelButton.hidden = true;
        populateAdminAssignmentControls();

        if (!options.preserveMessage) {
            setMessage(managerAssignmentFormMessage, t("admin.assignments.ready", "Choose a user and center to create an assignment."));
        }
    }

    function populateManagerAssignmentForm(assignment, options = {}) {
        if (!assignment) {
            resetManagerAssignmentForm(options);
            return;
        }

        state.editingManagerAssignmentId = assignment.id;
        populateAdminAssignmentControls();
        managerAssignmentForm.elements.namedItem("assignmentId").value = String(assignment.id);
        managerAssignmentUserSelect.value = String(assignment.userId);
        managerAssignmentCenterSelect.value = String(assignment.centerId);
        managerAssignmentFormHeading.textContent = t("admin.assignments.editingTitle", "Editing {user} and {center}", {
            user: assignment.userName,
            center: assignment.centerName
        });
        managerAssignmentSubmitButton.textContent = t("actions.saveChanges", "Save changes");
        managerAssignmentSubmitButton.dataset.defaultLabel = t("actions.saveChanges", "Save changes");
        managerAssignmentCancelButton.hidden = false;

        if (!options.preserveMessage) {
            setMessage(managerAssignmentFormMessage, t("admin.assignments.editingMessage", "Editing the assignment for {user}.", { user: assignment.userName }), "success");
        }
    }

    function resetCenterForm() {
        state.editingCenterId = null;
        centerForm.reset();
        centerForm.elements.namedItem("centerId").value = "";
        centerFormHeading.textContent = t("admin.centerEditor.title", "Edit a published center");
        centerSubmitButton.textContent = t("admin.centerEditor.submit", "Save center");
        centerSubmitButton.dataset.defaultLabel = t("admin.centerEditor.submit", "Save center");
        centerSubmitButton.disabled = true;
        centerCancelButton.hidden = true;
        setMessage(centerFormMessage, t("admin.centerEditor.choose", "Choose a center from the list to edit."));
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
        centerFormHeading.textContent = t("admin.centerEditor.editingTitle", "Editing {name}", { name: center.name });
        centerSubmitButton.textContent = t("actions.saveChanges", "Save changes");
        centerSubmitButton.dataset.defaultLabel = t("actions.saveChanges", "Save changes");
        centerSubmitButton.disabled = false;
        centerCancelButton.hidden = false;
        setMessage(centerFormMessage, t("admin.centerEditor.editingMessage", "Editing \"{name}\".", { name: center.name }), "success");
        syncAdminImageCenterSelection(center.id);
        loadAdminCenterImages().catch(() => {
            setMessage(adminCenterImagesMessage, "Center images could not be loaded for moderation.", "error");
        });
        document.getElementById("center-management")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function renderAllSections(options = {}) {
        renderAdminCenterRequests(centerRequestsContainer, state.centerRequests);
        renderAdminManagerRequests(managerRequestsContainer, state.managerRequests);
        renderAdminLanguageSuggestions(languageSuggestionsContainer, state.languageSuggestions);
        renderAdminSuggestions(suggestionsContainer, state.suggestions);
        renderAdminCenters(centersContainer, state.centers);
        populateAdminImageControls();
        renderAdminUsersTable(usersTableContainer, getFilteredUsers(), {
            selectedUserId: state.selectedUserId,
            searchTerm: state.userSearch,
            roleFilter: state.userRoleFilter,
            verificationFilter: state.userVerificationFilter
        });
        renderAdminManagerAssignmentsTable(managerAssignmentsTableContainer, state.managerAssignments, {
            selectedUserId: state.selectedUserId,
            editingAssignmentId: state.editingManagerAssignmentId
        });
        renderAdminAuditLogsTable(auditLogsTableContainer, state.auditLogs);
        populateAdminAssignmentControls();
        populateAdminUserForm(state.selectedUser, {
            preserveMessage: options.preserveUserFormMessage
        });

        if (state.editingManagerAssignmentId) {
            populateManagerAssignmentForm(getEditingManagerAssignment(), {
                preserveMessage: options.preserveAssignmentFormMessage
            });
        } else {
            resetManagerAssignmentForm({
                preserveMessage: options.preserveAssignmentFormMessage
            });
        }

        updateCounters();
        refreshOverviewCards();
        bindModerationActions();

        if (state.editingCenterId) {
            const editedCenter = state.centers.find(center => center.id === state.editingCenterId);
            if (editedCenter) {
                populateCenterForm(editedCenter);
                return;
            }
        }

        resetCenterForm();
    }

    async function hydrateSelectedUser() {
        if (!state.selectedUserId || !state.users.some(user => user.id === state.selectedUserId)) {
            state.selectedUser = null;
            state.selectedUserId = null;
            return;
        }

        try {
            const response = await window.NoorLocatorApi.getAdminUser(state.selectedUserId);
            state.selectedUser = response.data || null;
        } catch {
            state.selectedUser = null;
        }
    }

    async function selectAdminUser(id, options = {}) {
        const userId = Number(id);
        if (!Number.isInteger(userId) || userId <= 0) {
            resetAdminUserForm();
            renderAllSections({
                preserveUserFormMessage: false,
                preserveAssignmentFormMessage: true
            });
            return;
        }

        state.selectedUserId = userId;
        setMessage(userFormMessage, t("admin.users.loadingDetails", "Loading user details..."));
        userDetailSummaryContainer.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.users.loadingDetails", "Loading user details..."))}</div>`;

        try {
            const response = await window.NoorLocatorApi.getAdminUser(userId);
            state.selectedUser = response.data || null;
            renderAllSections({
                preserveUserFormMessage: true,
                preserveAssignmentFormMessage: true
            });
            setMessage(userFormMessage, t("admin.users.editingMessage", "Editing {name}.", { name: state.selectedUser?.name || "" }), "success");

            if (options.scroll !== false) {
                document.getElementById("user-management")?.scrollIntoView({ behavior: "smooth", block: "start" });
            }
        } catch (error) {
            const message = normalizeErrorMessage(error, "User details could not be loaded.");
            state.selectedUser = null;
            renderAllSections({
                preserveUserFormMessage: true,
                preserveAssignmentFormMessage: true
            });
            setMessage(userFormMessage, message, "error");
            showToast(message, "error");
        }
    }

    async function openAdminUserEditor(id) {
        openAdminUserEditorDrawer();
        await selectAdminUser(id, { scroll: false });

        const nameField = userForm.elements.namedItem("name");
        if (nameField instanceof HTMLInputElement) {
            window.requestAnimationFrame(() => {
                nameField.focus();
                nameField.select();
            });
        }
    }

    async function loadAdminWorkspace(showLoading = false) {
        if (showLoading) {
            setCardLoadingState(cardsContainer, 4);
            setContainerMessage(centerRequestsContainer, t("admin.centerRequests.loading", "Loading center requests..."), "soft");
            setContainerMessage(managerRequestsContainer, t("admin.managerRequests.loading", "Loading manager requests..."), "soft");
            setContainerMessage(languageSuggestionsContainer, t("admin.languageSuggestions.loading", "Loading language suggestions..."), "soft");
            setContainerMessage(suggestionsContainer, t("admin.appSuggestions.loading", "Loading app suggestions..."), "soft");
            setContainerMessage(centersContainer, t("admin.centers.loading", "Loading centers..."), "soft");
            setMessage(adminCenterImagesMessage, t("admin.images.loading", "Loading center gallery moderation tools..."));
            setContainerMessage(adminCenterImagesContainer, t("admin.gallery.loading", "Loading center gallery..."), "soft");
            usersTableContainer.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.users.loading", "Loading users..."))}</div>`;
            managerAssignmentsTableContainer.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.assignments.loading", "Loading manager assignments..."))}</div>`;
            auditLogsTableContainer.innerHTML = `<div class="empty-state empty-state--soft">${escapeHtml(t("admin.audit.loading", "Loading audit logs..."))}</div>`;
            setMessage(userFormMessage, t("admin.users.selectPrompt", "Choose a user from the list to load editable account details."));
            setMessage(managerAssignmentFormMessage, t("admin.assignments.ready", "Choose a user and center to create an assignment."));
        }

        const [
            user,
            dashboardResponse,
            centerRequestsResponse,
            managerRequestsResponse,
            languageSuggestionsResponse,
            suggestionsResponse,
            usersResponse,
            managerAssignmentsResponse,
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
            window.NoorLocatorApi.getAdminManagerAssignments(),
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
        state.managerAssignments = managerAssignmentsResponse.data || [];
        state.centers = centersResponse.data || [];
        state.auditLogs = auditLogsResponse.data || [];
        await hydrateSelectedUser();

        renderAllSections({
            preserveUserFormMessage: true,
            preserveAssignmentFormMessage: true
        });
        await loadAdminCenterImages();
        setMessage(pageMessage, t("admin.page.ready", "Admin workspace is ready."), "success");
    }

    async function runAdminAction(confirmMessage, action, options = {}) {
        if (confirmMessage && !window.confirm(confirmMessage)) {
            return;
        }

        try {
            const response = await action();
            await loadAdminWorkspace();

            if (typeof options.afterSuccess === "function") {
                await options.afterSuccess(response);
            }

            setMessage(pageMessage, response.message, "success");
            showToast(response.message, "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "The admin action could not be completed.");
            setMessage(pageMessage, message, "error");
            showToast(message, "error");
        }
    }

    async function deleteAdminUserFromList(id, name) {
        const userId = Number(id);
        if (!Number.isInteger(userId) || userId <= 0) {
            return;
        }

        const targetUser = state.users.find(user => user.id === userId);
        if (targetUser && !targetUser.canDelete) {
            const blockedMessage = targetUser.deleteBlockedReason || t("admin.users.deleteBlocked", "Deletion blocked");
            setMessage(pageMessage, blockedMessage, "error");
            showToast(blockedMessage, "error");
            return;
        }

        const isSelectedUser = state.selectedUserId === userId;
        await runAdminAction(
            t("admin.confirm.deleteUser", "Delete the account for {name}?", {
                name: name || targetUser?.name || t("common.member", "Member")
            }),
            () => window.NoorLocatorApi.deleteAdminUser(userId),
            {
                afterSuccess: () => {
                    if (isSelectedUser) {
                        closeAdminUserEditorDrawer();
                    }
                }
            });
    }

    function bindModerationActions() {
        centerRequestsContainer.querySelectorAll("[data-admin-center-request-approve]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-center-request-approve"));
                await runAdminAction(
                    t("admin.confirm.approveCenterRequest", "Approve this center request and publish it as a live center?"),
                    () => window.NoorLocatorApi.approveAdminCenterRequest(id));
            });
        });

        centerRequestsContainer.querySelectorAll("[data-admin-center-request-reject]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-center-request-reject"));
                await runAdminAction(
                    t("admin.confirm.rejectCenterRequest", "Reject this center request?"),
                    () => window.NoorLocatorApi.rejectAdminCenterRequest(id));
            });
        });

        managerRequestsContainer.querySelectorAll("[data-admin-manager-request-approve]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-manager-request-approve"));
                await runAdminAction(
                    t("admin.confirm.approveManagerRequest", "Approve this manager request and grant center access?"),
                    () => window.NoorLocatorApi.approveAdminManagerRequest(id));
            });
        });

        managerRequestsContainer.querySelectorAll("[data-admin-manager-request-reject]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-manager-request-reject"));
                await runAdminAction(
                    t("admin.confirm.rejectManagerRequest", "Reject this manager request?"),
                    () => window.NoorLocatorApi.rejectAdminManagerRequest(id));
            });
        });

        languageSuggestionsContainer.querySelectorAll("[data-admin-language-suggestion-approve]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-language-suggestion-approve"));
                await runAdminAction(
                    t("admin.confirm.approveLanguageSuggestion", "Approve this center language suggestion?"),
                    () => window.NoorLocatorApi.approveAdminCenterLanguageSuggestion(id));
            });
        });

        languageSuggestionsContainer.querySelectorAll("[data-admin-language-suggestion-reject]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-language-suggestion-reject"));
                await runAdminAction(
                    t("admin.confirm.rejectLanguageSuggestion", "Reject this center language suggestion?"),
                    () => window.NoorLocatorApi.rejectAdminCenterLanguageSuggestion(id));
            });
        });

        suggestionsContainer.querySelectorAll("[data-admin-suggestion-review]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-suggestion-review"));
                await runAdminAction(
                    t("admin.confirm.reviewSuggestion", "Mark this suggestion as reviewed?"),
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
                    t("admin.confirm.deleteCenter", "Delete \"{name}\" and its related center data?", { name: centerName }),
                    async () => {
                        if (state.editingCenterId === id) {
                            resetCenterForm();
                        }

                        return await window.NoorLocatorApi.deleteAdminCenter(id);
                    });
            });
        });

        usersTableContainer.querySelectorAll("[data-admin-user-edit]").forEach(button => {
            button.addEventListener("click", async () => {
                await openAdminUserEditor(Number(button.getAttribute("data-admin-user-edit")));
            });
        });

        usersTableContainer.querySelectorAll("[data-admin-user-delete]").forEach(button => {
            button.addEventListener("click", async () => {
                await deleteAdminUserFromList(
                    Number(button.getAttribute("data-admin-user-delete")),
                    button.getAttribute("data-admin-user-name") || "");
            });
        });

        managerAssignmentsTableContainer.querySelectorAll("[data-admin-manager-assignment-open-user]").forEach(button => {
            button.addEventListener("click", async () => {
                await openAdminUserEditor(Number(button.getAttribute("data-admin-manager-assignment-open-user")));
            });
        });

        managerAssignmentsTableContainer.querySelectorAll("[data-admin-manager-assignment-edit]").forEach(button => {
            button.addEventListener("click", () => {
                const assignment = state.managerAssignments.find(currentAssignment => currentAssignment.id === Number(button.getAttribute("data-admin-manager-assignment-edit")));
                if (!assignment) {
                    return;
                }

                populateManagerAssignmentForm(assignment);
                document.getElementById("manager-assignments")?.scrollIntoView({ behavior: "smooth", block: "start" });
            });
        });

        managerAssignmentsTableContainer.querySelectorAll("[data-admin-manager-assignment-delete]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-manager-assignment-delete"));
                const label = button.getAttribute("data-admin-manager-assignment-name") || t("admin.assignments.item", "this assignment");
                await runAdminAction(
                    t("admin.confirm.deleteAssignment", "Remove the assignment for {name}?", { name: label }),
                    async () => {
                        if (state.editingManagerAssignmentId === id) {
                            state.editingManagerAssignmentId = null;
                        }

                        return await window.NoorLocatorApi.deleteAdminManagerAssignment(id);
                    });
            });
        });

        selectedUserCentersContainer.querySelectorAll("[data-admin-managed-center-edit]").forEach(button => {
            button.addEventListener("click", () => {
                const assignment = state.managerAssignments.find(currentAssignment => currentAssignment.id === Number(button.getAttribute("data-admin-managed-center-edit")));
                if (!assignment) {
                    return;
                }

                populateManagerAssignmentForm(assignment);
                document.getElementById("manager-assignments")?.scrollIntoView({ behavior: "smooth", block: "start" });
            });
        });

        selectedUserCentersContainer.querySelectorAll("[data-admin-managed-center-remove]").forEach(button => {
            button.addEventListener("click", async () => {
                const id = Number(button.getAttribute("data-admin-managed-center-remove"));
                const centerName = button.getAttribute("data-admin-managed-center-name") || t("common.selectCenter", "center");
                await runAdminAction(
                    t("admin.confirm.deleteAssignment", "Remove the assignment for {name}?", { name: centerName }),
                    async () => {
                        if (state.editingManagerAssignmentId === id) {
                            state.editingManagerAssignmentId = null;
                        }

                        return await window.NoorLocatorApi.deleteAdminManagerAssignment(id);
                    });
            });
        });
    }

    centerForm.addEventListener("submit", async event => {
        event.preventDefault();

        if (!state.editingCenterId) {
            setMessage(centerFormMessage, t("admin.centerEditor.selectBeforeSave", "Choose a center from the list before saving changes."), "error");
            return;
        }

        setSubmitButtonState(centerForm, true, t("actions.saveChanges", "Saving changes..."));
        setMessage(centerFormMessage, t("admin.centerEditor.saving", "Saving center changes..."));

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
            setSubmitButtonState(centerForm, false, t("actions.saveChanges", "Saving changes..."));
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

    userSearchInput.addEventListener("input", event => {
        state.userSearch = String(event.target.value || "");
        renderAllSections({
            preserveUserFormMessage: true,
            preserveAssignmentFormMessage: true
        });
    });

    userRoleFilterSelect.addEventListener("change", event => {
        state.userRoleFilter = String(event.target.value || "");
        renderAllSections({
            preserveUserFormMessage: true,
            preserveAssignmentFormMessage: true
        });
    });

    userVerificationFilterSelect.addEventListener("change", event => {
        state.userVerificationFilter = String(event.target.value || "");
        renderAllSections({
            preserveUserFormMessage: true,
            preserveAssignmentFormMessage: true
        });
    });

    usersRefreshButton.addEventListener("click", async () => {
        await loadAdminWorkspace();
    });

    userEditorCloseButtons.forEach(button => {
        button.addEventListener("click", () => {
            closeAdminUserEditorDrawer();
        });
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && isAdminUserEditorOpen()) {
            closeAdminUserEditorDrawer();
        }
    });

    userForm.addEventListener("submit", async event => {
        event.preventDefault();

        if (!state.selectedUserId) {
            setMessage(userFormMessage, t("admin.users.selectPrompt", "Choose a user from the list to load editable account details."), "error");
            return;
        }

        setSubmitButtonState(userForm, true, t("actions.saveChanges", "Saving changes..."));
        setMessage(userFormMessage, t("admin.users.saving", "Saving user changes..."));

        const values = getTrimmedFormValues(userForm);
        const payload = {
            name: values.name,
            email: values.email,
            role: values.role,
            preferredLanguageCode: values.preferredLanguageCode
        };

        try {
            const response = await window.NoorLocatorApi.updateAdminUser(state.selectedUserId, payload);
            await loadAdminWorkspace();
            if (state.selectedUserId) {
                await selectAdminUser(state.selectedUserId, { scroll: false });
            }
            setMessage(userFormMessage, response.message || "User updated successfully.", "success");
            setMessage(pageMessage, response.message || "User updated successfully.", "success");
            showToast(response.message || "User updated successfully.", "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "User changes could not be saved.");
            setMessage(userFormMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(userForm, false, t("actions.saveChanges", "Saving changes..."));
        }
    });

    userDeleteButton.addEventListener("click", async () => {
        if (!state.selectedUser) {
            setMessage(userFormMessage, t("admin.users.selectPrompt", "Choose a user from the list to load editable account details."), "error");
            return;
        }

        await deleteAdminUserFromList(state.selectedUser.id, state.selectedUser.name);
    });

    managerAssignmentForm.addEventListener("submit", async event => {
        event.preventDefault();

        const values = getTrimmedFormValues(managerAssignmentForm);
        const payload = {
            userId: Number(values.userId),
            centerId: Number(values.centerId)
        };

        if (!Number.isInteger(payload.userId) || payload.userId <= 0 || !Number.isInteger(payload.centerId) || payload.centerId <= 0) {
            setMessage(managerAssignmentFormMessage, t("admin.assignments.validation", "Choose both a user and a center to continue."), "error");
            return;
        }

        setSubmitButtonState(managerAssignmentForm, true, t("admin.assignments.saving", "Saving assignment..."));
        setMessage(managerAssignmentFormMessage, t("admin.assignments.saving", "Saving assignment..."));

        try {
            const response = state.editingManagerAssignmentId
                ? await window.NoorLocatorApi.updateAdminManagerAssignment(state.editingManagerAssignmentId, payload)
                : await window.NoorLocatorApi.createAdminManagerAssignment(payload);

            await loadAdminWorkspace();
            state.editingManagerAssignmentId = null;

            if (state.selectedUserId) {
                await selectAdminUser(state.selectedUserId, { scroll: false });
            }

            setMessage(managerAssignmentFormMessage, response.message || "Manager assignment saved successfully.", "success");
            setMessage(pageMessage, response.message || "Manager assignment saved successfully.", "success");
            showToast(response.message || "Manager assignment saved successfully.", "success");
        } catch (error) {
            const message = normalizeErrorMessage(error, "Manager assignment changes could not be saved.");
            setMessage(managerAssignmentFormMessage, message, "error");
            showToast(message, "error");
        } finally {
            setSubmitButtonState(managerAssignmentForm, false, t("admin.assignments.saving", "Saving assignment..."));
        }
    });

    managerAssignmentCancelButton.addEventListener("click", () => {
        resetManagerAssignmentForm();
    });

    ensureAdminApiSurface()
        .then(isReady => {
            if (!isReady) {
                return;
            }

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
                managerAssignmentsTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
                auditLogsTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
                showToast(message, "error");
            });
        })
        .catch(error => {
            const message = normalizeErrorMessage(error, "The admin workspace could not be loaded right now.");
            setMessage(pageMessage, message, "error");
            usersTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
            managerAssignmentsTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
            auditLogsTableContainer.innerHTML = `<div class="empty-state empty-state--error">${escapeHtml(message)}</div>`;
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

window.addEventListener("noorlocator:auth-changed", () => {
    syncNotificationBell();
});
