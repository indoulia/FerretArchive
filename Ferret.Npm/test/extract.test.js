'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { writeSingleEntryZip } = require('./helpers/make-zip');
const { extract } = require('../lib/extract');

test('extract unpacks a zip entry into destDir', async () => {
    const base = path.join(
        os.tmpdir(),
        `ferret-extract-${process.pid}-${Math.round(performance.now())}`
    );
    await fsp.mkdir(base, { recursive: true });
    const zipPath = path.join(base, 'pkg.zip');
    const dest = path.join(base, 'out');
    writeSingleEntryZip(zipPath, 'ferret', 'BINARY');
    await extract(zipPath, dest);
    assert.strictEqual((await fsp.readFile(path.join(dest, 'ferret'))).toString(), 'BINARY');
    await fsp.rm(base, { recursive: true, force: true });
});
