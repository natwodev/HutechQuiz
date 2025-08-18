// Audio control with max 5 plays per audioId
// Global states
window.audioPlayCounts = window.audioPlayCounts || {};
window.audioPlayingStates = window.audioPlayingStates || {};

const MAX_PLAY_COUNT = 5;

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


