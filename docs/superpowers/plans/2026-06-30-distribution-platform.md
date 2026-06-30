# Distribution Platform Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish Ferret as consumable, checksum-verified release assets driven by a versioned `release-manifest.json` contract, and ship an NPM wrapper (`@indoulia/ferret`) as the first consumer that downloads, verifies, and installs the native binary.

**Architecture:** GitHub Releases are the single source of truth. The release pipeline publishes per-RID self-contained binary zips plus a top-level `SHA256SUMS.txt` and `release-manifest.json`. The NPM wrapper reads the manifest, selects the asset for the running platform, verifies its SHA256, and atomically installs the binary. The manifest is a versioned public contract that future consumers (Homebrew, winget, enterprise mirror, `ferret self-update`) reuse without pipeline changes.

**Tech Stack:** PowerShell 5.1+ (packaging, manifest generation), GitHub Actions (release + npm-publish workflows), Node.js ≥18 (wrapper), `node:test` (wrapper tests), `extract-zip` (sole runtime dependency).

**Reference spec:** `docs/superpowers/specs/2026-06-30-distribution-platform-design.md` (frozen).

## Global Constraints

Every task implicitly includes these. Values are copied verbatim from the spec.

- **Single source of truth:** consumers read `release-manifest.json`; never infer filenames, scrape HTML, or enumerate release assets.
- **Repository-agnostic config:** no hardcoded URLs anywhere in the wrapper. Config keys: `owner` (default `indoulia`), `repository` (default `Ferret`), `releaseEndpoint` (optional). All environment-overridable.
- **NPM package name:** `@indoulia/ferret`. Published with `--access public`.
- **Distribution Dependency Policy:** maximum **2** runtime dependencies; initial runtime deps = `extract-zip` only. Everything else from Node stdlib (`crypto`, `fetch`, `fs`, `path`, `child_process`, `stream`). DevDeps allowed: `eslint`, `prettier`.
- **Node engine:** `>=18`.
- **Supported RIDs:** `win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`. Binary name: `ferret.exe` on Windows, `ferret` elsewhere.
- **Version-locked:** NPM package version === Ferret release version; install tag is `v<version>`.
- **Atomic install:** extract to a staging folder, then rename into the install dir. Never overwrite the active install in place.
- **Install locations:** Windows `%LOCALAPPDATA%\Programs\Ferret`; macOS `~/Library/Application Support/Ferret`; Linux `~/.local/share/ferret`.
- **Uninstall preserves user data:** removes only the binary + temp; never touches `.ferret` workspaces, indexes, or config.
- **Manifest schema:** `schemaVersion`, `version`, `releaseTag`, `published`, `minimumInstallerSchema`, `metadata` (`generator`, `generatorVersion`), `assets[]` (`rid`, `file`, `size`, `sha256`, `binary`). Installer supports `schemaVersion` 1.
- **macOS artifacts are unsigned/unnotarized** — known milestone limitation.
- **Commit policy:** stage `.claude/` files with each commit; never stage `artifacts/` (gitignored) or secrets.

---

### Task 1: Scaffold `Ferret.Npm/` and `distribution-config.js`

Creates the package skeleton (so tests can run) and the repository-agnostic configuration module — the only place URLs are constructed.

**Files:**
- Create: `Ferret.Npm/package.json`
- Create: `Ferret.Npm/.gitignore`
- Create: `Ferret.Npm/.prettierrc.json`
- Create: `Ferret.Npm/eslint.config.js`
- Create: `Ferret.Npm/lib/distribution-config.js`
- Test: `Ferret.Npm/test/distribution-config.test.js`

**Interfaces:**
- Produces:
  - `OWNER: string`, `REPOSITORY: string`, `RELEASE_ENDPOINT: string`
  - `releaseBaseUrl(tag: string): string` — directory URL that holds the release's assets for `tag`.

- [ ] **Step 1: Write the failing test**

`Ferret.Npm/test/distribution-config.test.js`:
```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');

test('releaseBaseUrl builds the GitHub asset URL from owner/repo defaults', () => {
  delete process.env.FERRET_DIST_OWNER;
  delete process.env.FERRET_DIST_REPO;
  delete process.env.FERRET_DIST_RELEASE_ENDPOINT;
  const { releaseBaseUrl, OWNER, REPOSITORY } = require('../lib/distribution-config');
  assert.strictEqual(OWNER, 'indoulia');
  assert.strictEqual(REPOSITORY, 'Ferret');
  assert.strictEqual(
    releaseBaseUrl('v0.14.0'),
    'https://github.com/indoulia/Ferret/releases/download/v0.14.0'
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
```

- [ ] **Step 2: Create the scaffold so the test can run**

`Ferret.Npm/package.json`:
```json
{
  "name": "@indoulia/ferret",
  "version": "0.0.0",
  "description": "Ferret — local-first code & document search with an MCP server for AI agents.",
  "bin": { "ferret": "bin/ferret.js" },
  "scripts": {
    "postinstall": "node scripts/install.js",
    "preuninstall": "node scripts/uninstall.js",
    "test": "node --test",
    "lint": "eslint .",
    "format": "prettier --check ."
  },
  "engines": { "node": ">=18" },
  "dependencies": { "extract-zip": "^2.0.1" },
  "devDependencies": { "eslint": "^9.0.0", "prettier": "^3.0.0" },
  "files": ["bin/", "lib/", "scripts/"],
  "license": "MIT"
}
```

`Ferret.Npm/.gitignore`:
```
node_modules/
```

`Ferret.Npm/.prettierrc.json`:
```json
{ "singleQuote": true, "printWidth": 100, "trailingComma": "es5" }
```

`Ferret.Npm/eslint.config.js` (Node globals declared manually so `no-undef` does not flag `require`/`module`/`process`/etc. — keeps the zero-extra-dependency rule):
```js
'use strict';

const nodeGlobals = {
  require: 'readonly',
  module: 'writable',
  exports: 'writable',
  process: 'readonly',
  console: 'readonly',
  Buffer: 'readonly',
  __dirname: 'readonly',
  __filename: 'readonly',
  fetch: 'readonly',
  performance: 'readonly',
  URL: 'readonly',
  setTimeout: 'readonly',
  clearTimeout: 'readonly',
};

module.exports = [
  {
    files: ['**/*.js'],
    languageOptions: { ecmaVersion: 2022, sourceType: 'commonjs', globals: nodeGlobals },
    rules: { 'no-unused-vars': 'error', 'no-undef': 'error' },
  },
];
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `cd Ferret.Npm && node --test test/distribution-config.test.js`
Expected: FAIL — `Cannot find module '../lib/distribution-config'`.

- [ ] **Step 4: Implement `lib/distribution-config.js`**

```js
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
```

- [ ] **Step 5: Generate the lockfile and run the test**

Run: `cd Ferret.Npm && npm install --ignore-scripts && node --test test/distribution-config.test.js`
Expected: lockfile created; both tests PASS.

- [ ] **Step 6: Commit**

```bash
git add Ferret.Npm/package.json Ferret.Npm/package-lock.json Ferret.Npm/.gitignore Ferret.Npm/.prettierrc.json Ferret.Npm/eslint.config.js Ferret.Npm/lib/distribution-config.js Ferret.Npm/test/distribution-config.test.js .claude/
git commit -m "feat(npm): scaffold Ferret.Npm and repository-agnostic distribution config"
```

---

### Task 2: `platform.js` — RID resolution

Maps the running platform/arch to a Ferret RID, with an actionable error for unsupported combinations.

**Files:**
- Create: `Ferret.Npm/lib/platform.js`
- Test: `Ferret.Npm/test/platform.test.js`

**Interfaces:**
- Produces: `resolveRid(platform?: string, arch?: string): string` — defaults to `process.platform`/`process.arch`; throws on unsupported.

- [ ] **Step 1: Write the failing test**

```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const { resolveRid } = require('../lib/platform');

