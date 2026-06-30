'use strict';
// Validate a generated release-manifest.json using the SAME parser the
// installer uses (Ferret.Npm/lib/manifest). The release pipeline runs this
// before publishing so a manifest the installer would reject never ships.
//
// Usage: node scripts/validate-manifest.js <path-to-release-manifest.json>
const fs = require('node:fs');
const path = require('node:path');
const { parseManifest } = require(path.join(__dirname, '..', 'Ferret.Npm', 'lib', 'manifest'));

const file = process.argv[2];
if (!file) {
  console.error('usage: node validate-manifest.js <release-manifest.json>');
  process.exit(2);
}

const manifest = JSON.parse(fs.readFileSync(file, 'utf8'));
parseManifest(manifest); // throws (non-zero exit) on schema problems

for (const asset of manifest.assets) {
  for (const key of ['rid', 'file', 'size', 'sha256', 'binary']) {
    if (!(key in asset)) {
      console.error(`asset ${asset.rid} missing ${key}`);
      process.exit(1);
    }
  }
  if (!/^[0-9a-f]{64}$/.test(asset.sha256)) {
    console.error(`bad sha256 for ${asset.rid}`);
    process.exit(1);
  }
}

const rids = manifest.assets
  .map((a) => a.rid)
  .sort()
  .join(',');
console.log(`manifest valid: schema ${manifest.schemaVersion}, rids [${rids}]`);
