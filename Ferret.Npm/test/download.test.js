'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { Readable } = require('node:stream');
const { downloadFile } = require('../lib/download');

function tmp(name) {
    return path.join(
        os.tmpdir(),
        `ferret-dl-${process.pid}-${Math.round(performance.now())}`,
        name
    );
}

test('downloadFile writes the streamed body to disk', async () => {
    const dest = tmp('out.bin');
    const fakeFetch = async () => ({
        ok: true,
        status: 200,
        body: Readable.toWeb(Readable.from([Buffer.from('zipdata')])),
    });
    await downloadFile('http://x/y', dest, fakeFetch);
    assert.strictEqual((await fsp.readFile(dest)).toString(), 'zipdata');
    await fsp.rm(path.dirname(dest), { recursive: true, force: true });
});

test('downloadFile retries then throws after exhausting attempts', async () => {
    const dest = tmp('out.bin');
    let calls = 0;
    const fakeFetch = async () => {
        calls++;
        return { ok: false, status: 500 };
    };
    await assert.rejects(() => downloadFile('http://x/y', dest, fakeFetch, 3), /after 3 attempts/);
    assert.strictEqual(calls, 3);
    await fsp.rm(path.dirname(dest), { recursive: true, force: true });
});
