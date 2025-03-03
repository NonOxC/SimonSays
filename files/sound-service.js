// Sound service for Simon Says game
window.soundService = {
    // Play a tone based on the tile index
    playTone: function (tileIndex) {
        const frequencies = [329.63, 261.63, 220.00, 164.81]; // E, C, A, E (different octave)
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        
        // Create oscillator
        const oscillator = audioContext.createOscillator();
        oscillator.type = 'sine';
        oscillator.frequency.value = frequencies[tileIndex];
        
        // Create gain node for volume control
        const gainNode = audioContext.createGain();
        gainNode.gain.value = 0.3;
        
        // Connect the nodes
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        // Play tone
        oscillator.start();
        
        // Stop after 300ms
        setTimeout(() => {
            oscillator.stop();
            // Close the audio context to free up resources
            setTimeout(() => {
                audioContext.close();
            }, 100);
        }, 300);
    },
    
    // Play error sound
    playError: function () {
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        
        // Create oscillator
        const oscillator = audioContext.createOscillator();
        oscillator.type = 'square';
        oscillator.frequency.value = 80;
        
        // Create gain node
        const gainNode = audioContext.createGain();
        gainNode.gain.value = 0.3;
        
        // Connect the nodes
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        // Play tone
        oscillator.start();
        
        // Stop after 500ms
        setTimeout(() => {
            oscillator.stop();
            setTimeout(() => {
                audioContext.close();
            }, 100);
        }, 500);
    }
};
