'use strict';
const { test } = require('node:test');
const assert = require('node:assert');

test('releaseBaseUrl builds the GitHub asset URL from owner/repo defaults', () => {
    delete process.env.FERRET_DIST_OWNER;
    delete process.env.FERRET_DIST_REPO;
    delete process.env.FERRET_DIST_RELEASE_ENDPOINT;
    delete require.cache[require.resolve('../lib/distribution-config')];
    const { releaseBaseUrl, OWNER, REPOSITORY } = require('../lib/distribution-config');
    // Downloads default to a PUBLIC mirror repo, not the (private) source repo.
    // Anonymous `npm install` can only reach release assets on a public repo, so
    // the source repo going private must never break installs. See ferret-dist.
    assert.strictEqual(OWNER, 'indoulia');
    assert.strictEqual(REPOSITORY, 'ferret-dist');
    assert.strictEqual(
        releaseBaseUrl('v0.14.0'),
        'https://github.com/indoulia/ferret-dist/releases/download/v0.14.0'
    );
});

test('releaseEndpoint env override wins and trailing slash is normalized', () => {
    const fresh = '../lib/distribution-config.js';
    delete require.cache[require.resolve(fresh)];
    process.env.FERRET_DIST_RELEASE_ENDPOINT = 'https://mirror.corp.example/ferret/';
    const { releaseBaseUrl } = require('../lib/distribution-config');
    assert.strictEqual(releaseBaseUrl('v0.14.0'), 'https://mirror.corp.example/ferret/v0.14.0');
    delete process.env.FERRET_DIST_RELEASE_ENDPOINT;
    delete require.cache[require.resolve(fresh)];
});
