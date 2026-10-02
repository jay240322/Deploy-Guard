// ============================================================================
// DeployGuard Client-side scripts
// 1. Theme Controller (Dark / Light mode toggle with localStorage persistence)
// 2. Automatic UTC timestamp to local browser timezone converter
// ============================================================================

document.addEventListener("DOMContentLoaded", function () {
    initThemeController();
    formatAllUtcTimestamps();
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
            // Format: "02 Oct 2026 19:38"
            const datePart = date.toLocaleDateString('en-GB', {
                day: '2-digit',
                month: 'short',
                year: 'numeric'
            });
            const timePart = date.toLocaleTimeString(undefined, {
                hour: '2-digit',
                minute: '2-digit',
                hour12: false
            });
            el.textContent = datePart + " " + timePart;
        }
    });
}
