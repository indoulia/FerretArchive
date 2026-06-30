'use strict';

const extractZip = require('extract-zip');
const path = require('node:path');

// extract-zip requires an absolute destination directory.
async function extract(zipPath, destDir) {
    await extractZip(zipPath, { dir: path.resolve(destDir) });
}

module.exports = { extract };
