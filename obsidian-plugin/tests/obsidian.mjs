// SPDX-License-Identifier: AGPL-3.0-or-later
// Optional real-app check. Uses a disposable vault and separate Electron profile.
import { chromium } from '@playwright/test';
import { mkdir, writeFile, copyFile, readFile, rename } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import { createHash } from 'node:crypto';
import path from 'node:path';
import assert from 'node:assert/strict';

const vault = path.resolve('test-output/obsidian-vault');
const profile = path.resolve('test-output/obsidian-profile');
const config = path.join(vault, '.obsidian');
const plugin = path.join(config, 'plugins/gear-viewer');
await mkdir(plugin, { recursive: true });
await mkdir(profile, { recursive: true });
for (const name of ['main.js', 'manifest.json', 'styles.css']) await copyFile(name, path.join(plugin, name));
const original = await readFile('test-output/comparison.gear');
await writeFile(path.join(vault, 'comparison.gear'), original);
await writeFile(path.join(config, 'app.json'), JSON.stringify({ restrictedMode: false }));
await writeFile(path.join(config, 'community-plugins.json'), JSON.stringify(['gear-viewer']));
await writeFile(path.join(config, 'core-plugins.json'), JSON.stringify(['file-explorer']));
await writeFile(path.join(profile, 'obsidian.json'), JSON.stringify({ vaults: { gearviewertest: { path: vault, ts: Date.now(), open: true } } }));
const executable = process.env.OBSIDIAN_EXE || path.join(process.env.LOCALAPPDATA, 'Programs/obsidian/Obsidian.exe');
const child = spawn(executable, [`--user-data-dir=${profile}`, '--remote-debugging-port=19383', '--no-first-run'], { windowsHide: true, stdio: 'ignore' });
let browser;
try {
  for (let i = 0; i < 60; i++) {
    try { const response = await fetch('http://127.0.0.1:19383/json/version'); if (response.ok) break; } catch {}
    await new Promise(resolve => setTimeout(resolve, 1000));
  }
  browser = await chromium.connectOverCDP('http://127.0.0.1:19383');
  const page = browser.contexts()[0].pages()[0];
  page.on('console', msg => { if (msg.type() === 'error') console.log('Obsidian:', msg.text()); });
  await page.waitForFunction(() => window.app?.workspace?.layoutReady, { timeout: 30000 });
  // Obsidian exposes app in its developer console. This is test automation only;
  // the shipped plugin uses exclusively the public API.
  console.log('Obsidian runtime:', await page.evaluate(() => navigator.userAgent));
  const trust = page.getByRole('button', { name: 'Trust author and enable plugins', exact: true });
  if (await trust.count()) await trust.click();
  await page.waitForFunction(() => !!app.plugins.plugins['gear-viewer']);
  await page.locator('.nav-file-title[data-path="comparison.gear"]').waitFor({ state: 'visible', timeout: 15000 });
  await page.locator('.nav-file-title[data-path="comparison.gear"]').click();
  await page.locator('.gear-viewer-page').nth(2).waitFor({ state: 'attached', timeout: 15000 });
  assert.equal(await page.locator('.gear-viewer-page').count(), 3);
  assert.equal(await page.evaluate(() => app.workspace.getLeavesOfType('gear-viewer').length), 1);
  const sizeCheck = await page.locator('.gear-viewer').evaluate(el => {
    const pages = Array.from(el.querySelectorAll('svg'));
    const rectangles = pages.map(p => p.getBoundingClientRect());
    return rectangles.every((r, i) => Math.abs(r.width / r.height - [210 / 297, 297 / 210, 1][i]) < .01)
      && rectangles[1].top > rectangles[0].bottom && rectangles[2].top > rectangles[1].bottom
      && el.scrollHeight > el.clientHeight && el.scrollWidth <= el.clientWidth;
  });
  assert.ok(sizeCheck, 'ordered, proportional, width-fit scrolling pages');
  const hash = buffer => createHash('sha256').update(buffer).digest('hex');
  assert.equal(hash(await readFile(path.join(vault, 'comparison.gear'))), hash(original));
  // Save externally, rather than triggering a synthetic Obsidian event.
  const replacement = path.join(vault, 'comparison.gear.test.tmp');
  await writeFile(replacement, '{"Version":3,"Pages":[{"WidthMm":100,"HeightMm":200}]}');
  // GEAR saves by replacing the destination with a temporary file.
  await rename(replacement, path.join(vault, 'comparison.gear'));
  await page.waitForFunction(() => document.querySelector('.gear-viewer-page')?.getAttribute('viewBox') === '0 0 100 200');
  assert.equal(await page.locator('.gear-viewer-page').count(), 1);
  await writeFile(path.join(vault, 'comparison.gear'), original);
  await page.waitForFunction(() => document.querySelectorAll('.gear-viewer-page').length === 3);
  await page.screenshot({ path: 'test-output/obsidian-preview.png' });
  // Check malformed and unsupported files, then recovery from external save.
  for (const [text, message] of [['{', 'Malformed'], ['{"Version":99}', 'Unsupported']]) {
    await writeFile(path.join(vault, 'comparison.gear'), text);
    await page.waitForFunction(message => document.querySelector('.gear-viewer-error')?.textContent.includes(message), message);
  }
  await writeFile(path.join(vault, 'comparison.gear'), original);
  await page.waitForFunction(() => document.querySelectorAll('.gear-viewer-page').length === 3);
  assert.equal(hash(await readFile(path.join(vault, 'comparison.gear'))), hash(original));
  await page.evaluate(() => app.workspace.detachLeavesOfType('gear-viewer'));
  await page.waitForFunction(() => document.querySelectorAll('.gear-viewer').length === 0);
  // Disable the plugin using Obsidian's test-console interface and verify no view remains.
  await page.evaluate(() => app.plugins.disablePlugin('gear-viewer'));
  console.log('PASS in real Obsidian: file explorer, click-to-open tab, all pages, width fit, scrolling, external refresh, errors/recovery, source hashes, close and disable');
} finally {
  if (browser) await browser.close();
  child.kill();
}
