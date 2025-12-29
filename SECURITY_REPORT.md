# Vulnerability Report: WebXR Render Loop Freeze (DoS) & Asset Sanitization Gaps

## 1. [High] Denial of Service (DoS) via Unsandboxed Render Loop in `immersive-web-sdk`

**Vulnerability Type:** Denial of Service (DoS) / Resource Exhaustion
**Severity:** High (Safety Hazard in VR context)
**Affected Component:** `World`, `System` (ECS Architecture)

### Summary
The `immersive-web-sdk` allows developers and users (in dynamic loading scenarios) to register custom "Systems" via `world.registerSystem()`. These systems implement an `update()` method that is executed synchronously within the main browser render loop (`requestAnimationFrame`). The SDK lacks any mechanism to sandbox, time-limit, or monitor the execution time of these user-defined systems.

A malicious entity (e.g., a creator of a "malicious world" or "prefab" loaded by a platform using this SDK) can register a system with an infinite loop or heavy computation. This immediately freezes the main thread. In a WebXR/VR context, this is critical because it freezes the user's view (preventing head tracking updates), which can cause immediate motion sickness, disorientation, and physical safety hazards (the "VR Trap").

### Steps to Reproduce

1.  **Setup:** Build the SDK and serve `WebXR_DoS.html` from the root.
2.  **Attacker Action:** The PoC registers a custom system with a blocking `while` loop in its `update()` function.
3.  **Result:** Upon the next frame, the browser tab freezes completely for the duration of the loop (5 seconds in the PoC, or indefinitely in a real attack).

### Impact
*   **VR Safety Hazard:** Users trapped in a frozen view may experience nausea and loss of balance.
*   **Denial of Service:** The application becomes completely unresponsive requiring a browser force-quit.

### Recommendation
*   **Sandboxing:** Execute untrusted user scripts in a `Worker` thread or a sandboxed environment (e.g., QuickJS via Wasm) to decouple them from the main render loop.
*   **Watchdog Timer:** Implement a mechanism to measure the execution time of each system's `update()` method. If a system exceeds a budget (e.g., 5ms), automatically unregister it and warn the user.

---

## 2. [Low/Informational] Lack of SVG Sanitization in `AssetLoader`

**Vulnerability Type:** Improper Neutralization of Input During Web Page Generation (XSS)
**Severity:** Low (Defense-in-Depth Issue)
**Affected Component:** `TextureAssetLoader`, `AssetManager`

### Summary
The `AssetLoader` component blindly loads SVG files as textures. While modern browsers prevent script execution within `<img>` tags or WebGL textures sourced from SVGs (mitigating direct XSS), the SDK does not perform any sanitization of the SVG data.

If a developer using this SDK extracts the texture data and renders it into a DOM overlay (common in WebXR for UI panels) or uses a vulnerable older browser/webview, the embedded scripts in the SVG will execute.

### Proof of Concept
A crafted SVG containing `<script>alert(1)</script>` is loaded successfully by `TextureAssetLoader` (see `WebXR_XSS.html`). While standard browsers block the alert, the lack of sanitization allows the payload to persist in the application's memory and potentially be executed if the data is mishandled later.

### Recommendation
*   **Sanitize SVGs:** Use a library like DOMPurify to strip `<script>`, `on*` attributes, and other active content from SVG files before processing them.

---

## Meta
*   **Reported By:** Jules (AI Security Researcher)
*   **Date:** 2024-12-29
