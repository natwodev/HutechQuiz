// Anti-copy script: blocks context menu, common copy-related shortcuts, and copy events
window.antiCopy = window.antiCopy || {
    enable: function () {
        // Block right-click context menu
        document.addEventListener('contextmenu', function (e) {
            e.preventDefault();
        });

        // Block common key combinations: Ctrl/Cmd + (C, U, S, X) and PrintScreen
        document.addEventListener('keydown', function (e) {
            const key = (e.key || '').toLowerCase();
            const isModifier = e.ctrlKey || e.metaKey; // Support macOS Command key
            if ((isModifier && ['c', 'u', 's', 'x'].includes(key)) || key === 'printscreen') {
                e.preventDefault();
            }
        });

        // Block copy event
        document.addEventListener('copy', function (e) {
            e.preventDefault();
            alert('Không được phép sao chép nội dung!');
        });
    }
};

// Auto-enable on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        window.antiCopy.enable();
    });
} else {
    window.antiCopy.enable();
}


