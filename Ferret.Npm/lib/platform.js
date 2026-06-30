'use strict';

const MAP = {
    'win32:x64': 'win-x64',
    'darwin:arm64': 'osx-arm64',
    'darwin:x64': 'osx-x64',
    'linux:x64': 'linux-x64',
};

function resolveRid(platform = process.platform, arch = process.arch) {
    const rid = MAP[`${platform}:${arch}`];
    if (!rid) {
        throw new Error(
            `Unsupported platform: ${platform}/${arch}. Ferret supports: ${Object.values(MAP).join(', ')}.`
        );
    }
    return rid;
}

module.exports = { resolveRid };
