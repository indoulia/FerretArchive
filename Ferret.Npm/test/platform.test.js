'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const { resolveRid } = require('../lib/platform');

test('maps known platform/arch pairs to RIDs', () => {
    assert.strictEqual(resolveRid('win32', 'x64'), 'win-x64');
    assert.strictEqual(resolveRid('darwin', 'arm64'), 'osx-arm64');
    assert.strictEqual(resolveRid('darwin', 'x64'), 'osx-x64');
    assert.strictEqual(resolveRid('linux', 'x64'), 'linux-x64');
});

test('throws an actionable error on unsupported platform', () => {
    assert.throws(() => resolveRid('linux', 'arm64'), /Unsupported platform: linux\/arm64/);
});
