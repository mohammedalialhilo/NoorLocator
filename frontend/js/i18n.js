window.NoorLocatorI18n = (() => {
    const storageKey = "noorlocator.language";
    const messageNamespace = "_messages";
    const selectorAttribute = "data-language-selector";
    const supportedLanguages = [
        { code: "ar", nativeName: "\u0627\u0644\u0639\u0631\u0628\u064a\u0629", englishName: "Arabic", dir: "rtl" },
        { code: "fa", nativeName: "\u0641\u0627\u0631\u0633\u06cc", englishName: "Farsi", dir: "rtl" },
        { code: "da", nativeName: "Dansk", englishName: "Danish", dir: "ltr" },
        { code: "de", nativeName: "Deutsch", englishName: "German", dir: "ltr" },
        { code: "es", nativeName: "Espa\u00f1ol", englishName: "Spanish", dir: "ltr" },
        { code: "sv", nativeName: "Svenska", englishName: "Swedish", dir: "ltr" },
        { code: "pt", nativeName: "Portugu\u00eas", englishName: "Portuguese", dir: "ltr" },
        { code: "en", nativeName: "English", englishName: "English", dir: "ltr" }
    ];
    const supportedLanguageMap = new Map(supportedLanguages.map(language => [language.code, language]));

    let initPromise = null;
    let currentLanguage = "en";
    let currentLocale = {};
    let fallbackLocale = {};

    function isSupported(code) {
        return typeof code === "string" && supportedLanguageMap.has(code.trim().toLowerCase());
    }

    function normalize(code) {
        return isSupported(code) ? code.trim().toLowerCase() : "en";
    }

    function getBrowserLanguage() {
        const candidates = Array.isArray(navigator.languages) && navigator.languages.length
            ? navigator.languages
            : [navigator.language];

        for (const candidate of candidates) {
            const normalized = String(candidate || "").trim().toLowerCase();
            if (isSupported(normalized)) {
                return normalized;
            }

            const prefix = normalized.split("-")[0];
            if (isSupported(prefix)) {
                return prefix;
            }
        }

        return "en";
    }

    function readStoredLanguage() {
        try {
            return window.localStorage.getItem(storageKey) || "";
        } catch {
            return "";
        }
    }

    function persistLanguage(code) {
        try {
            window.localStorage.setItem(storageKey, code);
        } catch {
            // Ignore storage failures.
        }
    }

    function getSessionUserPreferredLanguage() {
        const preferredLanguageCode = window.NoorLocatorAuth?.getSessionUser?.()?.preferredLanguageCode;
        return isSupported(preferredLanguageCode) ? normalize(preferredLanguageCode) : "";
    }

    function resolveInitialLanguage() {
        return normalize(
            getSessionUserPreferredLanguage()
            || readStoredLanguage()
            || getBrowserLanguage());
    }

    async function loadLocale(code) {
        const response = await fetch(`locales/${normalize(code)}.json`, { cache: "no-store" });
        if (!response.ok) {
            throw new Error(`Locale ${code} could not be loaded.`);
        }

        return response.json();
    }

    function mergeLocales(base, localized) {
        return {
            ...base,
            ...localized,
            [messageNamespace]: {
                ...(base?.[messageNamespace] || {}),
                ...(localized?.[messageNamespace] || {})
            }
        };
    }

    function interpolate(template, params = {}) {
        return String(template).replace(/\{([^}]+)\}/g, (_, key) => {
            return Object.prototype.hasOwnProperty.call(params, key)
                ? String(params[key])
                : `{${key}}`;
        });
    }

    function lookup(key) {
        if (typeof key !== "string" || !key) {
            return "";
        }

        return currentLocale[key]
            ?? fallbackLocale[key]
            ?? "";
    }

    function t(key, params = {}, fallback = "") {
        const template = lookup(key) || fallback || key;
        return interpolate(template, params);
    }

    function translateMessage(message, fallbackMessage = "") {
        const text = String(message || "");
        if (!text) {
            return fallbackMessage;
        }

        const translated = currentLocale?.[messageNamespace]?.[text]
            ?? fallbackLocale?.[messageNamespace]?.[text];

        return translated || fallbackMessage || text;
    }

    function getLocaleCode() {
        return currentLanguage;
    }

    function getDirection(code = currentLanguage) {
        return supportedLanguageMap.get(normalize(code))?.dir || "ltr";
    }

    function isRtl(code = currentLanguage) {
        return getDirection(code) === "rtl";
    }

    function applyDocumentLanguageState() {
        const html = document.documentElement;
        if (!html) {
            return;
        }

        html.lang = currentLanguage;
        html.dir = getDirection(currentLanguage);
        document.body?.classList.toggle("is-rtl", isRtl(currentLanguage));
    }

    function applyTranslationToElement(element) {
        const textKey = element.getAttribute("data-i18n");
        if (textKey) {
            element.textContent = t(textKey);
        }

        const htmlKey = element.getAttribute("data-i18n-html");
        if (htmlKey) {
            element.innerHTML = t(htmlKey);
        }

        const placeholderKey = element.getAttribute("data-i18n-placeholder");
        if (placeholderKey) {
            element.setAttribute("placeholder", t(placeholderKey));
        }

        const titleKey = element.getAttribute("data-i18n-title");
        if (titleKey) {
            element.setAttribute("title", t(titleKey));
        }

        const ariaLabelKey = element.getAttribute("data-i18n-aria-label");
        if (ariaLabelKey) {
            element.setAttribute("aria-label", t(ariaLabelKey));
        }

        const valueKey = element.getAttribute("data-i18n-value");
        if (valueKey) {
            element.setAttribute("value", t(valueKey));
        }
    }

    function applyTranslations(root = document) {
        applyDocumentLanguageState();
        root.querySelectorAll("[data-i18n], [data-i18n-html], [data-i18n-placeholder], [data-i18n-title], [data-i18n-aria-label], [data-i18n-value]")
            .forEach(applyTranslationToElement);

        root.querySelectorAll(`[${selectorAttribute}]`).forEach(select => {
            select.value = currentLanguage;
        });

        document.dispatchEvent(new CustomEvent("noorlocator:language-applied", {
            detail: {
                language: currentLanguage,
                direction: getDirection(currentLanguage)
            }
        }));
    }

    function getSupportedLanguages() {
        return supportedLanguages.map(language => ({ ...language }));
    }

    function getLanguageLabel(code, options = {}) {
        const normalized = normalize(code);
        const language = supportedLanguageMap.get(normalized);
        if (!language) {
            return normalized;
        }

        if (options.native === true) {
            return language.nativeName;
        }

        try {
            const displayNames = new Intl.DisplayNames([options.locale || currentLanguage], { type: "language" });
            return displayNames.of(normalized) || language.englishName;
        } catch {
            return language.englishName;
        }
    }

    async function useLanguage(code) {
        const normalized = normalize(code);
        fallbackLocale = await loadLocale("en");
        currentLocale = normalized === "en"
            ? fallbackLocale
            : mergeLocales(fallbackLocale, await loadLocale(normalized));
        currentLanguage = normalized;
        persistLanguage(normalized);
        applyTranslations(document);
        bindLanguageSelectors(document);
    }

    async function setLanguage(code, options = {}) {
        const normalized = normalize(code);
        const savePreference = options.savePreference !== false;
        const reload = options.reload !== false;

        if (savePreference && window.NoorLocatorAuth?.isAuthenticated?.() && window.NoorLocatorApi?.updateMyPreferredLanguage) {
            try {
                const response = await window.NoorLocatorApi.updateMyPreferredLanguage({
                    preferredLanguageCode: normalized
                });

                if (response?.data) {
                    window.NoorLocatorAuth.updateSessionUser(response.data);
                }
            } catch {
                // Keep the UI usable even if the profile preference cannot be saved right now.
            }
        }

        await useLanguage(normalized);

        if (reload) {
            window.location.reload();
        }
    }

    function bindLanguageSelectors(root = document) {
        root.querySelectorAll(`[${selectorAttribute}]`).forEach(select => {
            if (select.dataset.languageBound === "true") {
                select.value = currentLanguage;
                return;
            }

            select.dataset.languageBound = "true";
            select.value = currentLanguage;
            select.addEventListener("change", async event => {
                const target = event.currentTarget;
                if (!(target instanceof HTMLSelectElement)) {
                    return;
                }

                target.disabled = true;

                try {
                    await setLanguage(target.value, { reload: true, savePreference: true });
                } finally {
                    target.disabled = false;
                }
            });
        });
    }

    async function init() {
        if (initPromise) {
            return initPromise;
        }

        initPromise = (async () => {
            await useLanguage(resolveInitialLanguage());

            window.addEventListener("noorlocator:auth-changed", event => {
                const preferredLanguageCode = event.detail?.preferredLanguageCode;
                if (!isSupported(preferredLanguageCode)) {
                    return;
                }

                const normalized = normalize(preferredLanguageCode);
                if (normalized === currentLanguage) {
                    persistLanguage(normalized);
                    return;
                }

                persistLanguage(normalized);
            });
        })();

        return initPromise;
    }

    return {
        init,
        t,
        translateMessage,
        applyTranslations,
        bindLanguageSelectors,
        getSupportedLanguages,
        getLanguageLabel,
        getLocaleCode,
        isRtl,
        setLanguage
    };
})();
