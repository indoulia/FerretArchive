'use strict';

const fsp = require('node:fs/promises');
const { installDir, tempDir } = require('../lib/paths');

async function uninstall(opts = {}) {
    const dir = installDir(opts.platform, opts.env, opts.home);
    await fsp.rm(dir, { recursive: true, force: true });
    await fsp.rm(tempDir(), { recursive: true, force: true });
    console.log(
        'Ferret binary removed. Your .ferret workspaces, indexes, and config were left untouched.'
    );
}

if (require.main === module) {
    uninstall().catch((err) => {
        console.error(err.message);
        process.exit(1);
    });
}

module.exports = { uninstall };
