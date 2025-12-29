# Vulnerability Report: Store Cache Poisoning in Relay Runtime

## Target Information
* **Product:** Relay (`relay-runtime`)
* **Component:** Store / Normalization (`RelayRecordSource`, `RelayResponseNormalizer`)
* **Version:** Verified on latest `relay-runtime` (installed via npm)

## Vulnerability Details
* **Title:** Client-Side Data Corruption via ID Collision (Store Cache Poisoning)
* **Type:** Improper Verification of Cryptographic Signature (CWE-347) / Improper Input Validation (CWE-20) - *Conceptually closest to Client-Side Cache Poisoning*
* **Severity:** High

## Summary
The `relay-runtime` library normalizes GraphQL responses into a global client-side store using a "flat" structure keyed by the `id` field. When processing a payload, Relay merges the fields of any object it encounters into the record with the corresponding `id` in the store.

This behavior is vulnerable to **Cache Poisoning** if the application renders data from queries where the server response can contain objects with arbitrary `id`s (e.g., a "Public Post" query returning an "Author" object). An attacker can craft a payload (or exploit a stored XSS/data entry flaw on the backend) to return an object with the `id` of a victim (e.g., the current Admin user) but with malicious field values. Relay will blindly merge these malicious values into the authoritative Admin record in the store.

## Impact
*   **UI Spoofing:** An attacker can overwrite the display name, avatar, or other visible fields of the logged-in user or other critical entities, leading to effective phishing or social engineering attacks.
*   **Logic Bypass:** If the client application uses data from the Relay Store to make authorization decisions (e.g., `user.isAdmin`), an attacker could potentially escalate privileges locally by overwriting these fields via a low-privileged query.

## Steps to Reproduce (Proof of Concept)

**Pre-requisites:**
*   A Relay environment set up with `relay-runtime`.
*   A "victim" record already in the store (e.g., an Admin user).

**Attack Vector:**
1.  **Setup:** The store contains an Admin user record:
    ```json
    { "id": "User:AdminID", "name": "Admin User", "role": "admin" }
    ```
2.  **Execution:** The attacker triggers a query (e.g., fetching a Post) that returns a malicious payload. The payload includes an `author` field that claims to have the ID `User:AdminID` but provides a different name.
    ```javascript
    const maliciousPayload = {
        post: {
            id: "post-1",
            title: "Malicious Post",
            author: {
                id: "User:AdminID", // ID Collision with Admin
                name: "HACKED NAME" // Malicious Data
            }
        }
    };
    ```
3.  **Result:** Relay's `commitPayload` (or normalization process) merges the new data. The store record for `User:AdminID` now has `name: "HACKED NAME"`.

**PoC Script (`RelayCachePoisoning.js`):**
(Attached in the repository)

```javascript
// [Script Content omitted for brevity, see attached file]
```

## Remediation / Suggestion
1.  **Strict Type/Provenance Checking:** Modify `RelayResponseNormalizer` to enforce that updates to existing records only occur if the update comes from a query known to be authoritative for that type, or warn on collisions.
2.  **Server-Side Validation:** Ensure that `id`s are globally unique and that an object cannot claim an `id` that belongs to a different type or entity context without proper verification.
3.  **Immutable Fields:** Allow developers to mark certain fields (like `role` or `permissions`) as immutable in the client store unless updated by specific "viewer" queries.

## References
*   Relay Documentation: https://relay.dev/docs/en/experimental/a-guided-tour-of-relay#normalization
