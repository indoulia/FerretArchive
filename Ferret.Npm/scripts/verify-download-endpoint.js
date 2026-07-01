'use strict';

// Post-publish smoke check: the release manifest MUST be anonymously reachable.
// `npm install -g @indoulia/ferret` runs unauthenticated, so if the distribution
// repo is private (or the mirror step was skipped) every download URL 404s and
// installs break for everyone. Running this in the release workflow turns that
// class of failure into a red CI run instead of a user-visible outage.
//
// Usage: node scripts/verify-download-endpoint.js <vX.Y.Z>

const { releaseBaseUrl } = require('../lib/distribution-config');

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// Resolve the manifest URL for `tag` and confirm it is anonymously fetchable,
// retrying a few times to absorb post-publish propagation delay. Returns
// { ok, url, reason }. `fetch` and timing are injectable so this is unit-testable
// without a network or a local server.
async function verifyManifest(tag, opts = {}) {
    const fetchImpl = opts.fetch || fetch;
    const attempts = opts.attempts || Number(process.env.FERRET_VERIFY_ATTEMPTS) || 8;
    const delayMs =
        opts.delayMs != null ? opts.delayMs : Number(process.env.FERRET_VERIFY_DELAY_MS) || 3000;
    const log = opts.log || console.log;

    const url = `${releaseBaseUrl(tag)}/release-manifest.json`;
    log(`Verifying anonymous reachability of ${url}`);

    let last = null;
    for (let i = 1; i <= attempts; i++) {
        try {
            const res = await fetchImpl(url);
            if (res.ok) {
                const body = await res.json();
                if (body.releaseTag && body.releaseTag !== tag) {
                    return {
                        ok: false,
                        url,
                        mismatch: true,
                        reason: `manifest releaseTag "${body.releaseTag}" != requested "${tag}"`,
                    };
                }
                return { ok: true, url };
            }
            last = `HTTP ${res.status}`;
        } catch (err) {
            last = err.message;
        }
        if (i < attempts) {
            log(`  attempt ${i}/${attempts} not ready (${last}); retrying...`);
            await sleep(delayMs);
        }
    }
    return { ok: false, url, reason: last };
}

async function main(argv) {
    const tag = argv[2] || process.env.FERRET_VERIFY_TAG || '';
    if (!/^v\d+\.\d+\.\d+/.test(tag)) {
        console.error('Usage: node scripts/verify-download-endpoint.js <vX.Y.Z>');
        return 2;
    }
    const result = await verifyManifest(tag);
    if (result.ok) {
        console.log('OK: manifest is publicly reachable and matches the tag.');
        return 0;
    }
    if (result.mismatch) {
        console.error(`FAIL: ${result.reason}.`);
        return 1;
    }
    console.error(
        `FAIL: manifest not anonymously reachable (${result.reason}).\n` +
            `The distribution repo/host must be PUBLIC. A private source repo cannot\n` +
            `serve release assets to unauthenticated npm installs. See lib/distribution-config.js.`
    );
    return 1;
}

if (require.main === module) {
    main(process.argv)
        .then((code) => process.exit(code))
        .catch((err) => {
            console.error(`FAIL: ${err.message}`);
            process.exit(1);
        });
}

module.exports = { verifyManifest };
