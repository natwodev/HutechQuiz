// Custom Audio Player - Thay thế audio tags bằng custom controls
window.replaceAudioWithCustomControls = function() {
    document.querySelectorAll('audio.exam-audio-player').forEach(function(audio) {
        // Skip nếu đã được thay thế
        if (audio.closest('.custom-audio-player')) {
            return;
        }
        
        const audioId = audio.id || audio.getAttribute('data-audio-path');
        if (!audioId) return;
        
        const audioUrl = audio.src;
        const audioPath = audio.getAttribute('data-audio-path') || '';
        
        // Tạo custom controls HTML
        const customControls = document.createElement('div');
        customControls.className = 'custom-audio-player';
        customControls.innerHTML = `
            <audio id="${audioId}" 
                   src="${audioUrl}" 
                   data-audio-path="${audioPath}" 
                   class="exam-audio-player"
                   style="display: none;">
            </audio>
            <div class="custom-audio-controls">
                <button class="audio-play-button" data-audio-id="${audioId}" title="Play">
                    <span class="audio-icon">▶</span>
                </button>
                <div class="audio-timeline-container">
                    <input type="range" 
                           class="audio-timeline-slider" 
                           data-audio-id="${audioId}"
                           min="0" 
                           max="0" 
                           step="0.1" 
                           value="0"
                           title="Timeline" />
                </div>
                <div class="audio-time-display" data-audio-id="${audioId}">
                    <span class="current-time">0:00</span>
                    <span class="time-separator">/</span>
                    <span class="total-time">0:00</span>
                </div>
                <button class="audio-volume-button" data-audio-id="${audioId}" title="Mute">
                    <span class="audio-icon">🔊</span>
                </button>
                <div class="audio-volume-container">
                    <input type="range" 
                           class="audio-volume-slider" 
                           data-audio-id="${audioId}"
                           min="0" 
                           max="100" 
                           step="1" 
                           value="100"
                           title="Volume" />
                </div>
                <div class="custom-audio-play-count-badge" data-audio-id="${audioId}">5/5</div>
            </div>
        `;
        
        // Thay thế audio element
        const container = audio.closest('.audio-player-container');
        if (container) {
            // Nếu có container bao quanh (từ ExamRenderingService), thay thế cả container
            container.parentNode.replaceChild(customControls, container);
        } else {
            // Nếu không, chỉ thay thế thẻ audio
            audio.parentNode.replaceChild(customControls, audio);
        }
        
        // Initialize custom controls
        initializeCustomAudioControls(audioId);
    });
};

