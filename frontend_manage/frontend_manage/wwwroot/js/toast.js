// Custom Toast Notification System
window.toast = (function() {
    'use strict';

    const TOAST_CONTAINER_ID = 'toast-container';
    const DEFAULT_DURATION = 4000; // 4 seconds
    const TOAST_TYPES = {
        SUCCESS: 'success',
        ERROR: 'error',
        WARNING: 'warning',
        INFO: 'info'
    };

    // Create toast container if not exists
    function ensureContainer() {
        let container = document.getElementById(TOAST_CONTAINER_ID);
        if (!container) {
            container = document.createElement('div');
            container.id = TOAST_CONTAINER_ID;
            container.className = 'toast-container';
            document.body.appendChild(container);
        }
        return container;
    }

    // Create toast element
    function createToast(message, type, duration) {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'polite');

        // Icon based on type
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };

        // Toast content
        toast.innerHTML = `
            <div class="toast-icon">${icons[type] || icons.info}</div>
            <div class="toast-message">${escapeHtml(message)}</div>
            <button class="toast-close" aria-label="Close">&times;</button>
        `;

        // Close button handler
        const closeBtn = toast.querySelector('.toast-close');
        closeBtn.addEventListener('click', () => {
            removeToast(toast);
        });

        // Auto remove after duration
        if (duration > 0) {
            setTimeout(() => {
                removeToast(toast);
            }, duration);
        }

        return toast;
    }

    // Remove toast with animation
    function removeToast(toast) {
        if (!toast || !toast.parentNode) return;
        
        toast.classList.add('toast-removing');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300); // Match CSS transition duration
    }

    // Escape HTML to prevent XSS
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Show toast
    function show(message, type, duration = DEFAULT_DURATION) {
        const container = ensureContainer();
        const toast = createToast(message, type, duration);
        
        container.appendChild(toast);
        
        // Trigger animation
        requestAnimationFrame(() => {
            toast.classList.add('toast-show');
        });

        return toast;
    }

    // Public API
    const api = {
        success: function(message, duration) {
            return show(message, TOAST_TYPES.SUCCESS, duration);
        },
        error: function(message, duration) {
            return show(message, TOAST_TYPES.ERROR, duration);
        },
        warning: function(message, duration) {
            return show(message, TOAST_TYPES.WARNING, duration);
        },
        info: function(message, duration) {
            return show(message, TOAST_TYPES.INFO, duration);
        },
        show: function(message, type, duration) {
            return show(message, type, duration);
        },
        clear: function() {
            const container = document.getElementById(TOAST_CONTAINER_ID);
            if (container) {
                const toasts = container.querySelectorAll('.toast');
                toasts.forEach(toast => removeToast(toast));
            }
        }
    };
    
    return api;
})();

