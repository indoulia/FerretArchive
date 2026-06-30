'use strict';

// Repository-agnostic distribution config. This is the ONLY module that
// constructs release URLs — nothing else in the wrapper hardcodes a host.
const OWNER = process.env.FERRET_DIST_OWNER || 'indoulia';
const REPOSITORY = process.env.FERRET_DIST_REPO || 'Ferret';
const RELEASE_ENDPOINT = process.env.FERRET_DIST_RELEASE_ENDPOINT || '';

// Directory URL holding the release assets for `tag` (e.g. "v0.14.0").
// Append "/<file>" to reach a specific asset.
function releaseBaseUrl(tag) {
    if (RELEASE_ENDPOINT) {
        return `${RELEASE_ENDPOINT.replace(/\/+$/, '')}/${tag}`;
    }
    return `https://github.com/${OWNER}/${REPOSITORY}/releases/download/${tag}`;
}

module.exports = { OWNER, REPOSITORY, RELEASE_ENDPOINT, releaseBaseUrl };
