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

    // Copy event
    document.addEventListener('copy', (e) => {
        if (state.dotNetRef) {
            state.dotNetRef.invokeMethodAsync('OnCopyDetected');
        }
    });

    // Paste event
    document.addEventListener('paste', (e) => {
        if (state.dotNetRef) {
            state.dotNetRef.invokeMethodAsync('OnPasteDetected');
        }
    });

    // Right click event
    document.addEventListener('contextmenu', (e) => {
        if (state.dotNetRef) {
            state.dotNetRef.invokeMethodAsync('OnRightClickDetected');
        }
    });

    // DevTools detection (check periodically)
    let devToolsOpen = false;
    function detectDevTools() {
        const widthThreshold = window.outerWidth - window.innerWidth > 160;
        const heightThreshold = window.outerHeight - window.innerHeight > 160;
        
        if ((widthThreshold || heightThreshold) && !devToolsOpen) {
            devToolsOpen = true;
            if (state.dotNetRef) {
                state.dotNetRef.invokeMethodAsync('OnDevToolsDetected');
            }
        } else if (!widthThreshold && !heightThreshold && devToolsOpen) {
            devToolsOpen = false;
        }
    }

    // Check DevTools every 500ms
    setInterval(detectDevTools, 500);

    // Screenshot detection (for some browsers)
    document.addEventListener('keydown', (e) => {
        // Detect PrintScreen key
        if (e.key === 'PrintScreen' || (e.ctrlKey && e.shiftKey && e.key === 'S')) {
            if (state.dotNetRef) {
                state.dotNetRef.invokeMethodAsync('OnScreenshotDetected', e.key);
            }
        }
    });

    // Force submit exam (called when violation count reaches max)
    function forceSubmitExam() {
        console.log('[examFullscreen] ⚠️ Force submitting exam due to violations...');
        
        // Try multiple ways to find the submit button
        let submitButton = null;
        
        // Method 1: Find by text content (most reliable)
        const buttons = document.querySelectorAll('button');
        for (let btn of buttons) {
            const text = btn.textContent || btn.innerText || '';
            if (text.includes('Nộp bài') || text.includes('Nộp')) {
                submitButton = btn;
                console.log('[examFullscreen] Found submit button by text:', btn);
                break;
            }
        }
        
        // Method 2: Find in exam-footer-right (specific location)
        if (!submitButton) {
            const footerRight = document.querySelector('.exam-footer-right');
            if (footerRight) {
                submitButton = footerRight.querySelector('button');
                if (submitButton) {
                    console.log('[examFullscreen] Found submit button in footer-right:', submitButton);
                }
            }
        }
        
        // Method 3: Find in exam-footer (fallback)
        if (!submitButton) {
            const footer = document.querySelector('.exam-footer');
            if (footer) {
                const footerButtons = footer.querySelectorAll('button');
                // Last button in footer is usually submit
                if (footerButtons.length > 0) {
                    submitButton = footerButtons[footerButtons.length - 1];
                    console.log('[examFullscreen] Found submit button in footer (last):', submitButton);
                }
            }
        }
        
        if (submitButton) {
            console.log('[examFullscreen] ✅ Clicking submit button...', submitButton);
            // Remove disabled attribute if exists
            submitButton.removeAttribute('disabled');
            submitButton.disabled = false;
            
            // Try multiple ways to trigger click
            try {
                submitButton.click();
            } catch (e) {
                console.warn('[examFullscreen] Click failed, trying dispatchEvent:', e);
                const clickEvent = new MouseEvent('click', {
                    bubbles: true,
                    cancelable: true,
                    view: window
                });
                submitButton.dispatchEvent(clickEvent);
            }
            
            // Also try calling DotNet as backup
            setTimeout(() => {
                if (state.dotNetRef) {
                    console.log('[examFullscreen] Also calling DotNet ForceSubmitExam as backup...');
                    state.dotNetRef.invokeMethodAsync('ForceSubmitExam').catch(err => {
                        console.error('[examFullscreen] DotNet call failed:', err);
                    });
                }
            }, 100);
            
            return true;
        }
        
        // If button not found, try to trigger via DotNet
        if (state.dotNetRef) {
            console.log('[examFullscreen] ⚠️ Button not found, triggering submit via DotNet...');
            state.dotNetRef.invokeMethodAsync('ForceSubmitExam').catch(err => {
                console.error('[examFullscreen] ❌ Error calling ForceSubmitExam:', err);
            });
            return true;
        }
        
        console.error('[examFullscreen] ❌ Could not find submit button or DotNet reference');
        return false;
    }

    return {
        enter,
        exit,
        isActive,
        registerExamEvents,
        unregisterExamEvents,
        forceSubmitExam
    };
})();

