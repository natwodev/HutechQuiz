// Debouncing for answer selection saves
window.answerDebouncer = window.answerDebouncer || {
    timers: {},
    debounceDelay: 2000, // 2 seconds

    // Debounce function for answer selection
    debounce: function (questionId, callbackRef) {
        // Clear previous timer if exists
        if (this.timers[questionId]) {
            clearTimeout(this.timers[questionId]);
        }

        // Set new timer
        this.timers[questionId] = setTimeout(async () => {
            try {
                await callbackRef.invokeMethodAsync('ExecuteSave');
            } catch (error) {
                // Error executing save
            }
            delete this.timers[questionId];
        }, this.debounceDelay);
    },

    // Force save all pending answers (e.g., on submit or time up)
    saveAllPending: async function (callbackMap) {
        const promises = [];
        Object.keys(this.timers).forEach(questionId => {
            if (this.timers[questionId]) {
                clearTimeout(this.timers[questionId]);
                if (callbackMap[questionId]) {
                    promises.push(callbackMap[questionId].invokeMethodAsync('ExecuteSave'));
                }
                delete this.timers[questionId];
            }
        });

        if (promises.length > 0) {
            await Promise.all(promises);
        }
    },

    // Check if there are pending saves
    hasPendingSaves: function () {
        return Object.keys(this.timers).length > 0;
    },

    // Get number of pending saves
    getPendingCount: function () {
        return Object.keys(this.timers).length;
    }
};


