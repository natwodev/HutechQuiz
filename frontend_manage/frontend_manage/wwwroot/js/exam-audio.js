// Audio control with max 5 plays per audioId
// Global states
window.audioPlayCounts = window.audioPlayCounts || {};
window.audioPlayingStates = window.audioPlayingStates || {};
window.audioPlayCountsLoaded = window.audioPlayCountsLoaded || false;

const MAX_PLAY_COUNT = 5;

// Load audio play counts from localStorage
// Key format: audioPlayCounts_{studentExamSessionId}
window.loadAudioPlayCounts = async function(studentExamSessionId) {
    if (!studentExamSessionId) return;
    
    const storageKey = `audioPlayCounts_${studentExamSessionId}`;
    
    // Load from localStorage
    try {
        const saved = localStorage.getItem(storageKey);
        if (saved) {
            const counts = JSON.parse(saved);
            // Đảm bảo không có giá trị nào vượt quá MAX_PLAY_COUNT
            Object.keys(counts).forEach(audioId => {
                window.audioPlayCounts[audioId] = Math.min(counts[audioId], MAX_PLAY_COUNT);
            });
            
            // Update UI cho tất cả audio players
            setTimeout(() => {
                Object.keys(window.audioPlayCounts).forEach(audioId => {
                    updateAudioButton(audioId);
                });
            }, 100);
        }
    } catch (e) {
        console.warn('Error loading audio counts from localStorage:', e);
    }
    
    window.audioPlayCountsLoaded = true;
};

// Save audio play count to localStorage
// Đảm bảo không vượt quá MAX_PLAY_COUNT
window.saveAudioPlayCount = function(studentExamSessionId, questionId, audioId, playCount) {
    if (!studentExamSessionId) return;
    
    // Đảm bảo playCount không vượt quá MAX_PLAY_COUNT
    const actualCount = Math.min(playCount, MAX_PLAY_COUNT);
    window.audioPlayCounts[audioId] = actualCount;
    
    const storageKey = `audioPlayCounts_${studentExamSessionId}`;
    
    // Save to localStorage immediately
    try {
        localStorage.setItem(storageKey, JSON.stringify(window.audioPlayCounts));
    } catch (e) {
        console.warn('Error saving audio counts to localStorage:', e);
    }
};

// Block keyboard media keys (play/pause, next, previous)
document.addEventListener('keydown', function(event) {
    // Block play/pause key (Space, MediaPlayPause)
    if (event.code === 'Space' || event.code === 'MediaPlayPause') {
        event.preventDefault();
        event.stopPropagation();
        return false;
    }
    
    // Block next/previous track keys
    if (event.code === 'MediaTrackNext' || event.code === 'MediaTrackPrevious') {
        event.preventDefault();
        event.stopPropagation();
        return false;
    }
    
    // Đã loại bỏ chặn F12 để cho phép mở DevTools
});

// Block media session API
if ('mediaSession' in navigator) {
    navigator.mediaSession.setActionHandler('play', () => {});
    navigator.mediaSession.setActionHandler('pause', () => {});
    navigator.mediaSession.setActionHandler('stop', () => {});
    navigator.mediaSession.setActionHandler('seekbackward', () => {});
    navigator.mediaSession.setActionHandler('seekforward', () => {});
    navigator.mediaSession.setActionHandler('previoustrack', () => {});
    navigator.mediaSession.setActionHandler('nexttrack', () => {});
}

