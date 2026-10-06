// ============================================================================
// DeployGuard Client-side scripts
// 1. Theme Controller (Dark / Light mode toggle with localStorage persistence)
// 2. Automatic UTC timestamp to local browser timezone converter
// ============================================================================

document.addEventListener("DOMContentLoaded", function () {
    initThemeController();
    formatAllUtcTimestamps();
    initGlobalLoader();
});

// ----------------------------------------------------------------------------
// Theme Controller
// ----------------------------------------------------------------------------
function initThemeController() {
    const savedTheme = localStorage.getItem('deployguard-theme') || 'light';
    applyTheme(savedTheme);

    // Bind all theme toggle buttons on the page
    document.querySelectorAll('.theme-toggle-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const currentTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
            const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
            applyTheme(nextTheme);
            localStorage.setItem('deployguard-theme', nextTheme);
        });
    });
}

function applyTheme(theme) {
    document.documentElement.setAttribute('data-bs-theme', theme);

    // Update icons on all toggle buttons
    document.querySelectorAll('.theme-toggle-btn').forEach(btn => {
        const darkIcon = btn.querySelector('.theme-icon-dark');
        const lightIcon = btn.querySelector('.theme-icon-light');

        if (darkIcon && lightIcon) {
            if (theme === 'dark') {
                darkIcon.classList.add('d-none');
                lightIcon.classList.remove('d-none');
                btn.setAttribute('title', 'Switch to Light Mode');
            } else {
                darkIcon.classList.remove('d-none');
                lightIcon.classList.add('d-none');
                btn.setAttribute('title', 'Switch to Dark Mode');
            }
        }
    });
}

// ----------------------------------------------------------------------------
// UTC to Local Timezone Formatter
// ----------------------------------------------------------------------------
function formatAllUtcTimestamps() {
    document.querySelectorAll("[data-utc]").forEach(function (el) {
        const utcStr = el.getAttribute("data-utc");
        if (!utcStr) return;

        // Ensure date string ends with Z so browser interprets it strictly as UTC
        const isoString = (utcStr.endsWith("Z") || utcStr.includes("+") || (utcStr.length > 10 && utcStr.lastIndexOf("-") > 8))
            ? utcStr
            : utcStr + "Z";

        const date = new Date(isoString);
        if (isNaN(date.getTime())) return;

        const format = el.getAttribute("data-utc-format") || "datetime";

        if (format === "date") {
            // Format: "02 Oct 2026"
            el.textContent = date.toLocaleDateString('en-GB', {
                day: '2-digit',
                month: 'short',
                year: 'numeric'
            });
        } else if (format === "time") {
            // Format: "07:38 PM"
            el.textContent = date.toLocaleTimeString(undefined, {
                hour: '2-digit',
                minute: '2-digit',
                hour12: true
            });
        } else if (format === "datetime") {
            // Format: "02 Oct 2026, 07:38 PM"
            const datePart = date.toLocaleDateString('en-GB', {
                day: '2-digit',
                month: 'short',
                year: 'numeric'
            });
            const timePart = date.toLocaleTimeString(undefined, {
                hour: '2-digit',
                minute: '2-digit',
                hour12: true
            });
            el.textContent = datePart + ", " + timePart;
        } else if (format === "datetime-short") {
            // Format: "02 Oct 2026, 07:38 PM"
            const datePart = date.toLocaleDateString('en-GB', {
                day: '2-digit',
                month: 'short',
                year: 'numeric'
            });
            const timePart = date.toLocaleTimeString(undefined, {
                hour: '2-digit',
                minute: '2-digit',
                hour12: true
            });
            el.textContent = datePart + " " + timePart;
        }
    });
}

// ----------------------------------------------------------------------------
// Global Processing / Button Loader
// ----------------------------------------------------------------------------
function initGlobalLoader() {
    const overlay = document.getElementById('globalLoaderOverlay');
    const loaderText = document.getElementById('globalLoaderText');
    let showTimer = null;
    let safetyTimer = null;

    window.showLoader = function (message = "Processing request...") {
        if (!overlay) return;
        if (loaderText) loaderText.textContent = message;

        if (showTimer) clearTimeout(showTimer);
        if (safetyTimer) clearTimeout(safetyTimer);

        // Small debounce (150ms) so instant page switches feel immediate without flash,
        // while operations that take time smoothly display the loader
        showTimer = setTimeout(() => {
            overlay.classList.remove('d-none');
            void overlay.offsetWidth; // Force layout reflow
            overlay.classList.add('show');
        }, 150);

        // Auto-dismiss failsafe after 15 seconds
        safetyTimer = setTimeout(() => {
            window.hideLoader();
        }, 15000);
    };

    window.hideLoader = function () {
        if (showTimer) clearTimeout(showTimer);
        if (safetyTimer) clearTimeout(safetyTimer);
        if (!overlay) return;

        overlay.classList.remove('show');
        setTimeout(() => {
            if (!overlay.classList.contains('show')) {
                overlay.classList.add('d-none');
            }
        }, 220);
    };

    // Auto-attach to Form Submissions (Login, Register, Scans, User management, etc.)
    document.addEventListener('submit', function (e) {
        const form = e.target;
        if (!form || form.hasAttribute('data-no-loader')) return;

        // Verify HTML5 form validation before showing loader
        if (form.checkValidity && !form.checkValidity()) {
            return;
        }

        const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
        let loadingMsg = "Processing request...";

        if (submitBtn) {
            loadingMsg = submitBtn.getAttribute('data-loading-text') || "Processing...";
            if (!submitBtn.classList.contains('btn-loading-active')) {
                submitBtn.classList.add('btn-loading-active');
                submitBtn.dataset.origHtml = submitBtn.innerHTML;
                const spinner = document.createElement('span');
                spinner.className = 'spinner-border spinner-border-sm me-2';
                spinner.setAttribute('role', 'status');
                spinner.setAttribute('aria-hidden', 'true');
                submitBtn.prepend(spinner);
            }
        }

        window.showLoader(loadingMsg);
    });

    // Auto-attach to Action buttons / Navigation links that perform operations
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('a.btn, button.btn, .btn, .trigger-loader');
        if (!btn) return;

        // Skip dropdown toggles, modal triggers/dismisses, theme toggles, tabs, hash links, or blank targets
        if (btn.hasAttribute('data-bs-toggle') ||
            btn.hasAttribute('data-bs-dismiss') ||
            btn.classList.contains('theme-toggle-btn') ||
            btn.classList.contains('dropdown-toggle') ||
            btn.getAttribute('target') === '_blank' ||
            btn.getAttribute('href')?.startsWith('#') ||
            btn.getAttribute('href')?.startsWith('javascript:') ||
            (btn.type === 'button' && !btn.classList.contains('trigger-loader'))) {
            return;
        }

        // For action navigation links (like Export PDF, trigger scan from link, etc.)
        const href = btn.getAttribute('href');
        if (href && !href.startsWith('#') && !btn.closest('form')) {
            const loadingMsg = btn.getAttribute('data-loading-text') || "Loading...";
            window.showLoader(loadingMsg);
        }
    });

    // Automatically hide loader and restore buttons on browser history navigation (bfcache)
    window.addEventListener('pageshow', function () {
        window.hideLoader();
        document.querySelectorAll('.btn-loading-active').forEach(btn => {
            btn.classList.remove('btn-loading-active');
            if (btn.dataset.origHtml) {
                btn.innerHTML = btn.dataset.origHtml;
            }
        });
    });
}
