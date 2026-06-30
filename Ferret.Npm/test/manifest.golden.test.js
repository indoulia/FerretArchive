'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const fs = require('node:fs');
const path = require('node:path');
const { parseManifest, selectAsset } = require('../lib/manifest');

// Golden fixture: the frozen schemaVersion=1 contract. Both the JS parser tests
// here and the Task 11 manifest generator validate against this exact shape.
const golden = JSON.parse(
    fs.readFileSync(path.join(__dirname, 'fixtures', 'release-manifest.golden.json'), 'utf8')
);

test('golden manifest parses and is schemaVersion 1', () => {
    const m = parseManifest(golden);
    assert.strictEqual(m.schemaVersion, 1);
    assert.strictEqual(m.minimumInstallerSchema, 1);
});

test('golden manifest carries all four RIDs with the frozen asset shape', () => {
    const rids = ['win-x64', 'linux-x64', 'osx-arm64', 'osx-x64'];
    assert.strictEqual(golden.assets.length, rids.length);
    for (const rid of rids) {
        const a = selectAsset(golden, rid);
        for (const key of ['rid', 'file', 'size', 'sha256', 'binary']) {
            assert.ok(Object.prototype.hasOwnProperty.call(a, key), `${rid} asset missing ${key}`);
        }
        assert.strictEqual(a.binary, rid.startsWith('win') ? 'ferret.exe' : 'ferret');
        assert.match(a.sha256, /^[0-9a-f]{64}$/);
        assert.strictEqual(typeof a.size, 'number');
    }
});
