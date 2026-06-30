'use strict';

const { createHash } = require('node:crypto');
const { createReadStream } = require('node:fs');

function sha256File(filePath) {
    return new Promise((resolve, reject) => {
        const hash = createHash('sha256');
        const stream = createReadStream(filePath);
        stream.on('error', reject);
        stream.on('data', (chunk) => hash.update(chunk));
        stream.on('end', () => resolve(hash.digest('hex')));
    });
}

async function verifyChecksum(filePath, expectedSha256) {
    const actual = (await sha256File(filePath)).toLowerCase();
    const expected = String(expectedSha256).toLowerCase();
    if (actual !== expected) {
        throw new Error(`Checksum mismatch for ${filePath}: expected ${expected}, got ${actual}.`);
    }
    return true;
}

module.exports = { sha256File, verifyChecksum };
