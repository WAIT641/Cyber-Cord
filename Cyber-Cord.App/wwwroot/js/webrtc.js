// wwwroot/js/webrtc.js
// ─────────────────────────────────────────────────────────────────────────────
// WebRTC module for CyberCord.
// Imported lazily by CallHandler.cs via IJSRuntime.
// ─────────────────────────────────────────────────────────────────────────────

const ICE_SERVERS = {
    iceServers: [
        { urls: "stun:stun.l.google.com:19302" },
        { urls: "stun:stun1.l.google.com:19302" },
        // Add your TURN server here for production:
        // { urls: "turn:your-server.com:3478", username: "user", credential: "pass" }
    ]
};

/** @type {RTCPeerConnection|null} */
let pc = null;

/** @type {MediaStream|null} */
let localStream = null;

/** @type {string|null} */
let targetUserId = null;

// ─── Public API ──────────────────────────────────────────────────────────────

/**
 * Start or join a call.
 * @param {DotNetObjectReference} dotnetRef   - C# CallHandler reference
 * @param {string}                peerId      - The remote user's ID
 * @param {boolean}               isInitiator - true = caller, false = callee
 */
export async function startCall(dotnetRef, peerId, isInitiator) {
    targetUserId = peerId;

    // Capture local microphone (+ optional camera)
    try {
        localStream = await navigator.mediaDevices.getUserMedia({
            audio: true,
            video: false   // flip to true for video calls
        });
    } catch (err) {
        console.error("[WebRTC] getUserMedia failed:", err);
        throw err;
    }

    // Attach local stream to preview element if present
    attachStream("localVideo", localStream);

    pc = new RTCPeerConnection(ICE_SERVERS);

    // Add local tracks to the peer connection
    localStream.getTracks().forEach(track => pc.addTrack(track, localStream));

    // When remote tracks arrive, attach to the remote video/audio element
    pc.ontrack = (event) => {
        attachStream("remoteVideo", event.streams[0]);
    };

    // Trickle ICE: send each candidate to .NET as it arrives
    pc.onicecandidate = (event) => {
        if (event.candidate) {
            dotnetRef.invokeMethodAsync(
                "OnIceCandidateAsync",
                peerId,
                JSON.stringify(event.candidate)
            );
        }
    };

    pc.onconnectionstatechange = () => {
        console.log("[WebRTC] Connection state:", pc.connectionState);
    };

    if (isInitiator) {
        // Caller creates and sends the offer
        const offer = await pc.createOffer();
        await pc.setLocalDescription(offer);

        dotnetRef.invokeMethodAsync(
            "OnOfferReadyAsync",
            peerId,
            JSON.stringify(pc.localDescription)
        );
    }
    // Callee path: receiveOffer() is called next by CallHandler
}

/**
 * Called on the callee side after startCall().
 * Sets the remote offer and returns the local answer SDP.
 * @param {string} offerSdp - Serialised RTCSessionDescription (offer)
 * @returns {Promise<string>} Serialised RTCSessionDescription (answer)
 */
export async function receiveOffer(offerSdp) {
    await pc.setRemoteDescription(JSON.parse(offerSdp));
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);
    return JSON.stringify(pc.localDescription);
}

/**
 * Called on the caller side once the callee answers.
 * @param {string} answerSdp - Serialised RTCSessionDescription (answer)
 */
export async function receiveAnswer(answerSdp) {
    await pc.setRemoteDescription(JSON.parse(answerSdp));
}

/**
 * Add a trickled ICE candidate from the remote peer.
 * @param {string} candidateJson - Serialised RTCIceCandidate
 */
export async function addIceCandidate(candidateJson) {
    try {
        await pc.addIceCandidate(JSON.parse(candidateJson));
    } catch (err) {
        console.warn("[WebRTC] addIceCandidate failed:", err);
    }
}

/**
 * Tear down the peer connection and stop all local tracks.
 */
export function endCall() {
    localStream?.getTracks().forEach(t => t.stop());
    pc?.close();

    localStream = null;
    pc = null;
    targetUserId = null;

    // Clear video elements
    clearStream("localVideo");
    clearStream("remoteVideo");
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function attachStream(elementId, stream) {
    const el = document.getElementById(elementId);
    if (el) {
        el.srcObject = stream;
        el.play().catch(() => {
            // Autoplay policy — browser will play on first user gesture
        });
    }
}

function clearStream(elementId) {
    const el = document.getElementById(elementId);
    if (el) el.srcObject = null;
}