test('maps known platform/arch pairs to RIDs', () => {
  assert.strictEqual(resolveRid('win32', 'x64'), 'win-x64');
  assert.strictEqual(resolveRid('darwin', 'arm64'), 'osx-arm64');
  assert.strictEqual(resolveRid('darwin', 'x64'), 'osx-x64');
  assert.strictEqual(resolveRid('linux', 'x64'), 'linux-x64');
});

test('throws an actionable error on unsupported platform', () => {
  assert.throws(() => resolveRid('linux', 'arm64'), /Unsupported platform: linux\/arm64/);
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/platform.test.js`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement `lib/platform.js`**

```js
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
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/platform.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/lib/platform.js Ferret.Npm/test/platform.test.js .claude/
git commit -m "feat(npm): resolve platform/arch to Ferret RID"
```

---

### Task 3: `manifest.js` — parse, schema-guard, asset selection

Parses and validates the manifest and selects the asset for a RID. HTTP is injected so the logic is testable offline.

**Files:**
- Create: `Ferret.Npm/lib/manifest.js`
- Test: `Ferret.Npm/test/manifest.test.js`

**Interfaces:**
- Consumes: `releaseBaseUrl(tag)` from `distribution-config`.
- Produces:
  - `SUPPORTED_SCHEMA: number` (= 1)
  - `parseManifest(obj: object): object` — validates `schemaVersion` and `minimumInstallerSchema`; returns the manifest.
  - `selectAsset(manifest: object, rid: string): {rid, file, size, sha256, binary}`
  - `fetchManifest(tag: string, fetchImpl?: typeof fetch): Promise<object>`

- [ ] **Step 1: Write the failing test**

```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const { parseManifest, selectAsset, fetchManifest } = require('../lib/manifest');

const GOOD = {
  schemaVersion: 1,
  version: '0.14.0',
  releaseTag: 'v0.14.0',
  published: '2026-06-30',
  minimumInstallerSchema: 1,
  assets: [
    { rid: 'win-x64', file: 'Ferret-0.14.0-win-x64.zip', size: 100, sha256: 'abc', binary: 'ferret.exe' },
    { rid: 'linux-x64', file: 'Ferret-0.14.0-linux-x64.zip', size: 90, sha256: 'def', binary: 'ferret' },
  ],
};

test('parseManifest accepts a supported schema', () => {
  assert.strictEqual(parseManifest(GOOD).version, '0.14.0');
});

test('parseManifest rejects a too-new installer schema requirement', () => {
  assert.throws(() => parseManifest({ ...GOOD, minimumInstallerSchema: 2 }), /newer installer/);
});

test('parseManifest rejects a manifest without schemaVersion', () => {
  assert.throws(() => parseManifest({ version: 'x' }), /missing schemaVersion/);
});

test('selectAsset finds the RID and errors when absent', () => {
  assert.strictEqual(selectAsset(GOOD, 'win-x64').binary, 'ferret.exe');
  assert.throws(() => selectAsset(GOOD, 'osx-arm64'), /No asset for osx-arm64/);
});

test('fetchManifest builds the URL and parses the body via injected fetch', async () => {
  let calledUrl = null;
  const fakeFetch = async (url) => {
    calledUrl = url;
    return { ok: true, status: 200, json: async () => GOOD };
  };
  const m = await fetchManifest('v0.14.0', fakeFetch);
  assert.match(calledUrl, /\/v0\.14\.0\/release-manifest\.json$/);
  assert.strictEqual(m.releaseTag, 'v0.14.0');
});

test('fetchManifest throws an actionable error on HTTP failure', async () => {
  const fakeFetch = async () => ({ ok: false, status: 404, json: async () => ({}) });
  await assert.rejects(() => fetchManifest('v9.9.9', fakeFetch), /HTTP 404/);
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/manifest.test.js`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement `lib/manifest.js`**

```js
'use strict';

const { releaseBaseUrl } = require('./distribution-config');

const SUPPORTED_SCHEMA = 1;

function parseManifest(manifest) {
  if (!manifest || typeof manifest.schemaVersion !== 'number') {
    throw new Error('Invalid release manifest: missing schemaVersion.');
  }
  if (typeof manifest.minimumInstallerSchema === 'number' &&
      manifest.minimumInstallerSchema > SUPPORTED_SCHEMA) {
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
    throw new Error(`No asset for ${rid} in release ${manifest.releaseTag || manifest.version}.`);
  }
  return asset;
}

async function fetchManifest(tag, fetchImpl = fetch) {
  const url = `${releaseBaseUrl(tag)}/release-manifest.json`;
  const res = await fetchImpl(url);
  if (!res.ok) {
    throw new Error(
      `Could not fetch release manifest for ${tag} (HTTP ${res.status}). Is that version published?`
    );
  }
  return parseManifest(await res.json());
}

module.exports = { SUPPORTED_SCHEMA, parseManifest, selectAsset, fetchManifest };
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/manifest.test.js`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/lib/manifest.js Ferret.Npm/test/manifest.test.js .claude/
git commit -m "feat(npm): parse and validate release manifest, select asset by RID"
```

---

### Task 4: `verify.js` — SHA256 verification

Hashes a downloaded file and compares it against the manifest checksum; hard-fails on mismatch.

**Files:**
- Create: `Ferret.Npm/lib/verify.js`
- Test: `Ferret.Npm/test/verify.test.js`

**Interfaces:**
- Produces:
  - `sha256File(filePath: string): Promise<string>` — lowercase hex digest.
  - `verifyChecksum(filePath: string, expectedSha256: string): Promise<true>` — throws on mismatch.

- [ ] **Step 1: Write the failing test**

```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { createHash } = require('node:crypto');
const { sha256File, verifyChecksum } = require('../lib/verify');

async function tmpFile(content) {
  const p = path.join(os.tmpdir(), `ferret-verify-${process.pid}-${Math.round(performance.now())}.bin`);
  await fsp.writeFile(p, content);
  return p;
}

test('sha256File matches crypto digest', async () => {
  const p = await tmpFile('hello ferret');
  const expected = createHash('sha256').update('hello ferret').digest('hex');
  assert.strictEqual(await sha256File(p), expected);
  await fsp.rm(p, { force: true });
});

test('verifyChecksum passes on match (case-insensitive) and throws on mismatch', async () => {
  const p = await tmpFile('payload');
  const good = createHash('sha256').update('payload').digest('hex').toUpperCase();
  assert.strictEqual(await verifyChecksum(p, good), true);
  await assert.rejects(() => verifyChecksum(p, 'deadbeef'), /Checksum mismatch/);
  await fsp.rm(p, { force: true });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/verify.test.js`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement `lib/verify.js`**

```js
'use strict';

const { createHash } = require('node:crypto');
const { createReadStream } = require('node:fs');

function sha256File(filePath) {
  return new Promise((resolve, reject) => {
    const hash = createHash('sha256');
    const stream = createReadStream(filePath);
    stream.on('error', reject);
    stream.on('data', (chunk) => hash.update(chunk));
    stream.on('end', () => resolve(hash.digest('hex')));
  });
}

async function verifyChecksum(filePath, expectedSha256) {
  const actual = (await sha256File(filePath)).toLowerCase();
  const expected = String(expectedSha256).toLowerCase();
  if (actual !== expected) {
    throw new Error(`Checksum mismatch for ${filePath}: expected ${expected}, got ${actual}.`);
  }
  return true;
}

module.exports = { sha256File, verifyChecksum };
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/verify.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/lib/verify.js Ferret.Npm/test/verify.test.js .claude/
git commit -m "feat(npm): SHA256 verification with hard-fail on mismatch"
```

---

### Task 5: `paths.js` — install and temp locations

Resolves the per-OS install directory and a temp download directory. Platform/env are injected for testability.

**Files:**
- Create: `Ferret.Npm/lib/paths.js`
- Test: `Ferret.Npm/test/paths.test.js`

**Interfaces:**
- Produces:
  - `installDir(platform?: string, env?: object, home?: string): string`
  - `tempDir(): string`

- [ ] **Step 1: Write the failing test**

```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const path = require('node:path');
const { installDir } = require('../lib/paths');

test('Windows uses LOCALAPPDATA\\Programs\\Ferret', () => {
  const dir = installDir('win32', { LOCALAPPDATA: 'C:\\Users\\u\\AppData\\Local' }, 'C:\\Users\\u');
  assert.strictEqual(dir, path.join('C:\\Users\\u\\AppData\\Local', 'Programs', 'Ferret'));
});

test('macOS uses ~/Library/Application Support/Ferret', () => {
  const dir = installDir('darwin', {}, '/Users/u');
  assert.strictEqual(dir, path.join('/Users/u', 'Library', 'Application Support', 'Ferret'));
});

test('Linux uses ~/.local/share/ferret', () => {
  const dir = installDir('linux', {}, '/home/u');
  assert.strictEqual(dir, path.join('/home/u', '.local', 'share', 'ferret'));
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/paths.test.js`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement `lib/paths.js`**

```js
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
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/paths.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/lib/paths.js Ferret.Npm/test/paths.test.js .claude/
git commit -m "feat(npm): per-OS install and temp path resolution"
```

---

### Task 6: `download.js` — download with retry

Streams a URL to disk with a small retry loop. `fetch` is injected for testing.

**Files:**
- Create: `Ferret.Npm/lib/download.js`
- Test: `Ferret.Npm/test/download.test.js`

**Interfaces:**
- Produces: `downloadFile(url: string, destPath: string, fetchImpl?: typeof fetch, attempts?: number): Promise<string>`

- [ ] **Step 1: Write the failing test**

```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { Readable } = require('node:stream');
const { downloadFile } = require('../lib/download');

function tmp(name) {
  return path.join(os.tmpdir(), `ferret-dl-${process.pid}-${Math.round(performance.now())}`, name);
}

test('downloadFile writes the streamed body to disk', async () => {
  const dest = tmp('out.bin');
  const fakeFetch = async () => ({
    ok: true,
    status: 200,
    body: Readable.toWeb(Readable.from([Buffer.from('zipdata')])),
  });
  await downloadFile('http://x/y', dest, fakeFetch);
  assert.strictEqual((await fsp.readFile(dest)).toString(), 'zipdata');
  await fsp.rm(path.dirname(dest), { recursive: true, force: true });
});

test('downloadFile retries then throws after exhausting attempts', async () => {
  const dest = tmp('out.bin');
  let calls = 0;
  const fakeFetch = async () => { calls++; return { ok: false, status: 500 }; };
  await assert.rejects(() => downloadFile('http://x/y', dest, fakeFetch, 3), /after 3 attempts/);
  assert.strictEqual(calls, 3);
  await fsp.rm(path.dirname(dest), { recursive: true, force: true });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/download.test.js`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement `lib/download.js`**

```js
'use strict';

const { createWriteStream } = require('node:fs');
const { mkdir } = require('node:fs/promises');
const path = require('node:path');
const { pipeline } = require('node:stream/promises');
const { Readable } = require('node:stream');

async function downloadFile(url, destPath, fetchImpl = fetch, attempts = 3) {
  await mkdir(path.dirname(destPath), { recursive: true });
  let lastErr;
  for (let i = 1; i <= attempts; i++) {
    try {
      const res = await fetchImpl(url);
      if (!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
      await pipeline(Readable.fromWeb(res.body), createWriteStream(destPath));
      return destPath;
    } catch (err) {
      lastErr = err;
    }
  }
  throw new Error(`Download failed after ${attempts} attempts: ${lastErr.message}`);
}

module.exports = { downloadFile };
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/download.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/lib/download.js Ferret.Npm/test/download.test.js .claude/
git commit -m "feat(npm): streaming download with retry"
```

---

### Task 7: `extract.js` — zip extraction wrapper

Thin wrapper over `extract-zip` (the one runtime dependency). Tested against a real zip built in the test.

**Files:**
- Create: `Ferret.Npm/lib/extract.js`
- Test: `Ferret.Npm/test/extract.test.js`
- Create (test fixture helper, committed): `Ferret.Npm/test/helpers/make-zip.js`

**Interfaces:**
- Produces: `extract(zipPath: string, destDir: string): Promise<void>` — `destDir` must be absolute.

- [ ] **Step 1: Write the test fixture helper**

`Ferret.Npm/test/helpers/make-zip.js` (zero-dependency minimal STORE-method zip writer, enough for `extract-zip` to read):
```js
'use strict';
// Minimal ZIP writer (STORE/no compression) for tests. Writes one file entry.
const { writeFileSync } = require('node:fs');
const zlib = require('node:zlib');

function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }
  return (~c) >>> 0;
}

// name: entry name; content: Buffer|string
function writeSingleEntryZip(zipPath, name, content) {
  const data = Buffer.isBuffer(content) ? content : Buffer.from(content);
  const nameBuf = Buffer.from(name);
  const crc = crc32(data);

  const local = Buffer.alloc(30);
  local.writeUInt32LE(0x04034b50, 0);   // local file header sig
  local.writeUInt16LE(20, 4);            // version needed
  local.writeUInt16LE(0, 6);             // flags
  local.writeUInt16LE(0, 8);             // method = STORE
  local.writeUInt16LE(0, 10);            // mod time
  local.writeUInt16LE(0, 12);            // mod date
  local.writeUInt32LE(crc, 14);
  local.writeUInt32LE(data.length, 18);  // compressed size
  local.writeUInt32LE(data.length, 22);  // uncompressed size
  local.writeUInt16LE(nameBuf.length, 26);
  local.writeUInt16LE(0, 28);            // extra len

  const localHeaderOffset = 0;
  const central = Buffer.alloc(46);
  central.writeUInt32LE(0x02014b50, 0);  // central dir sig
  central.writeUInt16LE(20, 4);          // version made by
  central.writeUInt16LE(20, 6);          // version needed
  central.writeUInt16LE(0, 8);
  central.writeUInt16LE(0, 10);          // method = STORE
  central.writeUInt16LE(0, 12);
  central.writeUInt16LE(0, 14);
  central.writeUInt32LE(crc, 16);
  central.writeUInt32LE(data.length, 20);
  central.writeUInt32LE(data.length, 24);
  central.writeUInt16LE(nameBuf.length, 28);
  central.writeUInt16LE(0, 30);          // extra
  central.writeUInt16LE(0, 32);          // comment
  central.writeUInt16LE(0, 34);          // disk
  central.writeUInt16LE(0, 36);          // internal attrs
  central.writeUInt32LE(0, 38);          // external attrs
  central.writeUInt32LE(localHeaderOffset, 42);

  const localBlock = Buffer.concat([local, nameBuf, data]);
  const centralBlock = Buffer.concat([central, nameBuf]);

  const eocd = Buffer.alloc(22);
  eocd.writeUInt32LE(0x06054b50, 0);     // EOCD sig
  eocd.writeUInt16LE(0, 4);
  eocd.writeUInt16LE(0, 6);
  eocd.writeUInt16LE(1, 8);              // entries on disk
  eocd.writeUInt16LE(1, 10);             // total entries
  eocd.writeUInt32LE(centralBlock.length, 12);
  eocd.writeUInt32LE(localBlock.length, 16); // central dir offset
  eocd.writeUInt16LE(0, 20);

  writeFileSync(zipPath, Buffer.concat([localBlock, centralBlock, eocd]));
  void zlib; // reserved for future DEFLATE fixtures
}

module.exports = { writeSingleEntryZip };
```

- [ ] **Step 2: Write the failing test**

`Ferret.Npm/test/extract.test.js`:
```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { writeSingleEntryZip } = require('./helpers/make-zip');
const { extract } = require('../lib/extract');

test('extract unpacks a zip entry into destDir', async () => {
  const base = path.join(os.tmpdir(), `ferret-extract-${process.pid}-${Math.round(performance.now())}`);
  await fsp.mkdir(base, { recursive: true });
  const zipPath = path.join(base, 'pkg.zip');
  const dest = path.join(base, 'out');
  writeSingleEntryZip(zipPath, 'ferret', 'BINARY');
  await extract(zipPath, dest);
  assert.strictEqual((await fsp.readFile(path.join(dest, 'ferret'))).toString(), 'BINARY');
  await fsp.rm(base, { recursive: true, force: true });
});
```

- [ ] **Step 3: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/extract.test.js`
Expected: FAIL — `Cannot find module '../lib/extract'`.

- [ ] **Step 4: Implement `lib/extract.js`**

```js
'use strict';

const extractZip = require('extract-zip');
const path = require('node:path');

// extract-zip requires an absolute destination directory.
async function extract(zipPath, destDir) {
  await extractZip(zipPath, { dir: path.resolve(destDir) });
}

module.exports = { extract };
```

- [ ] **Step 5: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/extract.test.js`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Ferret.Npm/lib/extract.js Ferret.Npm/test/extract.test.js Ferret.Npm/test/helpers/make-zip.js .claude/
git commit -m "feat(npm): zip extraction via extract-zip"
```

---

### Task 8: `scripts/install.js` — atomic install orchestrator

Wires manifest → download → verify → atomic install. Exports `install(opts)` for an integration test that exercises the full chain against a local HTTP server.

**Files:**
- Create: `Ferret.Npm/scripts/install.js`
- Test: `Ferret.Npm/test/install.test.js`

**Interfaces:**
- Consumes: `resolveRid`, `fetchManifest`, `selectAsset`, `downloadFile`, `verifyChecksum`, `extract`, `installDir`, `tempDir`, `releaseBaseUrl`.
- Produces: `install(opts?: {version?, platform?, env?, home?}): Promise<string>` — returns the final install dir. Module also self-invokes when run as the postinstall entry (skipped for the dev sentinel version `0.0.0`).

- [ ] **Step 1: Write the failing test**

`Ferret.Npm/test/install.test.js` (stands up a real HTTP server serving a manifest + zip, points `FERRET_DIST_RELEASE_ENDPOINT` at it, installs into a temp HOME, asserts the binary lands and is executable on POSIX):
```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const http = require('node:http');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const fs = require('node:fs');
const { createHash } = require('node:crypto');
const { writeSingleEntryZip } = require('./helpers/make-zip');

test('install downloads, verifies, and atomically installs the binary', async () => {
  const base = path.join(os.tmpdir(), `ferret-install-${process.pid}-${Math.round(performance.now())}`);
  await fsp.mkdir(base, { recursive: true });
  const home = path.join(base, 'home');
  await fsp.mkdir(home, { recursive: true });

  // Build a real zip asset and its manifest entry.
  const zipPath = path.join(base, 'Ferret-1.2.3-linux-x64.zip');
  writeSingleEntryZip(zipPath, 'ferret', '#!/bin/sh\necho ferret-stub\n');
  const sha = createHash('sha256').update(await fsp.readFile(zipPath)).digest('hex');
  const manifest = {
    schemaVersion: 1, version: '1.2.3', releaseTag: 'v1.2.3', published: '2026-06-30',
    minimumInstallerSchema: 1,
    assets: [{ rid: 'linux-x64', file: 'Ferret-1.2.3-linux-x64.zip', size: fs.statSync(zipPath).size, sha256: sha, binary: 'ferret' }],
  };

  const server = http.createServer((req, res) => {
    if (req.url.endsWith('release-manifest.json')) { res.end(JSON.stringify(manifest)); return; }
    if (req.url.endsWith('Ferret-1.2.3-linux-x64.zip')) { res.end(fs.readFileSync(zipPath)); return; }
    res.statusCode = 404; res.end('nope');
  });
  await new Promise((r) => server.listen(0, r));
  const port = server.address().port;
  process.env.FERRET_DIST_RELEASE_ENDPOINT = `http://127.0.0.1:${port}/download`;
  delete require.cache[require.resolve('../lib/distribution-config')];
  delete require.cache[require.resolve('../lib/manifest')];
  delete require.cache[require.resolve('../scripts/install')];
  const { install } = require('../scripts/install');

  const finalDir = await install({ version: '1.2.3', platform: 'linux', env: {}, home });
  const binPath = path.join(finalDir, 'ferret');
  assert.ok(fs.existsSync(binPath), 'binary installed');
  assert.strictEqual((fs.statSync(binPath).mode & 0o100) !== 0, true, 'binary is executable');

  server.close();
  delete process.env.FERRET_DIST_RELEASE_ENDPOINT;
  await fsp.rm(base, { recursive: true, force: true });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/install.test.js`
Expected: FAIL — `Cannot find module '../scripts/install'`.

- [ ] **Step 3: Implement `scripts/install.js`**

```js
'use strict';

const path = require('node:path');
const fsp = require('node:fs/promises');
const { resolveRid } = require('../lib/platform');
const { fetchManifest, selectAsset } = require('../lib/manifest');
const { downloadFile } = require('../lib/download');
const { verifyChecksum } = require('../lib/verify');
const { extract } = require('../lib/extract');
const { installDir, tempDir } = require('../lib/paths');
const { releaseBaseUrl } = require('../lib/distribution-config');
const pkg = require('../package.json');

async function install(opts = {}) {
  const version = opts.version || pkg.version;
  const platform = opts.platform || process.platform;
  const env = opts.env || process.env;
  const home = opts.home; // undefined → paths default to os.homedir()
  const tag = `v${version}`;
  const rid = resolveRid(platform, opts.arch);

  console.log(`Installing Ferret ${version} (${rid})...`);
  const manifest = await fetchManifest(tag);
  const asset = selectAsset(manifest, rid);

  const tmp = tempDir();
  await fsp.rm(tmp, { recursive: true, force: true });
  await fsp.mkdir(tmp, { recursive: true });

  const zipPath = path.join(tmp, asset.file);
  const url = `${releaseBaseUrl(tag)}/${asset.file}`;
  const sizeMb = asset.size ? ` (${(asset.size / 1e6).toFixed(1)} MB)` : '';
  console.log(`Downloading ${asset.file}${sizeMb}...`);
  await downloadFile(url, zipPath);
  await verifyChecksum(zipPath, asset.sha256);

  // Atomic install: extract to staging, then swap into the final dir.
  const finalDir = installDir(platform, env, home);
  const stagingDir = path.join(tmp, 'staging');
  await fsp.rm(stagingDir, { recursive: true, force: true });
  await fsp.mkdir(stagingDir, { recursive: true });
  await extract(zipPath, stagingDir);

  await fsp.rm(finalDir, { recursive: true, force: true });
  await fsp.mkdir(path.dirname(finalDir), { recursive: true });
  try {
    await fsp.rename(stagingDir, finalDir);
  } catch (err) {
    if (err.code !== 'EXDEV') throw err; // cross-device: copy then remove
    await fsp.cp(stagingDir, finalDir, { recursive: true });
    await fsp.rm(stagingDir, { recursive: true, force: true });
  }

  if (platform !== 'win32') {
    await fsp.chmod(path.join(finalDir, asset.binary), 0o755);
  }

  await fsp.rm(tmp, { recursive: true, force: true });
  console.log(`Ferret installed to ${finalDir}`);
  return finalDir;
}

// Postinstall entry. Skip for the dev sentinel version so `npm install` inside
// the package checkout does not try to fetch a v0.0.0 release.
if (require.main === module) {
  if (pkg.version === '0.0.0') {
    console.log('Ferret (dev build): skipping binary install.');
    process.exit(0);
  }
  install().catch((err) => {
    console.error(`\nFerret installation failed: ${err.message}\n`);
    process.exit(1);
  });
}

module.exports = { install };
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/install.test.js`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/scripts/install.js Ferret.Npm/test/install.test.js .claude/
git commit -m "feat(npm): atomic install orchestrator (manifest→download→verify→swap)"
```

---

### Task 9: `bin/ferret.js` — launcher

Locates the installed binary and forwards all arguments, stdio, and the exit code.

**Files:**
- Create: `Ferret.Npm/bin/ferret.js`
- Test: `Ferret.Npm/test/launcher.test.js`

**Interfaces:**
- Consumes: `installDir` from `paths`.
- Behavior: spawns `<installDir>/ferret[.exe]` with `process.argv.slice(2)`, `stdio: 'inherit'`; exits with the child's status; prints an actionable error and exits 1 if the binary is missing.

- [ ] **Step 1: Write the failing test**

`Ferret.Npm/test/launcher.test.js` (runs the launcher as a child process so we can assert exit code + forwarding; POSIX-guarded executable test, cross-platform missing-binary test):
```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const { spawnSync } = require('node:child_process');

const LAUNCHER = path.join(__dirname, '..', 'bin', 'ferret.js');

test('launcher exits 1 with a helpful message when the binary is missing', () => {
  const home = path.join(os.tmpdir(), `ferret-missing-${process.pid}-${Math.round(performance.now())}`);
  const res = spawnSync(process.execPath, [LAUNCHER, '--version'], {
    env: { ...process.env, HOME: home, USERPROFILE: home, LOCALAPPDATA: path.join(home, 'AppData', 'Local') },
    encoding: 'utf8',
  });
  assert.strictEqual(res.status, 1);
  assert.match(res.stderr, /Ferret binary not found/);
});

test('launcher forwards args and propagates exit code (POSIX)', { skip: process.platform === 'win32' }, async () => {
  const home = path.join(os.tmpdir(), `ferret-run-${process.pid}-${Math.round(performance.now())}`);
  const installDir = path.join(home, '.local', 'share', 'ferret');
  await fsp.mkdir(installDir, { recursive: true });
  const fake = path.join(installDir, 'ferret');
  await fsp.writeFile(fake, '#!/bin/sh\necho "got:$1"\nexit 7\n');
  await fsp.chmod(fake, 0o755);
  const res = spawnSync(process.execPath, [LAUNCHER, 'hello'], {
    env: { ...process.env, HOME: home }, encoding: 'utf8',
  });
  assert.match(res.stdout, /got:hello/);
  assert.strictEqual(res.status, 7);
  await fsp.rm(home, { recursive: true, force: true });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/launcher.test.js`
Expected: FAIL — `Cannot find module '../bin/ferret.js'` (and the missing-binary test errors).

- [ ] **Step 3: Implement `bin/ferret.js`**

```js
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
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/launcher.test.js`
Expected: PASS (missing-binary test always; forwarding test on POSIX, skipped on Windows).

- [ ] **Step 5: Commit**

```bash
git add Ferret.Npm/bin/ferret.js Ferret.Npm/test/launcher.test.js .claude/
git commit -m "feat(npm): launcher forwards args, stdio, and exit code to the native binary"
```

---

### Task 10: `scripts/uninstall.js` — preuninstall cleanup

Removes only the install dir and temp dir; never touches `.ferret` user data.

**Files:**
- Create: `Ferret.Npm/scripts/uninstall.js`
- Test: `Ferret.Npm/test/uninstall.test.js`

**Interfaces:**
- Consumes: `installDir`, `tempDir`.
- Produces: `uninstall(opts?: {platform?, env?, home?}): Promise<void>`. Self-invokes when run as the preuninstall entry.

- [ ] **Step 1: Write the failing test**

```js
'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const os = require('node:os');
const path = require('node:path');
const fsp = require('node:fs/promises');
const fs = require('node:fs');
const { uninstall } = require('../scripts/uninstall');

test('uninstall removes the binary dir but preserves .ferret workspace data', async () => {
  const home = path.join(os.tmpdir(), `ferret-uninstall-${process.pid}-${Math.round(performance.now())}`);
  const installed = path.join(home, '.local', 'share', 'ferret');
  await fsp.mkdir(installed, { recursive: true });
  await fsp.writeFile(path.join(installed, 'ferret'), 'bin');

  const workspace = path.join(home, 'project', '.ferret');
  await fsp.mkdir(workspace, { recursive: true });
  await fsp.writeFile(path.join(workspace, 'index.db'), 'data');

  await uninstall({ platform: 'linux', env: {}, home });

  assert.strictEqual(fs.existsSync(installed), false, 'install dir removed');
  assert.strictEqual(fs.existsSync(path.join(workspace, 'index.db')), true, 'workspace preserved');
  await fsp.rm(home, { recursive: true, force: true });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `cd Ferret.Npm && node --test test/uninstall.test.js`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement `scripts/uninstall.js`**

```js
'use strict';

const fsp = require('node:fs/promises');
const { installDir, tempDir } = require('../lib/paths');

async function uninstall(opts = {}) {
  const dir = installDir(opts.platform, opts.env, opts.home);
  await fsp.rm(dir, { recursive: true, force: true });
  await fsp.rm(tempDir(), { recursive: true, force: true });
  console.log(
    'Ferret binary removed. Your .ferret workspaces, indexes, and config were left untouched.'
  );
}

if (require.main === module) {
  uninstall().catch((err) => {
    console.error(err.message);
    process.exit(1);
  });
}

module.exports = { uninstall };
```

- [ ] **Step 4: Run to verify it passes**

Run: `cd Ferret.Npm && node --test test/uninstall.test.js`
Expected: PASS.

- [ ] **Step 5: Run the full wrapper suite + lint/format**

Run: `cd Ferret.Npm && npm test && npm run lint && npm run format`
Expected: all tests PASS; eslint clean; prettier reports formatted (fix with `npx prettier --write .` if needed, then re-commit).

- [ ] **Step 6: Commit**

```bash
git add Ferret.Npm/scripts/uninstall.js Ferret.Npm/test/uninstall.test.js .claude/
git commit -m "feat(npm): preuninstall removes binary only, preserves user data"
```

---

### Task 11: `build-release-manifest.ps1` — manifest generator

Generates the top-level `SHA256SUMS.txt` and `release-manifest.json` from the per-RID zips. Keeps `package.ps1` single-RID; this aggregates.

**Files:**
- Create: `scripts/build-release-manifest.ps1`
- Test: `scripts/tests/build-release-manifest.Tests.ps1`

**Interfaces:**
- Inputs: `-Version <string>` (required), `-ArtifactsDir <path>` (default `./artifacts`), `-Published <yyyy-MM-dd>` (default today UTC), `-ReleaseTag <string>` (default `v<Version>`).
- Outputs: `<ArtifactsDir>/SHA256SUMS.txt` and `<ArtifactsDir>/release-manifest.json`.

- [ ] **Step 1: Write the failing test**

`scripts/tests/build-release-manifest.Tests.ps1`:
```powershell
#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptsDir = Split-Path -Parent $PSScriptRoot
$gen = Join-Path $scriptsDir "build-release-manifest.ps1"

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("ferret-manifest-test-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $work -Force | Out-Null
try {
    "win-binary"   | Set-Content (Join-Path $work "Ferret-9.9.9-win-x64.zip")
    "linux-binary" | Set-Content (Join-Path $work "Ferret-9.9.9-linux-x64.zip")

    & $gen -Version "9.9.9" -ArtifactsDir $work -Published "2026-06-30"

    $manifestPath = Join-Path $work "release-manifest.json"
    $sumsPath     = Join-Path $work "SHA256SUMS.txt"
    if (-not (Test-Path $manifestPath)) { throw "release-manifest.json not created" }
    if (-not (Test-Path $sumsPath))     { throw "SHA256SUMS.txt not created" }

    $m = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($m.schemaVersion -ne 1)             { throw "schemaVersion != 1" }
    if ($m.version -ne "9.9.9")             { throw "version mismatch" }
    if ($m.releaseTag -ne "v9.9.9")         { throw "releaseTag mismatch" }
    if ($m.minimumInstallerSchema -ne 1)    { throw "minimumInstallerSchema != 1" }
    if ($m.metadata.generator -ne "build-release-manifest.ps1") { throw "metadata.generator wrong" }
    if (@($m.assets).Count -ne 2)           { throw "expected 2 assets, got $(@($m.assets).Count)" }

    $win = @($m.assets) | Where-Object { $_.rid -eq "win-x64" }
    if ($win.binary -ne "ferret.exe")       { throw "win binary name wrong: $($win.binary)" }
    if ([int64]$win.size -le 0)             { throw "win size not set" }

    $expected = (Get-FileHash (Join-Path $work "Ferret-9.9.9-win-x64.zip") -Algorithm SHA256).Hash.ToLower()
    if ($win.sha256 -ne $expected)          { throw "win sha256 mismatch" }
    if (-not ((Get-Content $sumsPath) -match "Ferret-9.9.9-win-x64.zip")) { throw "SHA256SUMS missing win entry" }

    Write-Host "PASS: build-release-manifest.ps1 tests"
} finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
```

- [ ] **Step 2: Run to verify it fails**

Run (Windows local): `powershell -ExecutionPolicy Bypass -File scripts/tests/build-release-manifest.Tests.ps1`
(CI/Linux: `pwsh -File scripts/tests/build-release-manifest.Tests.ps1`)
Expected: FAIL — generator script does not exist.

- [ ] **Step 3: Implement `scripts/build-release-manifest.ps1`**

```powershell
#Requires -Version 5.1
<#
.SYNOPSIS
    Generate SHA256SUMS.txt and release-manifest.json for a Ferret release.
.DESCRIPTION
    Scans ArtifactsDir for Ferret-<Version>-<rid>.zip files, computes SHA256 and
    size for each, and writes the top-level checksum manifest and the
    Distribution Platform public contract (release-manifest.json).
.PARAMETER Version
    Release version (e.g. "0.14.0" or "0.14.0-rc1").
.PARAMETER ArtifactsDir
    Directory containing the per-RID zips. Defaults to <repo>/artifacts.
.PARAMETER Published
    Date string (yyyy-MM-dd) for the manifest. Defaults to today (UTC).
.PARAMETER ReleaseTag
    Git tag. Defaults to "v<Version>".
#>
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$ArtifactsDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts"),
    [string]$Published = "",
    [string]$ReleaseTag = ""
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $ReleaseTag) { $ReleaseTag = "v$Version" }
if (-not $Published)  { $Published  = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd") }

$SchemaVersion = 1
$RidOrder = @("win-x64", "linux-x64", "osx-arm64", "osx-x64")

$zips = Get-ChildItem -Path $ArtifactsDir -Filter "Ferret-$Version-*.zip" -File | Sort-Object Name
if ($zips.Count -eq 0) {
    Write-Error "No Ferret-$Version-*.zip files found in $ArtifactsDir."
    exit 1
}

$assets = @()
$sumsLines = @()
$escaped = [regex]::Escape($Version)
foreach ($zip in $zips) {
    if ($zip.Name -notmatch "^Ferret-$escaped-(.+)\.zip$") { continue }
    $rid = $Matches[1]
    $hash = (Get-FileHash $zip.FullName -Algorithm SHA256).Hash.ToLower()
    $binary = if ($rid -like "win-*") { "ferret.exe" } else { "ferret" }
    $assets += [ordered]@{
        rid    = $rid
        file   = $zip.Name
        size   = [int64]$zip.Length
        sha256 = $hash
        binary = $binary
    }
    $sumsLines += "$hash  $($zip.Name)"
}

$assets = $assets | Sort-Object `
    @{ Expression = { $i = $RidOrder.IndexOf($_.rid); if ($i -lt 0) { 999 } else { $i } } }, `
    @{ Expression = { $_.rid } }

$manifest = [ordered]@{
    schemaVersion          = $SchemaVersion
    version                = $Version
    releaseTag             = $ReleaseTag
    published              = $Published
    minimumInstallerSchema = 1
    metadata               = [ordered]@{ generator = "build-release-manifest.ps1"; generatorVersion = "1" }
    assets                 = @($assets)
}

$sumsPath = Join-Path $ArtifactsDir "SHA256SUMS.txt"
Set-Content -Path $sumsPath -Value $sumsLines -Encoding ascii

$manifestPath = Join-Path $ArtifactsDir "release-manifest.json"
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $manifestPath -Encoding ascii

Write-Host "Wrote $sumsPath"
Write-Host "Wrote $manifestPath"
Get-Content $manifestPath
```

- [ ] **Step 4: Run to verify it passes**

Run (Windows): `powershell -ExecutionPolicy Bypass -File scripts/tests/build-release-manifest.Tests.ps1`
Expected: `PASS: build-release-manifest.ps1 tests`.

- [ ] **Step 5: Commit**

```bash
git add scripts/build-release-manifest.ps1 scripts/tests/build-release-manifest.Tests.ps1 .claude/
git commit -m "feat(release): generate SHA256SUMS.txt and release-manifest.json"
```

---

### Task 12: Extend `release.yml` to publish binary assets + manifest

Adds binary packaging and manifest generation to the release job and uploads the new assets alongside the existing `.nupkg`.

**Files:**
- Modify: `.github/workflows/release.yml`

**Interfaces:**
- Consumes: `package.ps1`, `scripts/build-release-manifest.ps1`, `steps.version.outputs.VERSION`.
- Produces: GitHub Release assets `Ferret-<version>-<rid>.zip` (×4), `SHA256SUMS.txt`, `release-manifest.json`.

- [ ] **Step 1: Add a `DATE` output to the version step**

In `.github/workflows/release.yml`, in the `Resolve version` step (`id: version`), append after the existing `echo` lines (before the closing of the `run: |` block):
```bash
          echo "DATE=$(date -u +%Y-%m-%d)" >> "$GITHUB_OUTPUT"
```

- [ ] **Step 2: Add packaging + manifest steps after the `Pack` step**

Insert these two steps immediately after the existing `- name: Pack` step and before `- name: Create GitHub Release`:
```yaml
      - name: Build distribution packages (all RIDs)
        shell: pwsh
        run: |
          $rids = @('win-x64','linux-x64','osx-arm64','osx-x64')
          foreach ($rid in $rids) {
            ./package.ps1 -Version '${{ steps.version.outputs.VERSION }}' -Rid $rid
          }

      - name: Build release manifest
        shell: pwsh
        run: |
          ./scripts/build-release-manifest.ps1 `
            -Version '${{ steps.version.outputs.VERSION }}' `
            -ArtifactsDir './artifacts' `
            -Published '${{ steps.version.outputs.DATE }}'
```

- [ ] **Step 3: Extend the upload `files` list**

Replace the `files:` line in the `Create GitHub Release` step:
```yaml
        files: ./artifacts/*.nupkg
```
with:
```yaml
        files: |
          ./artifacts/*.nupkg
          ./artifacts/Ferret-*.zip
          ./artifacts/SHA256SUMS.txt
          ./artifacts/release-manifest.json
```

- [ ] **Step 4: Verify locally (the pipeline steps reproduce the same artifacts)**

Run (Windows, from repo root):
```bash
pwsh -NoProfile -Command "$rids=@('win-x64','linux-x64','osx-arm64','osx-x64'); foreach($r in $rids){ ./package.ps1 -Version 0.14.0 -Rid $r }; ./scripts/build-release-manifest.ps1 -Version 0.14.0 -ArtifactsDir ./artifacts -Published 2026-06-30"
```
Expected: four `artifacts/Ferret-0.14.0-<rid>.zip`, plus `artifacts/SHA256SUMS.txt` and `artifacts/release-manifest.json` with four asset entries. (If `actionlint` is installed, also run `actionlint .github/workflows/release.yml` and expect no errors.)

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/release.yml .claude/
git commit -m "ci(release): publish per-RID binary zips, SHA256SUMS, and release-manifest.json"
```

---

### Task 13: `npm-publish.yml` — publish the wrapper on release

Publishes `@indoulia/ferret` when a release is **published** (not on draft creation), so the assets the postinstall needs are already live. Version is set from the tag.

**Files:**
- Create: `.github/workflows/npm-publish.yml`

**Interfaces:**
- Trigger: `release: types: [published]` and `workflow_dispatch`.
- Secret required: `NPM_TOKEN` (npm automation token with publish rights on the `@indoulia` scope).

- [ ] **Step 1: Create `.github/workflows/npm-publish.yml`**

```yaml
name: Publish NPM

on:
  release:
    types: [published]
  workflow_dispatch:
    inputs:
      version:
        description: 'Version to publish (e.g. 0.14.0)'
        required: true

jobs:
  publish:
    name: Publish @indoulia/ferret
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          registry-url: 'https://registry.npmjs.org'

      - name: Resolve version
        id: v
        run: |
          if [ "${{ github.event_name }}" = "workflow_dispatch" ]; then
            V="${{ github.event.inputs.version }}"
          else
            V="${GITHUB_REF#refs/tags/v}"
          fi
          echo "VERSION=$V" >> "$GITHUB_OUTPUT"

      - name: Set package version
        working-directory: Ferret.Npm
        run: npm version "${{ steps.v.outputs.VERSION }}" --no-git-tag-version --allow-same-version

      - name: Install (no scripts)
        working-directory: Ferret.Npm
        run: npm ci --ignore-scripts

      - name: Test
        working-directory: Ferret.Npm
        run: npm test

      - name: Publish
        working-directory: Ferret.Npm
        run: npm publish --access public
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

- [ ] **Step 2: Verify the package publishes cleanly (dry run)**

Run (from repo root):
```bash
cd Ferret.Npm && npm version 0.14.0 --no-git-tag-version --allow-same-version && npm ci --ignore-scripts && npm test && npm publish --dry-run --access public
```
Expected: `npm publish --dry-run` lists `bin/`, `lib/`, `scripts/`, `package.json` in the tarball and exits 0. Then reset the dev version:
```bash
npm version 0.0.0 --no-git-tag-version --allow-same-version
```
(If `actionlint` is installed: `actionlint .github/workflows/npm-publish.yml` → no errors.)

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/npm-publish.yml Ferret.Npm/package.json Ferret.Npm/package-lock.json .claude/
git commit -m "ci(npm): publish @indoulia/ferret on release published"
```

> **Operator note (not a code step):** before the first real release, add the `NPM_TOKEN` secret to the repository and ensure the `@indoulia` org/scope exists on npm. The release flow is: push tag `vX` → `release.yml` builds the **draft** release with assets → a human publishes the draft → `npm-publish.yml` fires and publishes the wrapper.

---

### Task 14: Documentation — ARCH-022 + NPM README

Records the architecture and the user-facing install/upgrade/uninstall/failure-recovery docs.

**Files:**
- Create: `docs/002-Architecture/ARCH-022-Distribution-Platform-Architecture.md`
- Create: `Ferret.Npm/README.md`

- [ ] **Step 1: Write `ARCH-022-Distribution-Platform-Architecture.md`**

Open `docs/002-Architecture/ARCH-021-AI-Platform-Architecture.md` and mirror its heading structure and front-matter style. The document MUST contain these sections with this substance:

- **Status / Context:** Ferret needs a repeatable way to deliver binaries. Today only `.nupkg` is published; binaries are not consumable. GitHub Releases become the single source of truth.
- **Decision 1 — Distribution Principle:** GitHub Releases are the single source of truth; every consumer reads `release-manifest.json` and never infers filenames, scrapes HTML, or enumerates assets. Own-vs-reuse boundary (own: platform detection, binary selection, version resolution, manifest parsing, install workflow; reuse: `extract-zip`, Node `crypto`, Node `fetch`). Distribution tooling has a **maximum of 2 runtime dependencies**.
- **Decision 2 — Repository-agnostic configuration:** no hardcoded URLs or hosting assumptions; config keys `owner` / `repository` / optional `releaseEndpoint`; `IReleaseSource` reserved as a concept (GitHub Enterprise, Azure DevOps, internal CDN, enterprise mirror) but not implemented.
- **Decision 3 — Release manifest as a versioned public API:** document the schema (`schemaVersion`, `version`, `releaseTag`, `published`, `minimumInstallerSchema`, `metadata`, `assets[]` with `rid`/`file`/`size`/`sha256`/`binary`) and the compatibility rule (installer supports `schemaVersion` 1; refuses `minimumInstallerSchema` greater than supported). Reserve `ferret self-update` as a future consumer of the same manifest.
- **Pipeline:** Build → Tests → NuGet → Binary Packages → Manifest → GitHub Release (additive; existing `.nupkg` preserved). Single Ubuntu runner; macOS artifacts unsigned/unnotarized (known limitation).
- **Consumers diagram:** NPM (first), then Homebrew / winget / Chocolatey / Scoop / Enterprise mirror.
- **Reference:** link to `docs/superpowers/specs/2026-06-30-distribution-platform-design.md`.

- [ ] **Step 2: Write `Ferret.Npm/README.md`**

Must contain runnable sections (use the exact commands below):

````markdown
# @indoulia/ferret

Install Ferret via npm. This package downloads the official, checksum-verified
Ferret binary for your platform from GitHub Releases — it does not bundle or
build anything.

## Install
```bash
npm install -g @indoulia/ferret
ferret --version
```

## Upgrade
```bash
npm update -g @indoulia/ferret
```

## Uninstall
```bash
npm uninstall -g @indoulia/ferret
```
Removes only the Ferret binary. Your `.ferret` workspaces, indexes, and
configuration are left untouched.

## Where the binary is installed
- Windows: `%LOCALAPPDATA%\Programs\Ferret`
- macOS: `~/Library/Application Support/Ferret`
- Linux: `~/.local/share/ferret`

## Failure recovery
- **Checksum mismatch / corrupted download:** rerun `npm install -g @indoulia/ferret`. The installer is atomic — a failed install never overwrites a working one.
- **"Ferret binary not found":** the postinstall did not complete; rerun the install command.
- **Unsupported platform:** supported targets are Windows x64, Linux x64, macOS arm64, macOS x64.
- **macOS Gatekeeper prompt:** macOS builds are currently unsigned; allow the binary in System Settings → Privacy & Security.
- **Behind a firewall / no GitHub access:** point the installer at a mirror with `FERRET_DIST_RELEASE_ENDPOINT=<base-url>` (and optionally `FERRET_DIST_OWNER` / `FERRET_DIST_REPO`).

## Configuration (advanced)
| Env var | Default | Purpose |
| --- | --- | --- |
| `FERRET_DIST_OWNER` | `indoulia` | GitHub owner |
| `FERRET_DIST_REPO` | `Ferret` | Repository name |
| `FERRET_DIST_RELEASE_ENDPOINT` | _(unset)_ | Custom release base URL (enterprise mirror) |
````

- [ ] **Step 3: Commit**

```bash
git add docs/002-Architecture/ARCH-022-Distribution-Platform-Architecture.md Ferret.Npm/README.md .claude/
git commit -m "docs: ARCH-022 Distribution Platform + NPM install/upgrade/uninstall/recovery"
```

---

### Task 15: End-to-end validation on a clean target (acceptance gate)

Proves the full chain works against real artifacts. This is the milestone acceptance gate; it is a manual verification task with no production code.

**Files:** none (verification only).

- [ ] **Step 1: Build real release artifacts locally**

Run (Windows, repo root):
```bash
pwsh -NoProfile -Command "$rids=@('win-x64','linux-x64','osx-arm64','osx-x64'); foreach($r in $rids){ ./package.ps1 -Version 0.14.0 -Rid $r }; ./scripts/build-release-manifest.ps1 -Version 0.14.0 -ArtifactsDir ./artifacts -Published 2026-06-30"
```
Expected: four zips + `SHA256SUMS.txt` + `release-manifest.json` in `artifacts/`.

- [ ] **Step 2: Serve the artifacts and install through the wrapper end-to-end**

This simulates a clean machine without touching the real npm registry. From the repo root, start a static file server rooted so that `<base>/v0.14.0/<asset>` resolves to the files in `artifacts/`, then drive the wrapper's `install()` against it:
```bash
node -e "const http=require('node:http'),fs=require('node:fs'),path=require('node:path');const dir=path.resolve('artifacts');http.createServer((q,s)=>{const f=path.join(dir,path.basename(q.url));fs.existsSync(f)?s.end(fs.readFileSync(f)):(s.statusCode=404,s.end());}).listen(8099,()=>console.log('serving artifacts on :8099'));" &
cd Ferret.Npm && FERRET_DIST_RELEASE_ENDPOINT="http://127.0.0.1:8099" node -e "require('./scripts/install').install({version:'0.14.0'}).then(d=>console.log('installed at',d)).catch(e=>{console.error(e);process.exit(1)})"
```
Expected: "installed at &lt;platform install dir&gt;"; the `ferret`/`ferret.exe` binary exists there. Stop the background server afterward.

- [ ] **Step 3: Confirm the launcher runs the installed binary**

Run (from `Ferret.Npm`):
```bash
node bin/ferret.js --version
```
Expected: Ferret prints its version (`0.14.0`), proving the launcher locates and forwards to the installed binary.

- [ ] **Step 4: Record the result and clean up**

Append a short PASS/FAIL note (date, platform, observed version) to the milestone validation notes. Remove the locally installed binary to leave a clean state:
```bash
cd Ferret.Npm && node scripts/uninstall.js
```
Expected: "Ferret binary removed..." and the install dir is gone.

- [ ] **Step 5: Commit (validation notes only, if any were added)**

```bash
git add docs/ .claude/
git commit -m "docs: record Distribution Platform end-to-end validation result"
```

---

## Self-Review

**Spec coverage:**
- Extend `release.yml` to publish per-RID zips → Task 12 ✓
- Top-level `SHA256SUMS.txt` + `release-manifest.json` → Task 11 (generator) + Task 12 (upload) ✓
- Manifest schema incl. `size`, `metadata`, `minimumInstallerSchema`, `releaseTag` → Task 11 (producer) + Task 3 (consumer) ✓
- NPM wrapper: platform detect → Task 2; manifest read → Task 3; download → Task 6; verify → Task 4; extract → Task 7; atomic install → Task 8; launcher → Task 9 ✓
- Repository-agnostic config, no hardcoded URLs, optional `releaseEndpoint` → Task 1 ✓
- Install locations (`%LOCALAPPDATA%\Programs\Ferret`, etc.) → Task 5 ✓
- Atomic install (staging → rename) → Task 8 ✓
- Uninstall preserves user data → Task 10 ✓
- Version-locked publish on release published; draft-release sequencing → Task 13 ✓
- Dependency policy (extract-zip only) → Task 1 package.json + Global Constraints ✓
- `node:test` unit coverage + clean-machine E2E → Tasks 2–10 + Task 15 ✓
- ADRs (Distribution Principle, repo-agnostic config, manifest-as-public-API) + `IReleaseSource` + `ferret self-update` reserved → Task 14 ✓
- User docs (install/upgrade/uninstall/failure recovery) → Task 14 ✓
- macOS unsigned limitation documented → Task 14 ✓

**Placeholder scan:** No TBD/TODO; every code step contains complete code; the docs task lists exact required sections and commands.

**Type consistency:** `releaseBaseUrl(tag)`, `resolveRid(platform,arch)`, `fetchManifest(tag,fetchImpl)`, `selectAsset(manifest,rid)`, `verifyChecksum(path,sha)`, `downloadFile(url,dest,fetchImpl,attempts)`, `extract(zip,dir)`, `installDir(platform,env,home)`, `tempDir()`, `install(opts)`, `uninstall(opts)` are used identically across producer/consumer/test tasks. The manifest field set is identical in Task 11 (writer) and Task 3 (reader).
