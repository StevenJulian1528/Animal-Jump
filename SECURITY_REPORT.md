# SECURITY AUDIT: Relay Runtime

## 1. Prototype Pollution
**Status:** Not Vulnerable (Verified)
**Description:** Attempts to pollute `Object.prototype` via `__proto__` or `constructor.prototype` payloads in `commitPayload` were unsuccessful. Relay's normalization logic (`RelayResponseNormalizer`) iterates over the operation's selections (AST) rather than the raw payload keys. Since `__proto__` is not a valid GraphQL selection, it is ignored during traversal.

**Proof of Concept:** `node RelayPollution.js`
**Result:** "Attack Failed: Object.prototype is clean."

## 2. Store Cache Poisoning
**Status:** **VULNERABLE (CRITICAL)**
**Description:** Relay's `RelayRecordSource` normalizes records based on the `id` field. If a payload contains an object with an `id` matching an existing record (e.g., `User:AdminID`), Relay will merge the fields from the payload into the existing record. This allows an attacker to overwrite sensitive data (like `name`, `role`, or other fields) of other users or entities in the global store by returning a malicious object with the victim's ID in a response to an unrelated query.

**Proof of Concept:** `node RelayCachePoisoning.js`
**Vector:**
1.  Store contains `User:AdminID` with `name: "Admin User"`.
2.  Attacker executes a query (e.g., fetching a Post) that includes an `author` field.
3.  The response contains `author: { id: "User:AdminID", name: "HACKED NAME" }`.
4.  Relay normalizes this and updates `User:AdminID` in the store.
5.  Any component observing `User:AdminID` now displays "HACKED NAME".

**Impact:** UI Spoofing, Phishing, Potential Logic Bypass (if store state drives authorization).

**Remediation Recommendation:**
Implement a strict type check or an "expected type" validation during normalization. If the record in the store has type `User`, and the payload (via query selection) implies it should be `User`, it matches. However, if the query context implies a `Post` author (which is a `User`), it is valid.
The issue is inherent to Global ID systems.
Mitigations:
1.  **Server-Side:** Ensure `id`s are globally unique and unguessable (e.g., UUIDs) or signed.
2.  **Client-Side (Relay):** Warn or throw when merging two records if the *provenance* or *typename* is suspicious, though `typename` is usually consistent here.
3.  **Strict Mode:** Disallow merging specific fields (like `role`) from non-authoritative queries.

**Fix Implementation (Conceptual):**
Modify `RelayResponseNormalizer.js` to validate that the record being updated matches the expected `concreteType` of the selection more strictly, or introduce a mechanism to mark certain records as "ReadOnly" or "Authoritative" that cannot be updated by partial queries.

Since we cannot modify the library source in `node_modules` within this environment, this report serves as the deliverable.
