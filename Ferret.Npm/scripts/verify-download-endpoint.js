'use strict';

// Post-publish smoke check: the release manifest MUST be anonymously reachable.
// `npm install -g @indoulia/ferret` runs unauthenticated, so if the distribution
// repo is private (or the mirror step was skipped) every download URL 404s and
// installs break for everyone. Running this in the release workflow turns that
// class of failure into a red CI run instead of a user-visible outage.
//
// Usage: node scripts/verify-download-endpoint.js <vX.Y.Z>

const { releaseBaseUrl } = require('../lib/distribution-config');

// Release assets can take a few seconds to propagate after publish; retry a few
// times before failing. Tunable via env for CI and for fast failure-path tests.
const ATTEMPTS = Number(process.env.FERRET_VERIFY_ATTEMPTS) || 8;
const DELAY_MS = Number(process.env.FERRET_VERIFY_DELAY_MS) || 3000;

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function check(url) {
    const res = await fetch(url);
    if (!res.ok) {
        return { ok: false, reason: `HTTP ${res.status}` };
    }
    const body = await res.json();
    return { ok: true, body };
}

async function main() {
    const tag = process.argv[2] || process.env.FERRET_VERIFY_TAG || '';
    if (!/^v\d+\.\d+\.\d+/.test(tag)) {
        console.error('Usage: node scripts/verify-download-endpoint.js <vX.Y.Z>');
        process.exit(2);
    }
    const url = `${releaseBaseUrl(tag)}/release-manifest.json`;
    console.log(`Verifying anonymous reachability of ${url}`);

    let last = null;
    for (let i = 1; i <= ATTEMPTS; i++) {
        try {
            const res = await check(url);
            if (res.ok) {
                if (res.body.releaseTag && res.body.releaseTag !== tag) {
                    console.error(
                        `FAIL: manifest releaseTag "${res.body.releaseTag}" != requested "${tag}".`
                    );
                    process.exit(1);
                }
                console.log('OK: manifest is publicly reachable and matches the tag.');
                return;
            }
            last = res.reason;
        } catch (err) {
            last = err.message;
        }
        if (i < ATTEMPTS) {
            console.log(`  attempt ${i}/${ATTEMPTS} not ready (${last}); retrying...`);
            await sleep(DELAY_MS);
        }
    }
    console.error(
        `FAIL: manifest not anonymously reachable after ${ATTEMPTS} attempts (${last}).\n` +
            `The distribution repo/host must be PUBLIC. A private source repo cannot\n` +
            `serve release assets to unauthenticated npm installs. See lib/distribution-config.js.`
    );
    process.exit(1);
}

main().catch((err) => {
    console.error(`FAIL: ${err.message}`);
    process.exit(1);
});
