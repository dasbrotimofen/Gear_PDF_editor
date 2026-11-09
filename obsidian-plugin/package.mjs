// SPDX-License-Identifier: AGPL-3.0-or-later
import { mkdir, copyFile } from 'node:fs/promises';
await mkdir('release/gear-viewer', { recursive: true });
for (const name of ['main.js', 'manifest.json', 'styles.css', 'LICENSE', 'README.md']) {
  await copyFile(name, `release/gear-viewer/${name}`);
}
console.log('Copy release/gear-viewer to <vault>/.obsidian/plugins/gear-viewer');
