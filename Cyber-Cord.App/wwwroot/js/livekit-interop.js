livekitInterop = (() => {
    let room = null;

    async function connect(serverUrl, token, dotnetRef) {
        try {
            room = new LivekitClient.Room({
                audioCaptureDefaults: { echoCancellation: true, noiseSuppression: true },
            });

            // -- Event: another participant's track becomes available
            room.on(LivekitClient.RoomEvent.TrackSubscribed, (track, _pub, participant) => {
                if (track.kind === LivekitClient.Track.Kind.Audio) {
                    track.attach(); // audio attaches to a hidden element automatically
                    dotnetRef.invokeMethodAsync('OnParticipantJoined', participant.identity, participant.name);
                }
            });

            // -- Event: participant's track removed
            room.on(LivekitClient.RoomEvent.TrackUnsubscribed, (track, _pub, participant) => {
                track.detach();
                dotnetRef.invokeMethodAsync('OnParticipantLeft', participant.identity);
            });

            // -- Event: someone joined
            room.on(LivekitClient.RoomEvent.ParticipantConnected, (participant) => {
                dotnetRef.invokeMethodAsync('OnParticipantJoined', participant.identity, participant.name);
            });

            // -- Event: someone left
            room.on(LivekitClient.RoomEvent.ParticipantDisconnected, (participant) => {
                dotnetRef.invokeMethodAsync('OnParticipantLeft', participant.identity);
            });

            // -- Event: speaking state changed
            room.on(LivekitClient.RoomEvent.ActiveSpeakersChanged, (speakers) => {
                const ids = speakers.map(s => s.identity);
                dotnetRef.invokeMethodAsync('OnActiveSpeakersChanged', ids);
            });

            // -- Event: mute state changed
            room.on(LivekitClient.RoomEvent.TrackMuted, (_pub, participant) => {
                dotnetRef.invokeMethodAsync('OnParticipantMuteChanged', participant.identity, true);
            });
            room.on(LivekitClient.RoomEvent.TrackUnmuted, (_pub, participant) => {
                dotnetRef.invokeMethodAsync('OnParticipantMuteChanged', participant.identity, false);
            });

            // -- Event: disconnected
            room.on(LivekitClient.RoomEvent.Disconnected, () => {
                dotnetRef.invokeMethodAsync('OnDisconnected');
            });

            room.on(LivekitClient.RoomEvent.TrackSubscribed, (track, _pub, participant) => {
                if (track.kind === LivekitClient.Track.Kind.Audio) {
                    track.attach();
                }
                if (track.kind === LivekitClient.Track.Kind.Video) {
                    const container = document.getElementById(`participant-${participant.identity}`);
                    if (container) {
                        const el = track.attach();
                        el.style.width = '100%';
                        el.style.borderRadius = '8px';
                        container.appendChild(el);
                    }
                }
                dotnetRef.invokeMethodAsync('OnParticipantJoined', participant.identity, participant.name);
            });

            await room.connect(serverUrl, token);
            await room.localParticipant.setMicrophoneEnabled(true);

            // Return existing participants so Blazor can populate the list
            const participants = [];
            room.remoteParticipants.forEach((p) => {
                participants.push({ identity: p.identity, name: p.name, isMuted: false });
            });
            return { success: true, participants };

        } catch (e) {
            console.error('LiveKit connect error:', e);
            return { success: false, error: e.message };
        }
    }

    async function disconnect() {
        if (room) {
            await room.disconnect();
            room = null;
        }
    }

    async function setMuted(muted) {
        if (room) {
            await room.localParticipant.setMicrophoneEnabled(!muted);
        }
    }

    function isConnected() {
        return room !== null && room.state === LivekitClient.ConnectionState.Connected;
    }

    async function setCameraEnabled(enabled) {
        if (room) {
            await room.localParticipant.setCameraEnabled(enabled);
        }
    }

    return { connect, disconnect, setMuted, setCameraEnabled, isConnected };
})();