function initializeCustomAudioControls(audioId) {
    const audio = document.getElementById(audioId);
    if (!audio) return;
    
    const playButton = document.querySelector(`.audio-play-button[data-audio-id="${audioId}"]`);
    const timelineSlider = document.querySelector(`.audio-timeline-slider[data-audio-id="${audioId}"]`);
    const timeDisplay = document.querySelector(`.audio-time-display[data-audio-id="${audioId}"]`);
    const volumeButton = document.querySelector(`.audio-volume-button[data-audio-id="${audioId}"]`);
    const volumeSlider = document.querySelector(`.audio-volume-slider[data-audio-id="${audioId}"]`);
    const badge = document.querySelector(`.custom-audio-play-count-badge[data-audio-id="${audioId}"]`);
    
    if (!playButton || !timelineSlider || !timeDisplay) return;
    
    let isPlaying = false;
    let isMuted = false;
    let lastAllowedTime = 0;
    
    // Update time display
    function updateTimeDisplay() {
        const currentTime = audio.currentTime || 0;
        const duration = audio.duration || 0;
        
        const currentTimeSpan = timeDisplay.querySelector('.current-time');
        const totalTimeSpan = timeDisplay.querySelector('.total-time');
        
        if (currentTimeSpan) {
            currentTimeSpan.textContent = formatTime(currentTime);
        }
        if (totalTimeSpan) {
            totalTimeSpan.textContent = formatTime(duration);
        }
        
        // Update timeline slider
        if (!timelineSlider.matches(':active')) {
            timelineSlider.max = duration || 0;
            timelineSlider.value = currentTime;
        }
    }
    
    // Format time
    function formatTime(seconds) {
        if (isNaN(seconds) || seconds < 0) return "0:00";
        const minutes = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${minutes}:${secs.toString().padStart(2, '0')}`;
    }
    
    // Play/Pause button
    playButton.addEventListener('click', function() {
        if (isPlaying) {
            audio.pause();
        } else {
            // Check play count
            const currentCount = window.audioPlayCounts && window.audioPlayCounts[audioId] || 0;
            if (currentCount >= 5) {
                return;
            }
            
            // Increase count
            if (window.audioPlayCounts) {
                const newCount = currentCount + 1;
                window.audioPlayCounts[audioId] = newCount;
                
                // Save to localStorage if sessionId and questionId are available
                const sessionId = parseInt(audio.getAttribute('data-session-id'));
                const questionId = parseInt(audio.getAttribute('data-question-id'));
                if (sessionId && questionId && window.saveAudioPlayCount) {
                    window.saveAudioPlayCount(sessionId, questionId, audioId, newCount);
                }
                
                // Update badge
                if (badge) {
                    const remaining = 5 - newCount;
                    badge.textContent = `${remaining}/5`;
                    badge.setAttribute('data-remaining', remaining.toString());
                    
                    if (remaining <= 0) {
                        badge.style.background = '#d32f2f';
                        badge.style.boxShadow = '0 2px 8px rgba(211, 47, 47, 0.4)';
                    } else if (remaining <= 1) {
                        badge.style.background = '#f57c00';
                        badge.style.boxShadow = '0 2px 8px rgba(245, 124, 0, 0.4)';
                    } else {
                        badge.style.background = '#1976d2';
                        badge.style.boxShadow = '0 2px 8px rgba(25, 118, 210, 0.4)';
                    }
                }
            }
            
            lastAllowedTime = 0;
            audio.currentTime = 0;
            audio.play();
        }
    });
    
    // Timeline slider
    timelineSlider.addEventListener('input', function(e) {
        const newTime = parseFloat(e.target.value);
        
        // Chỉ cho phép seek về sau
        if (newTime < lastAllowedTime - 0.3) {
            timelineSlider.value = lastAllowedTime;
            return;
        }
        
        audio.currentTime = newTime;
        lastAllowedTime = newTime;
    });
    
    // Volume button
    volumeButton.addEventListener('click', function() {
        isMuted = !isMuted;
        audio.muted = isMuted;
        const icon = volumeButton.querySelector('.audio-icon');
        if (icon) {
            icon.textContent = isMuted ? '🔇' : '🔊';
        }
    });
    
    // Volume slider
    volumeSlider.addEventListener('input', function(e) {
        audio.volume = parseFloat(e.target.value) / 100;
    });
    
    // Audio events
    audio.addEventListener('play', function() {
        isPlaying = true;
        const icon = playButton.querySelector('.audio-icon');
        if (icon) {
            icon.textContent = '⏸';
        }
    });
    
    audio.addEventListener('pause', function() {
        isPlaying = false;
        const icon = playButton.querySelector('.audio-icon');
        if (icon) {
            icon.textContent = '▶';
        }
    });
    
    audio.addEventListener('loadedmetadata', function() {
        updateTimeDisplay();
    });
    
    audio.addEventListener('timeupdate', function() {
        // Chặn tua ngược
        if (audio.currentTime < lastAllowedTime - 0.3) {
            audio.currentTime = lastAllowedTime;
        } else {
            lastAllowedTime = audio.currentTime;
        }
        
        updateTimeDisplay();
    });
    
    audio.addEventListener('ended', function() {
        isPlaying = false;
        lastAllowedTime = 0;
        const icon = playButton.querySelector('.audio-icon');
        if (icon) {
            icon.textContent = '▶';
        }
        updateTimeDisplay();
    });
    
    // Chặn seeking về trước
    audio.addEventListener('seeking', function(e) {
        if (audio.currentTime < lastAllowedTime - 0.3) {
            e.preventDefault();
            audio.currentTime = lastAllowedTime;
        } else {
            lastAllowedTime = audio.currentTime;
        }
    }, { capture: true });
    
    audio.addEventListener('seeked', function(e) {
        if (audio.currentTime < lastAllowedTime - 0.3) {
            audio.currentTime = lastAllowedTime;
        } else {
            lastAllowedTime = audio.currentTime;
        }
    }, { capture: true });
    
    // Load play count
    if (window.audioPlayCounts && window.audioPlayCounts[audioId] !== undefined) {
        const currentCount = window.audioPlayCounts[audioId] || 0;
        const remaining = 5 - currentCount;
        if (badge) {
            badge.textContent = `${remaining}/5`;
            badge.setAttribute('data-remaining', remaining.toString());
            
            if (remaining <= 0) {
                badge.style.background = '#d32f2f';
                badge.style.boxShadow = '0 2px 8px rgba(211, 47, 47, 0.4)';
            } else if (remaining <= 1) {
                badge.style.background = '#f57c00';
                badge.style.boxShadow = '0 2px 8px rgba(245, 124, 0, 0.4)';
            } else {
                badge.style.background = '#1976d2';
                badge.style.boxShadow = '0 2px 8px rgba(25, 118, 210, 0.4)';
            }
        }
        
        if (remaining <= 0) {
            playButton.disabled = true;
            timelineSlider.disabled = true;
        }
    }
}

// Auto replace when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        setTimeout(window.replaceAudioWithCustomControls, 100);
    });
} else {
    setTimeout(window.replaceAudioWithCustomControls, 100);
}

// Also replace when new audio elements are added
const customAudioObserver = new MutationObserver(function(mutations) {
    mutations.forEach(function(mutation) {
        mutation.addedNodes.forEach(function(node) {
            if (node.nodeType === 1) {
                if (node.classList && node.classList.contains('exam-audio-player')) {
                    setTimeout(window.replaceAudioWithCustomControls, 50);
                } else if (node.querySelector && node.querySelector('.exam-audio-player')) {
                    setTimeout(window.replaceAudioWithCustomControls, 50);
                }
            }
        });
    });
});

customAudioObserver.observe(document.body, {
    childList: true,
    subtree: true
});

