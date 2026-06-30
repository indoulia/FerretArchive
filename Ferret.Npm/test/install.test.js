'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const http = require('node:http');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const fs = require('node:fs');
const { createHash } = require('node:crypto');
const { writeSingleEntryZip } = require('./helpers/make-zip');

test('install downloads, verifies, and atomically installs the binary', async () => {
    const base = path.join(
        os.tmpdir(),
        `ferret-install-${process.pid}-${Math.round(performance.now())}`
    );
    await fsp.mkdir(base, { recursive: true });
    const home = path.join(base, 'home');
    await fsp.mkdir(home, { recursive: true });

    // Build a real zip asset and its manifest entry. The release zip wraps its
    // payload in a top-level Ferret-<version>-<rid>/ folder (matching package.ps1
    // and the manual install flow), so the entry is nested under that folder.
    const zipPath = path.join(base, 'Ferret-1.2.3-linux-x64.zip');
    writeSingleEntryZip(zipPath, 'Ferret-1.2.3-linux-x64/ferret', '#!/bin/sh\necho ferret-stub\n');
    const sha = createHash('sha256')
        .update(await fsp.readFile(zipPath))
        .digest('hex');
    const manifest = {
        schemaVersion: 1,
        version: '1.2.3',
        releaseTag: 'v1.2.3',
        published: '2026-06-30',
        minimumInstallerSchema: 1,
        assets: [
            {
                rid: 'linux-x64',
                file: 'Ferret-1.2.3-linux-x64.zip',
                size: fs.statSync(zipPath).size,
                sha256: sha,
                binary: 'ferret',
            },
        ],
    };

    const server = http.createServer((req, res) => {
        if (req.url.endsWith('release-manifest.json')) {
            res.end(JSON.stringify(manifest));
            return;
        }
        if (req.url.endsWith('Ferret-1.2.3-linux-x64.zip')) {
            res.end(fs.readFileSync(zipPath));
            return;
        }
        res.statusCode = 404;
        res.end('nope');
    });
    await new Promise((r) => server.listen(0, r));

    // Always close the server and restore env, even if install() throws — an
    // open listener would otherwise keep the test process alive forever.
    try {
        const port = server.address().port;
        process.env.FERRET_DIST_RELEASE_ENDPOINT = `http://127.0.0.1:${port}/download`;
        delete require.cache[require.resolve('../lib/distribution-config')];
        delete require.cache[require.resolve('../lib/manifest')];
        delete require.cache[require.resolve('../scripts/install')];
        const { install } = require('../scripts/install');

        const finalDir = await install({ version: '1.2.3', platform: 'linux', env: {}, home });
        const binPath = path.join(finalDir, 'ferret');
        assert.ok(fs.existsSync(binPath), 'binary installed');
        // The POSIX executable bit is only observable on a POSIX host filesystem.
        // On Windows the chmod is a no-op at the FS level, so assert it only where
        // it is meaningful (e.g. the Linux CI runner).
        if (process.platform !== 'win32') {
            assert.strictEqual(
                (fs.statSync(binPath).mode & 0o100) !== 0,
                true,
                'binary is executable'
            );
        }

        // Re-install over an existing install: exercises the backup-and-swap
        // path and must leave a working binary in place.
        const finalDir2 = await install({ version: '1.2.3', platform: 'linux', env: {}, home });
        assert.strictEqual(finalDir2, finalDir, 'same install dir on re-install');
        assert.ok(fs.existsSync(binPath), 'binary present after re-install');
        assert.ok(!fs.existsSync(`${finalDir}.bak-${process.pid}`), 'backup cleaned up');
    } finally {
        await new Promise((r) => server.close(r));
        delete process.env.FERRET_DIST_RELEASE_ENDPOINT;
        delete require.cache[require.resolve('../lib/distribution-config')];
        await fsp.rm(base, { recursive: true, force: true });
    }
});
