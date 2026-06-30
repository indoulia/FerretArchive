'use strict';

const { createWriteStream } = require('node:fs');
const { mkdir } = require('node:fs/promises');
const path = require('node:path');
const { pipeline } = require('node:stream/promises');
const { Readable } = require('node:stream');

async function downloadFile(url, destPath, fetchImpl = fetch, attempts = 3) {
    await mkdir(path.dirname(destPath), { recursive: true });
    let lastErr;
    for (let i = 1; i <= attempts; i++) {
        try {
            const res = await fetchImpl(url);
            if (!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
            await pipeline(Readable.fromWeb(res.body), createWriteStream(destPath));
            return destPath;
        } catch (err) {
            lastErr = err;
        }
    }
    throw new Error(`Download failed after ${attempts} attempts: ${lastErr.message}`);
}

module.exports = { downloadFile };
