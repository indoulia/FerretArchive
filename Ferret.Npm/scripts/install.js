'use strict';

const path = require('node:path');
const fsp = require('node:fs/promises');
const { resolveRid } = require('../lib/platform');
const { fetchManifest, selectAsset } = require('../lib/manifest');
const { downloadFile } = require('../lib/download');
const { verifyChecksum } = require('../lib/verify');
const { extract } = require('../lib/extract');
const { installDir, tempDir } = require('../lib/paths');
const { releaseBaseUrl } = require('../lib/distribution-config');
const pkg = require('../package.json');

async function install(opts = {}) {
    const version = opts.version || pkg.version;
    const platform = opts.platform || process.platform;
    const env = opts.env || process.env;
    const home = opts.home; // undefined → paths default to os.homedir()
    const tag = `v${version}`;
    const rid = resolveRid(platform, opts.arch);

    console.log(`Installing Ferret ${version} (${rid})...`);
    const manifest = await fetchManifest(tag);
    const asset = selectAsset(manifest, rid);

    const tmp = tempDir();
    await fsp.rm(tmp, { recursive: true, force: true });
    await fsp.mkdir(tmp, { recursive: true });

    const zipPath = path.join(tmp, asset.file);
    const url = `${releaseBaseUrl(tag)}/${asset.file}`;
    const sizeMb = asset.size ? ` (${(asset.size / 1e6).toFixed(1)} MB)` : '';
    console.log(`Downloading ${asset.file}${sizeMb}...`);
    await downloadFile(url, zipPath);
    await verifyChecksum(zipPath, asset.sha256);

    // Atomic install: extract to staging, then swap into the final dir.
    const finalDir = installDir(platform, env, home);
    const stagingDir = path.join(tmp, 'staging');
    await fsp.rm(stagingDir, { recursive: true, force: true });
    await fsp.mkdir(stagingDir, { recursive: true });
    await extract(zipPath, stagingDir);

    // The release zip wraps its payload in a single top-level folder
    // (Ferret-<version>-<rid>/, the same layout the manual install flow uses).
    // If the archive has exactly one top-level entry and it is a directory,
    // treat that as the package root so the binary lands at <finalDir>/<binary>.
    let contentRoot = stagingDir;
    const entries = await fsp.readdir(stagingDir, { withFileTypes: true });
    if (entries.length === 1 && entries[0].isDirectory()) {
        contentRoot = path.join(stagingDir, entries[0].name);
    }
    try {
        await fsp.access(path.join(contentRoot, asset.binary));
    } catch {
        throw new Error(`Expected ${asset.binary} in the extracted package but did not find it.`);
    }

    // Atomic swap that never destroys a working install: move any existing
    // install aside to a sibling backup, move the new payload into place, then
    // drop the backup. If the move-into-place fails, restore the backup so the
    // user is never left without a working install.
    await fsp.mkdir(path.dirname(finalDir), { recursive: true });
    const backupDir = `${finalDir}.bak-${process.pid}`;
    await fsp.rm(backupDir, { recursive: true, force: true });
    let hadExisting = false;
    try {
        await fsp.rename(finalDir, backupDir);
        hadExisting = true;
    } catch (err) {
        if (err.code !== 'ENOENT') throw err; // no existing install is fine
    }
    try {
        try {
            await fsp.rename(contentRoot, finalDir);
        } catch (err) {
            if (err.code !== 'EXDEV') throw err; // cross-device: copy instead of rename
            await fsp.cp(contentRoot, finalDir, { recursive: true });
        }
    } catch (err) {
        if (hadExisting) {
            await fsp.rm(finalDir, { recursive: true, force: true });
            await fsp.rename(backupDir, finalDir);
        }
        throw err;
    }
    await fsp.rm(backupDir, { recursive: true, force: true });

    if (platform !== 'win32') {
        await fsp.chmod(path.join(finalDir, asset.binary), 0o755);
    }

    await fsp.rm(tmp, { recursive: true, force: true });
    console.log(`Ferret installed to ${finalDir}`);
    return finalDir;
}

// Postinstall entry. Skip for the dev sentinel version so `npm install` inside
// the package checkout does not try to fetch a v0.0.0 release.
if (require.main === module) {
    if (pkg.version === '0.0.0') {
        console.log('Ferret (dev build): skipping binary install.');
        process.exit(0);
    }
    install().catch((err) => {
        console.error(`\nFerret installation failed: ${err.message}\n`);
        process.exit(1);
    });
}

module.exports = { install };
