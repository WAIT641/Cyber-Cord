// Audio context and cache for better performance
const audioCache = new Map();


window.isSoundSupported = function () {
    return typeof Audio !== 'undefined';
};


window.playNotificationSound = function (soundUrl) {
    if (!soundUrl) return;

    try {
        let audio = audioCache.get(soundUrl);

        if (!audio) {
            audio = new Audio(soundUrl);
            audio.preload = 'auto';
            audioCache.set(soundUrl, audio);
        }
        
        const audioClone = audio.cloneNode();
        audioClone.volume = 0.5;
        audioClone.play().catch(err => {
            console.warn('Failed to play notification sound:', err);
        });
    } catch (error) {
        console.error('Error playing sound:', error);
    }
};


window.preloadNotificationSounds = function (soundUrls) {
    if (!Array.isArray(soundUrls)) return;

    soundUrls.forEach(url => {
        if (!audioCache.has(url)) {
            const audio = new Audio(url);
            audio.preload = 'auto';
            audioCache.set(url, audio);
        }
    });
};


window.clearAudioCache = function () {
    audioCache.clear();
};