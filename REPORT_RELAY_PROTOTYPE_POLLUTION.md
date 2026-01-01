# Security Analysis: Prototype Pollution in Relay Runtime

## Target Information
* **Product:** Relay (`relay-runtime`)
* **Component:** Normalization (`RelayResponseNormalizer`)
* **Version:** Verified on latest `relay-runtime` (installed via npm)

## Analysis Details
* **Title:** Assessment of Prototype Pollution via `__proto__` in GraphQL Payloads
* **Type:** Prototype Pollution
* **Severity:** Informational (Safe)

## Summary
A security assessment was performed to determine if `relay-runtime` is vulnerable to Prototype Pollution when normalizing malicious GraphQL responses containing `__proto__` or `constructor.prototype` keys.

The analysis confirms that **Relay is NOT vulnerable** to this attack vector in its standard configuration.

## Technical Details
Relay's normalization process (`commitPayload` -> `normalizeResponse`) relies on the GraphQL Operation AST (Abstract Syntax Tree) to traverse the server response. The normalizer iterates over the *selections* defined in the client-side query (e.g., `user { id name }`) and looks up the corresponding keys in the payload.

Because `__proto__` is not a valid field in a standard GraphQL selection set, and Relay does not blindly iterate over all keys in the JSON payload (it only reads keys that match the query selections), the malicious `__proto__` key is ignored during the normalization process.

## Steps to Verify (Proof of Concept)

**Test Vector:**
1.  **Setup:** Create a Relay Environment.
2.  **Payload:** Construct a malicious JSON response:
    ```json
    {
        "user": {
            "id": "user-1",
            "name": "Normal User",
            "__proto__": { "polluted": "true" }
        }
    }
    ```
3.  **Execution:** Commit the payload using `commitPayload`.
4.  **Verification:** Check `Object.prototype.polluted`.

**Result:**
The test script (`RelayPollution.js`) confirms that `Object.prototype` remains unmodified.

```text
[-] Attack Failed: Object.prototype is clean.
    (Relay likely ignored the __proto__ key or sanitized it)
```

## Conclusion
No remediation is required. Relay's design of driving data ingestion via the Query AST acts as an effective mitigation against standard JSON Prototype Pollution attacks.
