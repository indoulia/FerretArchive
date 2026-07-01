'use strict';

const { releaseBaseUrl } = require('./distribution-config');

const SUPPORTED_SCHEMA = 1;

function parseManifest(manifest) {
    if (!manifest || typeof manifest.schemaVersion !== 'number') {
        throw new Error('Invalid release manifest: missing schemaVersion.');
    }
    if (
        typeof manifest.minimumInstallerSchema === 'number' &&
        manifest.minimumInstallerSchema > SUPPORTED_SCHEMA
    ) {
        throw new Error(
            `This release requires a newer installer (manifest schema ` +
                `${manifest.minimumInstallerSchema} > supported ${SUPPORTED_SCHEMA}). ` +
                `Run: npm update -g @indoulia/ferret`
        );
    }
    return manifest;
}

function selectAsset(manifest, rid) {
    const asset = (manifest.assets || []).find((a) => a.rid === rid);
    if (!asset) {
        throw new Error(
            `No asset for ${rid} in release ${manifest.releaseTag || manifest.version}.`
        );
    }
    return asset;
}

async function fetchManifest(tag, fetchImpl = fetch) {
    const url = `${releaseBaseUrl(tag)}/release-manifest.json`;
    const res = await fetchImpl(url);
    if (!res.ok) {
        throw new Error(
            `Could not fetch the release manifest for ${tag} (HTTP ${res.status}).\n` +
                `  URL: ${url}\n` +
                `  The asset may be missing/unpublished, or hosted in a private ` +
                `repository — GitHub returns 404 for private release assets to ` +
                `unauthenticated clients. Point downloads at a public host via ` +
                `FERRET_DIST_OWNER / FERRET_DIST_REPO / FERRET_DIST_RELEASE_ENDPOINT.`
        );
    }
    return parseManifest(await res.json());
}

module.exports = { SUPPORTED_SCHEMA, parseManifest, selectAsset, fetchManifest };
