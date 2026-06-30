'use strict';

const os = require('node:os');
const path = require('node:path');

function installDir(platform = process.platform, env = process.env, home = os.homedir()) {
    if (platform === 'win32') {
        const base = env.LOCALAPPDATA || path.join(home, 'AppData', 'Local');
        return path.join(base, 'Programs', 'Ferret');
    }
    if (platform === 'darwin') {
        return path.join(home, 'Library', 'Application Support', 'Ferret');
    }
    return path.join(home, '.local', 'share', 'ferret');
}

function tempDir() {
    return path.join(os.tmpdir(), 'ferret-install');
}

module.exports = { installDir, tempDir };
