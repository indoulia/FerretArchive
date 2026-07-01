'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const { parseManifest, selectAsset, fetchManifest } = require('../lib/manifest');

const GOOD = {
    schemaVersion: 1,
    version: '0.14.0',
    releaseTag: 'v0.14.0',
    published: '2026-06-30',
    minimumInstallerSchema: 1,
    assets: [
        {
            rid: 'win-x64',
            file: 'Ferret-0.14.0-win-x64.zip',
            size: 100,
            sha256: 'abc',
            binary: 'ferret.exe',
        },
        {
            rid: 'linux-x64',
            file: 'Ferret-0.14.0-linux-x64.zip',
            size: 90,
            sha256: 'def',
            binary: 'ferret',
        },
    ],
};

test('parseManifest accepts a supported schema', () => {
    assert.strictEqual(parseManifest(GOOD).version, '0.14.0');
});

test('parseManifest rejects a too-new installer schema requirement', () => {
    assert.throws(() => parseManifest({ ...GOOD, minimumInstallerSchema: 2 }), /newer installer/);
});

test('parseManifest rejects a manifest without schemaVersion', () => {
    assert.throws(() => parseManifest({ version: 'x' }), /missing schemaVersion/);
});

test('selectAsset finds the RID and errors when absent', () => {
    assert.strictEqual(selectAsset(GOOD, 'win-x64').binary, 'ferret.exe');
    assert.throws(() => selectAsset(GOOD, 'osx-arm64'), /No asset for osx-arm64/);
});

test('fetchManifest builds the URL and parses the body via injected fetch', async () => {
    let calledUrl = null;
    const fakeFetch = async (url) => {
        calledUrl = url;
        return { ok: true, status: 200, json: async () => GOOD };
    };
    const m = await fetchManifest('v0.14.0', fakeFetch);
    assert.match(calledUrl, /\/v0\.14\.0\/release-manifest\.json$/);
    assert.strictEqual(m.releaseTag, 'v0.14.0');
});

test('fetchManifest throws an actionable error on HTTP failure', async () => {
    const fakeFetch = async () => ({ ok: false, status: 404, json: async () => ({}) });
    await assert.rejects(() => fetchManifest('v9.9.9', fakeFetch), /HTTP 404/);
});

test('fetchManifest 404 error surfaces the exact URL and the private-repo cause', async () => {
    const fakeFetch = async () => ({ ok: false, status: 404, json: async () => ({}) });
    // The message must be diagnosable without re-running: it names the exact URL
    // that 404'd and the most common cause (a private distribution repo), rather
    // than only asking "is that version published?".
    await assert.rejects(
        () => fetchManifest('v9.9.9', fakeFetch),
        (err) => {
            assert.match(err.message, /\/v9\.9\.9\/release-manifest\.json/);
            assert.match(err.message, /private/i);
            return true;
        }
    );
});
