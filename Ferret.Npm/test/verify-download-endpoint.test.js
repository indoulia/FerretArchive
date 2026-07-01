'use strict';
const { test } = require('node:test');
const assert = require('node:assert');

// Reset the config module between cases so env-var overrides take effect.
function loadVerify() {
    delete require.cache[require.resolve('../lib/distribution-config')];
    delete require.cache[require.resolve('../scripts/verify-download-endpoint')];
    return require('../scripts/verify-download-endpoint').verifyManifest;
}

const noop = () => {};
const okRes = (body) => ({ ok: true, status: 200, json: async () => body });
const errRes = (status) => ({ ok: false, status, json: async () => ({}) });

test('verifyManifest succeeds when the manifest is reachable and the tag matches', async () => {
    const verifyManifest = loadVerify();
    let calledUrl = null;
    const res = await verifyManifest('v1.2.3', {
        fetch: async (url) => {
            calledUrl = url;
            return okRes({ schemaVersion: 1, releaseTag: 'v1.2.3', assets: [] });
        },
        log: noop,
    });
    assert.strictEqual(res.ok, true);
    assert.match(calledUrl, /\/v1\.2\.3\/release-manifest\.json$/);
});

test('verifyManifest fails when the manifest tag does not match the requested tag', async () => {
    const verifyManifest = loadVerify();
    const res = await verifyManifest('v1.2.3', {
        fetch: async () => okRes({ schemaVersion: 1, releaseTag: 'v9.9.9', assets: [] }),
        log: noop,
    });
    assert.strictEqual(res.ok, false);
    assert.strictEqual(res.mismatch, true);
    assert.match(res.reason, /releaseTag/);
});

test('verifyManifest fails after exhausting attempts on persistent 404', async () => {
    const verifyManifest = loadVerify();
    let calls = 0;
    const res = await verifyManifest('v1.2.3', {
        fetch: async () => {
            calls++;
            return errRes(404);
        },
        attempts: 3,
        delayMs: 0,
        log: noop,
    });
    assert.strictEqual(res.ok, false);
    assert.strictEqual(calls, 3, 'retries up to the attempt limit');
    assert.match(res.reason, /404/);
});

test('verifyManifest retries a transient failure then succeeds', async () => {
    const verifyManifest = loadVerify();
    let calls = 0;
    const res = await verifyManifest('v1.2.3', {
        fetch: async () => {
            calls++;
            if (calls < 2) throw new Error('fetch failed');
            return okRes({ schemaVersion: 1, releaseTag: 'v1.2.3', assets: [] });
        },
        attempts: 5,
        delayMs: 0,
        log: noop,
    });
    assert.strictEqual(res.ok, true);
    assert.strictEqual(calls, 2);
});

test('verifyManifest resolves the URL from the distribution config override', async () => {
    process.env.FERRET_DIST_RELEASE_ENDPOINT = 'https://mirror.example/ferret';
    const verifyManifest = loadVerify();
    let calledUrl = null;
    await verifyManifest('v2.0.0', {
        fetch: async (url) => {
            calledUrl = url;
            return okRes({ releaseTag: 'v2.0.0' });
        },
        log: noop,
    });
    assert.strictEqual(calledUrl, 'https://mirror.example/ferret/v2.0.0/release-manifest.json');
    delete process.env.FERRET_DIST_RELEASE_ENDPOINT;
    delete require.cache[require.resolve('../lib/distribution-config')];
});
