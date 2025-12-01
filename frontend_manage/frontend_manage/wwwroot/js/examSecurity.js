window.examSecurity = {
    violations: 0,
    maxViolations: 3,
    isFullscreen: false,
    violationHandler: null,
    forceSubmitHandler: null,

    init: function(violationHandler, forceSubmitHandler) {
        this.violationHandler = violationHandler;
        this.forceSubmitHandler = forceSubmitHandler;
        
        // Tự động bật fullscreen
        this.requestFullscreen();
        
        // Theo dõi fullscreen changes
        document.addEventListener('fullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('webkitfullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('mozfullscreenchange', () => this.handleFullscreenChange());
        document.addEventListener('MSFullscreenChange', () => this.handleFullscreenChange());
        
        // Theo dõi visibility change (chuyển tab/window)
        document.addEventListener('visibilitychange', () => this.handleVisibilityChange());
        
        // Ngăn chặn F11, ESC
        document.addEventListener('keydown', (e) => this.handleKeyDown(e));
        
        // Theo dõi blur/focus window
        window.addEventListener('blur', () => this.handleBlur());
    },

    requestFullscreen: function() {
        const elem = document.documentElement;
        if (elem.requestFullscreen) {
            elem.requestFullscreen().catch(err => console.warn('Fullscreen request failed:', err));
        } else if (elem.webkitRequestFullscreen) {
            elem.webkitRequestFullscreen();
        } else if (elem.mozRequestFullScreen) {
            elem.mozRequestFullScreen();
        } else if (elem.msRequestFullscreen) {
            elem.msRequestFullscreen();
        }
        this.isFullscreen = true;
    },

    isCurrentlyFullscreen: function() {
        return !!(document.fullscreenElement || document.webkitFullscreenElement || 
                  document.mozFullScreenElement || document.msFullscreenElement);
    },

    handleFullscreenChange: function() {
        const currentlyFullscreen = this.isCurrentlyFullscreen();
        if (this.isFullscreen && !currentlyFullscreen) {
            // Đã thoát fullscreen
            this.isFullscreen = false;
            this.recordViolation('Thoát chế độ toàn màn hình');
        } else if (!this.isFullscreen && currentlyFullscreen) {
            this.isFullscreen = true;
        }
    },

    handleVisibilityChange: function() {
        if (document.hidden) {
            // Tab/window bị ẩn (chuyển tab)
            // Chỉ record nếu chưa có trong 500ms để tránh duplicate với blur
            if (!this._visibilityViolationPending) {
                this._visibilityViolationPending = true;
                this.recordViolation('Chuyển tab hoặc cửa sổ khác');
                setTimeout(() => { this._visibilityViolationPending = false; }, 500);
            }
        }
    },

    handleKeyDown: function(e) {
        // Chặn F11 (fullscreen toggle)
        if (e.key === 'F11') {
            e.preventDefault();
            this.recordViolation('Nhấn phím F11');
        }
        // Chặn ESC khi đang fullscreen (để ngăn thoát)
        if (e.key === 'Escape' && this.isCurrentlyFullscreen()) {
            e.preventDefault();
            this.recordViolation('Nhấn phím ESC để thoát fullscreen');
        }
    },

    handleBlur: function() {
        // Window mất focus (chuyển sang app khác)
        if (document.hasFocus && !document.hasFocus()) {
            this.recordViolation('Chuyển sang ứng dụng khác');
        }
    },

    recordViolation: function(reason) {
        this.violations++;
        
        if (this.violationHandler) {
            this.violationHandler.invokeMethodAsync('HandleViolation', this.violations, reason, this.maxViolations - this.violations);
        }
        
        if (this.violations >= this.maxViolations) {
            if (this.forceSubmitHandler) {
                this.forceSubmitHandler.invokeMethodAsync('HandleForceSubmit');
            }
        } else {
            // Tự động yêu cầu fullscreen lại
            setTimeout(() => {
                if (!this.isCurrentlyFullscreen()) {
                    this.requestFullscreen();
                }
            }, 1000);
        }
    },

    cleanup: function() {
        // Note: Arrow functions can't be removed, but cleanup is mainly for resetting state
        this.violations = 0;
        this.violationHandler = null;
        this.forceSubmitHandler = null;
        this.isFullscreen = false;
    }
};

