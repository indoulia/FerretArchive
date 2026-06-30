#!/usr/bin/env node
'use strict';

const { spawnSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const { installDir } = require('../lib/paths');

const binaryName = process.platform === 'win32' ? 'ferret.exe' : 'ferret';
const binaryPath = path.join(installDir(), binaryName);

if (!fs.existsSync(binaryPath)) {
    console.error(
        `Ferret binary not found at ${binaryPath}. Try reinstalling: npm install -g @indoulia/ferret`
    );
    process.exit(1);
}

const result = spawnSync(binaryPath, process.argv.slice(2), { stdio: 'inherit' });
if (result.error) {
    console.error(result.error.message);
    process.exit(1);
}
process.exit(result.status === null ? 1 : result.status);