// Initialize audio players after DOM is ready
window.initializeAudioPlayers = function(studentExamSessionId, questionId) {
    if (!studentExamSessionId || !questionId) return;
    
    // Find all audio players and attach event listeners
    document.querySelectorAll('.exam-audio-player').forEach(function(audio) {
        const audioId = audio.id || audio.getAttribute('data-audio-path');
        if (!audioId) return;
        
        // Initialize count if not exists
        if (!window.audioPlayCounts[audioId]) {
            window.audioPlayCounts[audioId] = 0;
        }
        
        // Set data attributes
        audio.setAttribute('data-session-id', studentExamSessionId);
        audio.setAttribute('data-question-id', questionId);
        
        // Update button state
        updateAudioButton(audioId);
        
            // Attach play event listener (only once)
            if (!audio.hasAttribute('data-listener-attached')) {
                audio.setAttribute('data-listener-attached', 'true');
                
                // Lưu thời gian hiện tại để ngăn tua ngược
                let lastCurrentTime = 0;
                let isPlaying = false;
                
                // Ngăn chặn tua ngược - reset về vị trí trước đó nếu cố gắng tua ngược
                audio.addEventListener('timeupdate', function() {
                    if (!isPlaying) return;
                    
                    const currentTime = audio.currentTime;
                    // Nếu currentTime nhỏ hơn lastCurrentTime (tua ngược) hoặc nhảy quá xa về sau
                    if (currentTime < lastCurrentTime - 0.3) { // Cho phép sai số 0.3s
                        audio.currentTime = lastCurrentTime;
                    } else {
                        lastCurrentTime = currentTime;
                    }
                });
                
                // Ngăn chặn seeking - reset về vị trí trước đó nếu cố gắng seek về sau
                audio.addEventListener('seeking', function(e) {
                    if (!isPlaying) return;
                    
                    const currentTime = audio.currentTime;
                    // Nếu seek về trước (nhỏ hơn lastCurrentTime)
                    if (currentTime < lastCurrentTime - 0.3) {
                        e.preventDefault();
                        audio.currentTime = lastCurrentTime;
                    } else {
                        lastCurrentTime = currentTime;
                    }
                });
                
                // Ngăn chặn seeked event
                audio.addEventListener('seeked', function(e) {
                    if (!isPlaying) return;
                    
                    const currentTime = audio.currentTime;
                    if (currentTime < lastCurrentTime - 0.3) {
                        audio.currentTime = lastCurrentTime;
                    }
                });
                
                // Reset lastCurrentTime khi bắt đầu play
                audio.addEventListener('play', function() {
                    lastCurrentTime = 0;
                    audio.currentTime = 0;
                    isPlaying = true;
                    
                    const sessionId = parseInt(audio.getAttribute('data-session-id'));
                    const qId = parseInt(audio.getAttribute('data-question-id'));
                    
                    if (!window.audioPlayCounts[finalAudioId]) {
                        window.audioPlayCounts[finalAudioId] = 0;
                    }
                    
                    // Check play limit - đảm bảo không vượt quá 5 lần
                    const currentCount = window.audioPlayCounts[finalAudioId] || 0;
                    if (currentCount >= MAX_PLAY_COUNT) {
                        audio.pause();
                        audio.currentTime = 0;
                        isPlaying = false;
                        alert('Bạn đã hết lượt nghe audio này! (Tối đa 5 lần)');
                        updateAudioButton(finalAudioId);
                        return;
                    }
                    
                    // Increase count
                    const newCount = currentCount + 1;
                    window.audioPlayCounts[finalAudioId] = newCount;
                    window.audioPlayingStates[finalAudioId] = true;
                    
                    // Save to localStorage
                    if (sessionId && qId) {
                        window.saveAudioPlayCount(sessionId, qId, finalAudioId, newCount);
                    }
                    
                    updateAudioButton(finalAudioId);
                });
                
                audio.addEventListener('ended', function() {
                    window.audioPlayingStates[finalAudioId] = false;
                    lastCurrentTime = 0;
                    isPlaying = false;
                    updateAudioButton(finalAudioId);
                });
                
                audio.addEventListener('pause', function() {
                    window.audioPlayingStates[finalAudioId] = false;
                    isPlaying = false;
                    updateAudioButton(finalAudioId);
                });
            }
    });
};

