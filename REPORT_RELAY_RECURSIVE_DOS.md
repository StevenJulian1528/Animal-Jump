# Vulnerability Report: Denial of Service via Recursive Fragments

## Target Information
* **Product:** Relay (`relay-runtime`)
* **Component:** Normalization (`RelayResponseNormalizer`)
* **Version:** Verified on latest `relay-runtime` (installed via npm)

## Vulnerability Details
* **Title:** Denial of Service (DoS) via Infinite Recursion in Normalization
* **Type:** Resource Exhaustion / Stack Overflow
* **Severity:** Medium (Availability)

## Summary
The `relay-runtime` library's normalization process (`_traverseSelections`) recursively processes GraphQL selections. If a GraphQL query contains a recursive fragment structure (e.g., `User` selecting `bestFriend` which recursively selects `User`) AND the server payload contains a cyclic graph (e.g., User A -> User B -> User A), Relay will enter an infinite recursion loop until the call stack is exceeded.

While Relay's Compiler typically prevents recursive fragments during build time, an attacker who can construct arbitrary operation descriptors (e.g., via dynamic query generation or by bypassing the compiler) or serve a cyclic payload to a vulnerable client-side query structure can crash the client.

## Impact
*   **Client Crash:** The browser tab or Node.js process executing the normalization will crash due to `RangeError: Maximum call stack size exceeded`.
*   **Denial of Service:** The application becomes unresponsive.

## Steps to Reproduce (Proof of Concept)

**Pre-requisites:**
*   A Relay environment.
*   Ability to define a recursive `OperationDescriptor` (bypassing Relay Compiler).

**Attack Vector:**
1.  **Query:** A query that selects a field which points back to the parent selection set recursively.
    ```javascript
    // Recursive Selection Structure
    const userSelections = [ { name: 'id' } ];
    const friendSelection = { name: 'bestFriend', selections: userSelections };
    userSelections.push(friendSelection); // Loop
    ```
2.  **Payload:** A cyclic JSON payload.
    ```javascript
    const userA = { id: 'A' };
    const userB = { id: 'B' };
    userA.bestFriend = userB;
    userB.bestFriend = userA;
    ```
3.  **Execution:** `commitPayload(environment, operation, { user: userA })`.

**PoC Script (`RelayRecursiveDoS.js`):**
(Attached in the repository)

**Result:**
The script outputs:
```
RelayObservable: Unhandled Error RangeError: Maximum call stack size exceeded
```

## Remediation / Suggestion
1.  **Cycle Detection:** Implement a `visited` set in `RelayResponseNormalizer._traverseSelections` to track the path of (Record ID, Selection Pointer) tuples and detect cycles.
2.  **Depth Limit:** Enforce a maximum recursion depth in the normalizer to prevent stack overflows.
