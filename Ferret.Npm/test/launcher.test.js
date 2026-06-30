'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { spawnSync } = require('node:child_process');

const LAUNCHER = path.join(__dirname, '..', 'bin', 'ferret.js');

test('launcher exits 1 with a helpful message when the binary is missing', () => {
    const home = path.join(
        os.tmpdir(),
        `ferret-missing-${process.pid}-${Math.round(performance.now())}`
    );
    const res = spawnSync(process.execPath, [LAUNCHER, '--version'], {
        env: {
            ...process.env,
            HOME: home,
            USERPROFILE: home,
            LOCALAPPDATA: path.join(home, 'AppData', 'Local'),
        },
        encoding: 'utf8',
    });
    assert.strictEqual(res.status, 1);
    assert.match(res.stderr, /Ferret binary not found/);
});

test(
    'launcher forwards args and propagates exit code (POSIX)',
    { skip: process.platform === 'win32' },
    async () => {
        const home = path.join(
            os.tmpdir(),
            `ferret-run-${process.pid}-${Math.round(performance.now())}`
        );
        const installDir = path.join(home, '.local', 'share', 'ferret');
        await fsp.mkdir(installDir, { recursive: true });
        const fake = path.join(installDir, 'ferret');
        await fsp.writeFile(fake, '#!/bin/sh\necho "got:$1"\nexit 7\n');
        await fsp.chmod(fake, 0o755);
        const res = spawnSync(process.execPath, [LAUNCHER, 'hello'], {
            env: { ...process.env, HOME: home },
            encoding: 'utf8',
        });
        assert.match(res.stdout, /got:hello/);
        assert.strictEqual(res.status, 7);
        await fsp.rm(home, { recursive: true, force: true });
    }
);
