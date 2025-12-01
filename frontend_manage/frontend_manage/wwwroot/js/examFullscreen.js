window.examFullscreen = (function () {
    const state = {
        targetSelector: null,
        dotNetRef: null
    };

    function resolveTarget(selector) {
        if (!selector) {
            return document.documentElement;
        }

        return document.querySelector(selector) || document.documentElement;
    }

    async function enter(selector) {
        const element = resolveTarget(selector);
        if (!element) {
            return false;
        }

        if (document.fullscreenElement === element) {
            return true;
        }

        try {
            if (element.requestFullscreen) {
                await element.requestFullscreen();
                state.targetSelector = selector;
                return true;
            }
        } catch (err) {
            console.warn('Unable to enter fullscreen:', err);
        }

        return false;
    }

    function exit() {
        if (document.fullscreenElement) {
            return document.exitFullscreen();
        }

        return Promise.resolve();
    }

    function isActive() {
        return !!document.fullscreenElement;
    }

    function registerExamEvents(dotNetRef) {
        state.dotNetRef = dotNetRef;
    }

    function unregisterExamEvents() {
        state.dotNetRef = null;
    }

    document.addEventListener('fullscreenchange', () => {
        if (!document.fullscreenElement) {
            state.targetSelector = null;
        }

        if (state.dotNetRef) {
            state.dotNetRef.invokeMethodAsync('OnFullscreenStateChanged', !!document.fullscreenElement);
        }
    });

    document.addEventListener('visibilitychange', () => {
        if (state.dotNetRef) {
            state.dotNetRef.invokeMethodAsync('OnVisibilityChanged', document.hidden);
        }
    });

    return {
        enter,
        exit,
        isActive,
        registerExamEvents,
        unregisterExamEvents
    };
})();

