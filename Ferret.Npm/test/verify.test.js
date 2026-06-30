'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { createHash } = require('node:crypto');
const { sha256File, verifyChecksum } = require('../lib/verify');

async function tmpFile(content) {
    const p = path.join(
        os.tmpdir(),
        `ferret-verify-${process.pid}-${Math.round(performance.now())}.bin`
    );
    await fsp.writeFile(p, content);
    return p;
}

test('sha256File matches crypto digest', async () => {
    const p = await tmpFile('hello ferret');
    const expected = createHash('sha256').update('hello ferret').digest('hex');
    assert.strictEqual(await sha256File(p), expected);
    await fsp.rm(p, { force: true });
});

test('verifyChecksum passes on match (case-insensitive) and throws on mismatch', async () => {
    const p = await tmpFile('payload');
    const good = createHash('sha256').update('payload').digest('hex').toUpperCase();
    assert.strictEqual(await verifyChecksum(p, good), true);
    await assert.rejects(() => verifyChecksum(p, 'deadbeef'), /Checksum mismatch/);
    await fsp.rm(p, { force: true });
});
