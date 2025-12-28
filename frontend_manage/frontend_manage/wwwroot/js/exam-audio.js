// Audio control with max 5 plays per audioId
// Global states
window.audioPlayCounts = window.audioPlayCounts || {};
window.audioPlayingStates = window.audioPlayingStates || {};
window.audioPlayCountsLoaded = window.audioPlayCountsLoaded || false;

const MAX_PLAY_COUNT = 5;

// MutationObserver để tự động cập nhật badge khi audio được thêm vào DOM
let audioObserver = null;
function setupAudioObserver() {
    if (audioObserver) return; // Đã setup rồi
    
    audioObserver = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            mutation.addedNodes.forEach(function(node) {
                if (node.nodeType === 1) { // Element node
                    // Kiểm tra nếu node là audio player
                    if (node.classList && node.classList.contains('exam-audio-player')) {
                        const audioId = node.id || node.getAttribute('data-audio-path');
                        if (audioId) {
                            // Initialize count if not exists
                            if (!window.audioPlayCounts[audioId]) {
                                window.audioPlayCounts[audioId] = 0;
                            }
                            setTimeout(function() {
                                updateAudioButton(audioId);
                            }, 100);
                        }
                    }
                    // Kiểm tra nếu node chứa audio players
                    const audioPlayers = node.querySelectorAll && node.querySelectorAll('.exam-audio-player');
                    if (audioPlayers && audioPlayers.length > 0) {
                        audioPlayers.forEach(function(audio) {
                            const audioId = audio.id || audio.getAttribute('data-audio-path');
                            if (audioId) {
                                // Initialize count if not exists
                                if (!window.audioPlayCounts[audioId]) {
                                    window.audioPlayCounts[audioId] = 0;
                                }
                                setTimeout(function() {
                                    updateAudioButton(audioId);
                                }, 100);
                            }
                        });
                    }
                }
            });
        });
    });
    
    // Observe changes to the document body
    audioObserver.observe(document.body, {
        childList: true,
        subtree: true
    });
}

// Setup observer khi DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', setupAudioObserver);
} else {
    setupAudioObserver();
}

// Hàm để tự động tạo badge cho tất cả audio elements hiện có
window.ensureAllAudioBadges = function() {
    document.querySelectorAll('.exam-audio-player').forEach(function(audio) {
        const audioId = audio.id || audio.getAttribute('data-audio-path');
        if (audioId) {
            // Initialize count if not exists
            if (!window.audioPlayCounts[audioId]) {
                window.audioPlayCounts[audioId] = 0;
            }
            // Update badge
            updateAudioButton(audioId);
        }
    });
};

