'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const fs = require('node:fs');
const { uninstall } = require('../scripts/uninstall');

test('uninstall removes the binary dir but preserves .ferret workspace data', async () => {
    const home = path.join(
        os.tmpdir(),
        `ferret-uninstall-${process.pid}-${Math.round(performance.now())}`
    );
    const installed = path.join(home, '.local', 'share', 'ferret');
    await fsp.mkdir(installed, { recursive: true });
    await fsp.writeFile(path.join(installed, 'ferret'), 'bin');

    const workspace = path.join(home, 'project', '.ferret');
    await fsp.mkdir(workspace, { recursive: true });
    await fsp.writeFile(path.join(workspace, 'index.db'), 'data');

    await uninstall({ platform: 'linux', env: {}, home });

    assert.strictEqual(fs.existsSync(installed), false, 'install dir removed');
    assert.strictEqual(
        fs.existsSync(path.join(workspace, 'index.db')),
        true,
        'workspace preserved'
    );
    await fsp.rm(home, { recursive: true, force: true });
});
