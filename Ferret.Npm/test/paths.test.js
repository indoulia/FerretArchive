'use strict';
const { test } = require('node:test');
const assert = require('node:assert');
const path = require('node:path');
const { installDir } = require('../lib/paths');

test('Windows uses LOCALAPPDATA\\Programs\\Ferret', () => {
    const dir = installDir(
        'win32',
        { LOCALAPPDATA: 'C:\\Users\\u\\AppData\\Local' },
        'C:\\Users\\u'
    );
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