window.playAudioSimple = function (audioId, audioPath, studentExamSessionId, questionId) {
    // Initialize count if not exists
    if (!window.audioPlayCounts[audioId]) {
        window.audioPlayCounts[audioId] = 0;
    }

    // Prevent clicking while audio is playing
    if (window.audioPlayingStates[audioId]) {
        return;
    }

    // Check play limit - đảm bảo không vượt quá 5 lần
    const currentCount = window.audioPlayCounts[audioId] || 0;
    if (currentCount >= MAX_PLAY_COUNT) {
        alert('Bạn đã hết lượt nghe audio này! (Tối đa 5 lần)');
        return;
    }

    // Increase count and set playing state
    const newCount = currentCount + 1;
    window.audioPlayCounts[audioId] = newCount;
    window.audioPlayingStates[audioId] = true;
    
    // Save to localStorage
    if (studentExamSessionId && questionId) {
        window.saveAudioPlayCount(studentExamSessionId, questionId, audioId, newCount);
    }

    const audio = document.getElementById(audioId);
    if (audio) {
        // Lưu thời gian hiện tại để ngăn tua ngược
        let lastCurrentTime = 0;
        
        // Ngăn chặn tua ngược - reset về vị trí trước đó nếu cố gắng tua ngược
        audio.addEventListener('timeupdate', function() {
            const currentTime = audio.currentTime;
            // Nếu currentTime nhỏ hơn lastCurrentTime (tua ngược) hoặc nhảy quá xa về sau
            if (currentTime < lastCurrentTime - 0.5) { // Cho phép sai số 0.5s
                audio.currentTime = lastCurrentTime;
            } else {
                lastCurrentTime = currentTime;
            }
        });
        
        // Ngăn chặn seeking - reset về vị trí trước đó nếu cố gắng seek về sau
        audio.addEventListener('seeking', function(e) {
            const currentTime = audio.currentTime;
            // Nếu seek về trước (nhỏ hơn lastCurrentTime)
            if (currentTime < lastCurrentTime - 0.5) {
                e.preventDefault();
                audio.currentTime = lastCurrentTime;
            } else {
                lastCurrentTime = currentTime;
            }
        });
        
        // Reset khi bắt đầu play
        audio.addEventListener('play', function() {
            lastCurrentTime = 0;
            audio.currentTime = 0;
        });

        // Block keyboard controls on audio element
        audio.addEventListener('keydown', function(event) {
            if (event.code === 'Space' || event.code === 'MediaPlayPause') {
                event.preventDefault();
                event.stopPropagation();
                return false;
            }
        });

        // Prevent audio from being paused by external controls
        audio.addEventListener('pause', function(event) {
            // Only allow pause if it's the end of audio or manual pause from our button
            if (!audio.ended && window.audioPlayingStates[audioId]) {
                // Resume playback if paused by external controls
                setTimeout(() => {
                    if (window.audioPlayingStates[audioId] && audio.paused) {
                        audio.play().catch(() => {
                            // If play fails, mark as stopped
                            window.audioPlayingStates[audioId] = false;
                            updateAudioButton(audioId);
                        });
                    }
                }, 100);
            }
        });

        audio.play();

        // When audio ends
        audio.onended = function () {
            window.audioPlayingStates[audioId] = false;
            updateAudioButton(audioId);
        };

        // When audio is paused
        audio.onpause = function () {
            // Treat pause as stopped to allow next click if user paused manually
            window.audioPlayingStates[audioId] = false;
            updateAudioButton(audioId);
        };
    }

    // Update button immediately
    updateAudioButton(audioId);
};

function updateAudioButton(audioId) {
    // Tìm audio element và update trạng thái
    const audio = document.getElementById(audioId);
    if (audio) {
        const currentCount = window.audioPlayCounts[audioId] || 0;
        const remaining = MAX_PLAY_COUNT - currentCount;
        
        // Disable audio nếu đã hết lượt
        if (remaining <= 0) {
            audio.disabled = true;
            audio.style.opacity = '0.5';
            audio.style.cursor = 'not-allowed';
            audio.title = 'Bạn đã hết lượt nghe audio này! (Tối đa 5 lần)';
            // Prevent play
            audio.onplay = function(e) {
                e.preventDefault();
                audio.pause();
                audio.currentTime = 0;
                alert('Bạn đã hết lượt nghe audio này! (Tối đa 5 lần)');
            };
        } else {
            audio.disabled = false;
            audio.style.opacity = '1';
            audio.style.cursor = 'pointer';
            audio.title = `Còn lại ${remaining}/5 lần nghe`;
        }
    }
    
    // Update button nếu có (cho compatibility với code cũ)
    const button = document.querySelector(`button[onclick*="${audioId}"]`);
    if (button) {
        const currentCount = window.audioPlayCounts[audioId] || 0;
        const remaining = MAX_PLAY_COUNT - currentCount;
        const isPlaying = !!window.audioPlayingStates[audioId];
        const span = button.querySelector('span');

        if (span) {
            if (remaining <= 0) {
                span.innerHTML = '🔊 Hết lượt phát (0/5)';
                button.disabled = true;
            } else if (isPlaying) {
                span.innerHTML = `🔊 Đang phát... (${remaining}/5)`;
                button.disabled = true; // Disable while playing
            } else {
                span.innerHTML = `🔊 Phát audio (${remaining}/5)`;
                button.disabled = false;
            }
        }
    }
}


