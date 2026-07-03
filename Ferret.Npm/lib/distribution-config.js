'use strict';

// Repository-agnostic distribution config. This is the ONLY module that
// constructs release URLs — nothing else in the wrapper hardcodes a host.
// REPOSITORY defaults to the PUBLIC distribution mirror, never the source repo.
// Release assets are only anonymously downloadable from a public repo, and
// `npm install` runs unauthenticated — so if the source repo is private, its
// release URLs 404. The mirror holds only the downloadable payload (zips,
// SHA256SUMS, release-manifest.json); source stays wherever it lives.
const OWNER = process.env.FERRET_DIST_OWNER || 'indoulia';
const REPOSITORY = process.env.FERRET_DIST_REPO || 'ferret-dist';
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
