// Audio control with max 5 plays per audioId
// Global states
window.audioPlayCounts = window.audioPlayCounts || {};
window.audioPlayingStates = window.audioPlayingStates || {};

const MAX_PLAY_COUNT = 5;

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
    
    // Block F1-F12 keys that might control media
    if (event.code.startsWith('F') && event.code.length <= 3) {
        event.preventDefault();
        event.stopPropagation();
        return false;
    }
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

window.playAudioSimple = function (audioId, audioPath) {
    // Initialize count if not exists
    if (!window.audioPlayCounts[audioId]) {
        window.audioPlayCounts[audioId] = 0;
    }

    // Prevent clicking while audio is playing
    if (window.audioPlayingStates[audioId]) {
        return;
    }

    // Check play limit
    if (window.audioPlayCounts[audioId] >= MAX_PLAY_COUNT) {
        alert('Bạn đã hết lượt nghe audio này!');
        return;
    }

    // Increase count and set playing state
    window.audioPlayCounts[audioId]++;
    window.audioPlayingStates[audioId] = true;

    const audio = document.getElementById(audioId);
    if (audio) {
        // Disable seeking by resetting currentTime on seek attempt
        audio.addEventListener('seeking', function () {
            if (audio.currentTime > 0) {
                audio.currentTime = 0;
            }
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
    const button = document.querySelector(`button[onclick*="${audioId}"]`);
    if (!button) return;

    const remaining = MAX_PLAY_COUNT - (window.audioPlayCounts[audioId] || 0);
    const isPlaying = !!window.audioPlayingStates[audioId];
    const span = button.querySelector('span');

    if (!span) return;

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