// Tự động gọi khi DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        setTimeout(window.ensureAllAudioBadges, 100);
    });
} else {
    setTimeout(window.ensureAllAudioBadges, 100);
}

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
    
    // Use setTimeout to ensure DOM is fully rendered
    setTimeout(function() {
        // Find all audio players and attach event listeners
        const audioPlayers = document.querySelectorAll('.exam-audio-player');
        console.log('Found audio players:', audioPlayers.length);
        
        audioPlayers.forEach(function(audio) {
            const audioId = audio.id || audio.getAttribute('data-audio-path');
            if (!audioId) {
                console.warn('Audio element missing ID or data-audio-path:', audio);
                return;
            }
            
            // Initialize count if not exists
            if (!window.audioPlayCounts[audioId]) {
                window.audioPlayCounts[audioId] = 0;
            }
            
            // Set data attributes
            audio.setAttribute('data-session-id', studentExamSessionId);
            audio.setAttribute('data-question-id', questionId);
            
            // Initialize count if not exists
            if (!window.audioPlayCounts[audioId]) {
                window.audioPlayCounts[audioId] = 0;
            }
            
            // Update button state - ensure badge is created and displayed
            updateAudioButton(audioId);
            
            // Force update again after a short delay to ensure badge is visible
            setTimeout(function() {
                updateAudioButton(audioId);
            }, 50);
            
            // Attach play event listener (only once)
            if (!audio.hasAttribute('data-listener-attached')) {
                audio.setAttribute('data-listener-attached', 'true');
                
                console.log('Attaching listeners for audio:', audioId, 'Session:', studentExamSessionId, 'Question:', questionId);
                
                // Lưu thời gian hiện tại để ngăn tua ngược
                let lastCurrentTime = 0;
                let isPlaying = false;
                
                // Ngăn chặn tua ngược - chỉ khi đang phát
                audio.addEventListener('timeupdate', function() {
                    if (!isPlaying) {
                        // Khi không phát, cập nhật lastCurrentTime để cho phép seek tự do
                        lastCurrentTime = audio.currentTime;
                        return;
                    }
                    
                    const currentTime = audio.currentTime;
                    // Chỉ chặn nếu tua ngược (nhỏ hơn lastCurrentTime) - cho phép sai số 0.3s
                    if (currentTime < lastCurrentTime - 0.3) {
                        audio.currentTime = lastCurrentTime;
                        console.log('Blocked timeupdate backward from', lastCurrentTime, 'to', currentTime);
                    } else {
                        // Cho phép tiến tới
                        lastCurrentTime = currentTime;
                    }
                });
                
                // Ngăn chặn seeking về trước - LUÔN chặn, không chỉ khi đang phát
                audio.addEventListener('seeking', function(e) {
                    const currentTime = audio.currentTime;
                    
                    // LUÔN chặn seek về trước (nhỏ hơn lastCurrentTime)
                    // Chỉ cho phép seek về sau (tiến tới) hoặc giữ nguyên
                    if (currentTime < lastCurrentTime - 0.3) {
                        e.preventDefault();
                        e.stopPropagation();
                        audio.currentTime = lastCurrentTime;
                        console.log('Blocked seeking backward from', lastCurrentTime, 'to', currentTime);
                        return false;
                    } else {
                        // Cho phép seek về sau hoặc giữ nguyên
                        lastCurrentTime = currentTime;
                    }
                }, { capture: true });
                
                // Ngăn chặn seeked event - LUÔN chặn seek về trước
                audio.addEventListener('seeked', function(e) {
                    const currentTime = audio.currentTime;
                    
                    // LUÔN chặn seek về trước
                    if (currentTime < lastCurrentTime - 0.3) {
                        audio.currentTime = lastCurrentTime;
                        console.log('Blocked seeked backward from', lastCurrentTime, 'to', currentTime);
                        e.preventDefault();
                        e.stopPropagation();
                        return false;
                    } else {
                        // Cho phép seek về sau hoặc giữ nguyên
                        lastCurrentTime = currentTime;
                    }
                }, { capture: true });
                
                // Reset lastCurrentTime khi bắt đầu play
                audio.addEventListener('play', function(e) {
                    console.log('Play event triggered for audio:', audioId);
                    
                    // Chặn timeline ngay khi bắt đầu play
                    const timeline = audio.querySelector ? null : null; // Audio element không có querySelector
                    // Disable timeline controls bằng cách set controlsList
                    if (!audio.hasAttribute('controlsList')) {
                        audio.setAttribute('controlsList', 'nodownload nofullscreen');
                    }
                    
                    lastCurrentTime = 0;
                    audio.currentTime = 0;
                    isPlaying = true;
                    
                    const sessionId = parseInt(audio.getAttribute('data-session-id'));
                    const qId = parseInt(audio.getAttribute('data-question-id'));
                    const finalAudioId = audioId; // Use the audioId from outer scope
                    
                    console.log('Audio play - SessionId:', sessionId, 'QuestionId:', qId, 'AudioId:', finalAudioId);
                    
                    if (!window.audioPlayCounts[finalAudioId]) {
                        window.audioPlayCounts[finalAudioId] = 0;
                    }
                    
                    // Check play limit - đảm bảo không vượt quá 5 lần
                    const currentCount = window.audioPlayCounts[finalAudioId] || 0;
                    console.log('Current play count:', currentCount, 'Max:', MAX_PLAY_COUNT);
                    
                    if (currentCount >= MAX_PLAY_COUNT) {
                        audio.pause();
                        audio.currentTime = 0;
                        isPlaying = false;
                        updateAudioButton(finalAudioId);
                        e.preventDefault();
                        return;
                    }
                    
                    // Increase count
                    const newCount = currentCount + 1;
                    window.audioPlayCounts[finalAudioId] = newCount;
                    window.audioPlayingStates[finalAudioId] = true;
                    
                    console.log('Increased play count to:', newCount);
                    
                    // Save to localStorage
                    if (sessionId && qId) {
                        window.saveAudioPlayCount(sessionId, qId, finalAudioId, newCount);
                        console.log('Saved play count to localStorage');
                    }
                    
                    updateAudioButton(finalAudioId);
                }, { once: false }); // Không dùng once để có thể đếm nhiều lần
                
                audio.addEventListener('ended', function() {
                    const finalAudioId = audioId; // Use the audioId from outer scope
                    window.audioPlayingStates[finalAudioId] = false;
                    lastCurrentTime = 0;
                    isPlaying = false;
                    updateAudioButton(finalAudioId);
                });
                
                audio.addEventListener('pause', function() {
                    const finalAudioId = audioId; // Use the audioId from outer scope
                    window.audioPlayingStates[finalAudioId] = false;
                    isPlaying = false;
                    updateAudioButton(finalAudioId);
                });
            }
        });
        
        // Also update all audio players globally to catch any missed ones
        setTimeout(function() {
            window.updateAllAudioPlayers();
        }, 150);
        
        // Force update lại sau 500ms để đảm bảo badge hiển thị
        setTimeout(function() {
            audioPlayers.forEach(function(audio) {
                const audioId = audio.id || audio.getAttribute('data-audio-path');
                if (audioId) {
                    updateAudioButton(audioId);
                }
            });
        }, 500);
    }, 100); // Delay 100ms to ensure DOM is ready
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
        let isPlaying = false;
        
        // Ngăn chặn tua ngược - chỉ khi đang phát
        audio.addEventListener('timeupdate', function() {
            if (!isPlaying) {
                lastCurrentTime = audio.currentTime;
                return;
            }
            
            const currentTime = audio.currentTime;
            // Chỉ chặn nếu tua ngược (nhỏ hơn lastCurrentTime) - cho phép sai số 0.5s
            if (currentTime < lastCurrentTime - 0.5) {
                audio.currentTime = lastCurrentTime;
            } else {
                lastCurrentTime = currentTime;
            }
        });
        
        // Ngăn chặn seeking về trước - chỉ khi đang phát
        audio.addEventListener('seeking', function(e) {
            if (!isPlaying) {
                lastCurrentTime = audio.currentTime;
                return;
            }
            
            const currentTime = audio.currentTime;
            // Chỉ chặn nếu seek về trước (nhỏ hơn lastCurrentTime)
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
            isPlaying = true;
        });
        
        // Cập nhật khi pause
        audio.addEventListener('pause', function() {
            isPlaying = false;
            lastCurrentTime = audio.currentTime;
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

// Helper function to update all audio players in the current question
window.updateAllAudioPlayers = function() {
    document.querySelectorAll('.exam-audio-player').forEach(function(audio) {
        const audioId = audio.id || audio.getAttribute('data-audio-path');
        if (audioId) {
            // Initialize count if not exists
            if (!window.audioPlayCounts[audioId]) {
                window.audioPlayCounts[audioId] = 0;
            }
            updateAudioButton(audioId);
        }
    });
};

// Helper function to get remaining play count for an audio
window.getAudioRemainingCount = function(audioId) {
    const currentCount = window.audioPlayCounts[audioId] || 0;
    return Math.max(0, MAX_PLAY_COUNT - currentCount);
};

window.updateAudioButton = function(audioId) {
    // Tìm audio element - thử nhiều cách
    let audio = document.getElementById(audioId);
    
    // Nếu không tìm thấy bằng ID, thử tìm bằng data-audio-path
    if (!audio) {
        audio = document.querySelector(`.exam-audio-player[data-audio-path="${audioId}"]`);
    }
    
    // Nếu vẫn không tìm thấy, thử tìm bằng ID có chứa audioId
    if (!audio && audioId) {
        const allAudios = document.querySelectorAll('.exam-audio-player');
        for (let i = 0; i < allAudios.length; i++) {
            const a = allAudios[i];
            if (a.id === audioId || a.getAttribute('data-audio-path') === audioId) {
                audio = a;
                break;
            }
        }
    }
    
    if (!audio) {
        console.warn('Audio element not found for audioId:', audioId);
        return;
    }

    // Skip nếu audio nằm trong custom-audio-player (đã có giao diện riêng)
    if (audio.closest('.custom-audio-player')) {
        return;
    }
    
    const currentCount = window.audioPlayCounts[audioId] || 0;
    const remaining = MAX_PLAY_COUNT - currentCount;
    
    // Tìm hoặc tạo container cho audio player và badge
    let audioContainer = audio.parentElement;
    
    // Kiểm tra xem audio đã nằm trong container chưa
    if (!audioContainer || !audioContainer.classList.contains('audio-player-container')) {
        // Nếu audio không có container, tìm container gần nhất hoặc tạo mới
        audioContainer = audio.closest('.audio-player-container');
        
        if (!audioContainer) {
            // Tạo container nếu chưa có
            audioContainer = document.createElement('div');
            audioContainer.className = 'audio-player-container';
            audioContainer.style.position = 'relative';
            audioContainer.style.display = 'inline-block';
            audioContainer.style.width = '100%';
            audioContainer.style.maxWidth = '100%';
            
            // Insert container before audio
            if (audio.parentNode) {
                audio.parentNode.insertBefore(audioContainer, audio);
                audioContainer.appendChild(audio);
            }
        }
    }
    
    // Tìm hoặc tạo overlay để chặn tương tác với timeline
    let overlay = audioContainer.querySelector('.audio-timeline-overlay');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.className = 'audio-timeline-overlay';
        // Insert overlay sau audio element để nó nằm trên timeline
        if (audio.nextSibling) {
            audioContainer.insertBefore(overlay, audio.nextSibling);
        } else {
            audioContainer.appendChild(overlay);
        }
        console.log('Overlay created for audio:', audioId);
    }
    
    // Đảm bảo overlay luôn hiển thị và có z-index cao
    if (overlay) {
        overlay.style.display = 'block';
        overlay.style.visibility = 'visible';
        overlay.style.opacity = '1';
    }
    
    // Tìm hoặc tạo badge hiển thị số lần nghe
    let badge = audioContainer.querySelector('.custom-audio-play-count-badge[data-audio-id="' + audioId + '"]') || 
                audioContainer.querySelector('.audio-play-count-badge[data-audio-id="' + audioId + '"]') || 
                audioContainer.querySelector('.custom-audio-play-count-badge') ||
                audioContainer.querySelector('.audio-play-count-badge');
    
    if (!badge) {
        badge = document.createElement('div');
        badge.className = 'custom-audio-play-count-badge';
        badge.setAttribute('data-audio-id', audioId);
        audioContainer.appendChild(badge);
        console.log('Badge created for audio:', audioId, 'Container:', audioContainer);
    } else {
        // Đảm bảo badge có đúng data-audio-id
        badge.setAttribute('data-audio-id', audioId);
    }
    
    // Đảm bảo badge luôn hiển thị và có text content
    if (!badge) {
        console.error('Badge not found for audio:', audioId);
        return;
    }
    
    // Disable audio nếu đã hết lượt
    if (remaining <= 0) {
        audio.disabled = true;
        audio.style.opacity = '0.5';
        audio.style.cursor = 'not-allowed';
        audio.title = 'Bạn đã hết lượt nghe audio này! (Tối đa 5 lần)';
        badge.textContent = '0/5';
        badge.setAttribute('data-remaining', '0');
        badge.style.background = '#d32f2f';
        badge.style.display = 'block';
        badge.style.visibility = 'visible';
        badge.style.opacity = '1';
        console.log('Badge updated for audio:', audioId, 'Text: 0/5');
        // Prevent play
        audio.onplay = function(e) {
            e.preventDefault();
            audio.pause();
            audio.currentTime = 0;
        };
    } else {
        audio.disabled = false;
        audio.style.opacity = '1';
        audio.style.cursor = 'pointer';
        audio.title = `Còn lại ${remaining}/5 lần nghe`;
        badge.textContent = `${remaining}/5`;
        badge.setAttribute('data-remaining', remaining.toString());
        badge.style.background = remaining <= 1 ? '#f57c00' : '#1976d2';
        badge.style.display = 'block';
        badge.style.visibility = 'visible';
        badge.style.opacity = '1';
        console.log('Badge updated for audio:', audioId, 'Text:', `${remaining}/5`);
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


