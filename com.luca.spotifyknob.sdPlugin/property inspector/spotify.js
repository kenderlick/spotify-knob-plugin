let $dom = {
    main: $('#main'),
    volumeStepInput: $('#volumeStepInput'),
    volumeStepValue: $('#volumeStepValue'),
    openIfClosedInput: $('#openIfClosedInput'),
    showVolumeFeedbackInput: $('#showVolumeFeedbackInput'),
    volumeModeSelect: $('#volumeModeSelect'),
    statusDot: $('#statusDot'),
    statusText: $('#statusText'),
    btnPlayPause: $('#btnPlayPause'),
    btnOpenSpotify: $('#btnOpenSpotify'),
    btnVolDown: $('#btnVolDown'),
    btnVolUp: $('#btnVolUp'),
    btnPrevTrack: $('#btnPrevTrack'),
    btnNextTrack: $('#btnNextTrack'),
    btnToggleTrackMode: $('#btnToggleTrackMode'),
};

let $propEvent = {
    didReceiveSettings: function (payload) {
        initUI(payload.settings);
    },
    sendToPropertyInspector: function (payload) {
        if (payload.spotifyRunning !== undefined) {
            updateSpotifyStatus(payload.spotifyRunning);
        }
    }
};

function initUI(settings) {
    if (!settings) settings = {};

    const step = settings.volumeStep !== undefined ? settings.volumeStep : 5;
    $dom.volumeStepInput.value = step;
    $dom.volumeStepValue.innerText = step + '%';

    $dom.openIfClosedInput.checked = settings.openIfClosed !== undefined ? settings.openIfClosed : true;
    $dom.showVolumeFeedbackInput.checked = settings.showVolumeFeedback !== undefined ? settings.showVolumeFeedback : true;
    $dom.volumeModeSelect.value = settings.volumeMode || 'app';
}

function updateSpotifyStatus(isRunning) {
    if (isRunning) {
        $dom.statusDot.className = 'status-dot online';
        $dom.statusText.innerText = 'Spotify Connesso (In esecuzione)';
    } else {
        $dom.statusDot.className = 'status-dot';
        $dom.statusText.innerText = 'Spotify Non in esecuzione';
    }
}

// Event Listeners for UI changes
$dom.volumeStepInput.addEventListener('input', () => {
    const val = parseInt($dom.volumeStepInput.value);
    $dom.volumeStepValue.innerText = val + '%';
    if ($settings) {
        $settings.volumeStep = val;
    }
});

$dom.openIfClosedInput.addEventListener('change', () => {
    if ($settings) {
        $settings.openIfClosed = $dom.openIfClosedInput.checked;
    }
});

$dom.showVolumeFeedbackInput.addEventListener('change', () => {
    if ($settings) {
        $settings.showVolumeFeedback = $dom.showVolumeFeedbackInput.checked;
    }
});

$dom.volumeModeSelect.addEventListener('change', () => {
    if ($settings) {
        $settings.volumeMode = $dom.volumeModeSelect.value;
    }
});

// Quick action buttons
$dom.btnPlayPause.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'playPause' });
    }
});

$dom.btnOpenSpotify.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'openSpotify' });
    }
});

$dom.btnVolDown.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'volDown' });
    }
});

$dom.btnVolUp.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'volUp' });
    }
});

$dom.btnPrevTrack.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'prevTrack' });
    }
});

$dom.btnNextTrack.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'nextTrack' });
    }
});

$dom.btnToggleTrackMode.addEventListener('click', () => {
    if ($websocket) {
        $websocket.sendToPlugin({ command: 'toggleTrackMode' });
    }
});
