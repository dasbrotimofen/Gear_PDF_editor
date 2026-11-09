// SPDX-License-Identifier: AGPL-3.0-or-later
import { build } from 'esbuild';
import { chromium } from '@playwright/test';
import { PNG } from 'pngjs';
import assert from 'node:assert/strict';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import { createHash } from 'node:crypto';
import path from 'node:path';

await mkdir('test-output', { recursive: true });
await build({ entryPoints: ['tests/browser-entry.ts'], bundle: true, format: 'iife', globalName: 'viewer',
  outfile: 'test-output/browser.js', alias: { obsidian: path.resolve('tests/obsidian-mock.ts') } });
const source = await readFile('test-output/comparison.gear', 'utf8');
const before = createHash('sha256').update(source).digest('hex');
const browser = await chromium.launch(process.env.GEAR_BROWSER ? { executablePath: process.env.GEAR_BROWSER } : { channel: 'msedge' });
try {
  const page = await browser.newPage({ viewport: { width: 1400, height: 1800 }, deviceScaleFactor: 1 });
  await page.setContent('<style>body{margin:0}svg{display:block;width:100%;height:auto}</style>');
  await page.addScriptTag({ path: 'test-output/browser.js' });
  const checks = await page.evaluate(async source => {
    const { parseDocument, renderDocument, GearView, Plugin } = window.viewer;
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    const parsed = parseDocument(source);
    // Guard large native base64 against regex stack overflows.
    const large = JSON.parse(source);
    const png = new Uint8Array(2 * 1024 * 1024);
    png.set(parsed.Assets.get('image').bytes);
    let binary = '';
    for (let offset = 0; offset < png.length; offset += 8192) binary += String.fromCharCode(...png.subarray(offset, offset + 8192));
    large.Assets.image.Png = btoa(binary);
    check(parseDocument(JSON.stringify(large)).Assets.get('image').bytes.length === png.length, 'large base64');
    const liveUrls = new Set();
    const createUrl = URL.createObjectURL.bind(URL), revokeUrl = URL.revokeObjectURL.bind(URL);
    URL.createObjectURL = blob => { const url = createUrl(blob); liveUrls.add(url); return url; };
    URL.revokeObjectURL = url => { liveUrls.delete(url); revokeUrl(url); };
    for (const version of [1, 2, 3]) {
      const legacy = parseDocument(JSON.stringify({ Version: version, Pages: [{ Marks: [{ Points: [{ X: 1, Y: 2 }] }] }] }));
      const m = legacy.Pages[0].Marks[0];
      check(m.Visible && !m.Locked && m.RotationDegrees === 0 && m.StrokeStyle === 0, 'legacy defaults');
      check(legacy.Pages[0].WidthMm === 210, 'legacy paper');
    }
    for (const bad of ['{', 'null', '{"Version":4}', '{"Version":"3"}', '{"Pages":[]}', '{"Assets":null}',
      '{"Pages":[{"GridOpacity":2}]}', '{"Pages":[{"Marks":[{"Points":[{}],"Visible":null}]}]}',
      '{"Pages":[{"BackgroundImageId":"missing"}]}', '{"Pages":[{"Marks":[{"Tool":5,"Points":[{}]}]}]}']) {
      let rejected = false; try { parseDocument(bad); } catch { rejected = true; } check(rejected, `accepted ${bad}`);
    }
    const preview = await renderDocument(parsed, document, new AbortController().signal);
    check(preview.element.children.length === 3, 'page count');
    check(preview.element.querySelectorAll('image').length === 10, 'hidden images or backgrounds');
    check(!preview.element.querySelector('[stroke="#ff0000"]'), 'hidden strokes rendered');
    check(preview.element.querySelectorAll('image[transform*="rotate(30"]').length === 3, 'rotations');
    preview.dispose();
    check(liveUrls.size === 0, 'preview URL cleanup');
    const broken = parseDocument(source);
    broken.Assets.get('image').bytes = new Uint8Array([0]);
    let decodeRejected = false;
    try { await renderDocument(broken, document, new AbortController().signal); } catch { decodeRejected = true; }
    check(decodeRejected && liveUrls.size === 0, 'corrupt PNG rejection and URL cleanup');

    // Exercise real view code with a read-only vault API stub. Every write-like
    // access throws. Obsidian's explorer and dispatch are tested separately.
    let text = source, reads = 0, listener;
    const file = { path: 'comparison.gear', name: 'comparison.gear', stat: { size: source.length } };
    const vault = new Proxy({
      on: (_event, callback) => { listener = callback; return callback; },
      offref: ref => { if (listener === ref) listener = undefined; },
      read: async () => { reads++; return text; },
    }, { get(target, key) { if (!(key in target)) throw new Error(`Unexpected vault API: ${String(key)}`); return target[key]; } });
    const app = { vault, workspace: { detachLeavesOfType: () => {} } };
    const plugin = new Plugin(); plugin.app = app; plugin.onload();
    check(plugin.extensions.get('gear') === 'gear-viewer', 'extension registration');
    const view = new GearView({ app }); document.body.append(view.contentEl);
    await view.onOpen(); await view.onLoadFile(file);
    check(view.contentEl.querySelectorAll('svg').length === 3, 'initial open');
    text = '{"Version":3,"Pages":[{"WidthMm":100,"HeightMm":200}]}';
    listener(file);
    await new Promise(resolve => setTimeout(resolve, 300));
    check(view.contentEl.querySelectorAll('svg').length === 1 && reads === 2, 'external refresh');
    check(view.contentEl.querySelector('svg').getAttribute('viewBox') === '0 0 100 200', 'fresh geometry');
    text = '{'; listener(file); await new Promise(resolve => setTimeout(resolve, 200));
    check(view.contentEl.querySelector('[role="alert"]').textContent.includes('Malformed'), 'readable error');
    const callback = listener;
    text = source; callback(file); await view.onClose();
    await new Promise(resolve => setTimeout(resolve, 200));
    check(!listener && view.contentEl.children.length === 0 && reads === 3, 'close cleanup');
    check(liveUrls.size === 0, 'closed view URL cleanup');
    // Late read completion must not resurrect a closed preview.
    let finish;
    const late = new GearView({ app: { vault: { read: () => new Promise(resolve => { finish = resolve; }) } } });
    const loading = late.onLoadFile(file); await late.onClose(); finish(source); await loading;
    check(late.contentEl.children.length === 0, 'stale load after close');
    view.contentEl.remove();
    return 'parser, v1/v2 defaults, visibility, image rotation, API registration, read-only access, refresh, errors and cleanup';
  }, source);
  console.log('PASS:', checks);
  // Compare SVG browser output to actual PDFsharp -> PDFium pixels.
  const results = [];
  for (let index = 0; index < 3; index++) {
    const reference = PNG.sync.read(await readFile(`test-output/reference-${index + 1}.png`));
    await page.setViewportSize({ width: reference.width, height: reference.height });
    await page.evaluate(async ({ source, index }) => {
      const preview = await window.viewer.renderDocument(window.viewer.parseDocument(source), document, new AbortController().signal);
      document.body.replaceChildren(preview.element.children[index]);
      await Promise.all(Array.from(document.images).map(img => img.decode()));
      // SVG images load separately; wait two frames before screenshotting.
      await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    }, { source, index });
    const buffer = await page.screenshot({ path: `test-output/preview-${index + 1}.png` });
    const actual = PNG.sync.read(buffer);
    let absolute = 0, different = 0;
    for (let i = 0; i < actual.data.length; i += 4) {
      let delta = 0;
      for (let c = 0; c < 3; c++) { const d = Math.abs(actual.data[i + c] - reference.data[i + c]); absolute += d; delta = Math.max(delta, d); }
      if (delta > 40) different++;
    }
    const mae = absolute / (actual.width * actual.height * 3), fraction = different / (actual.width * actual.height);
    results.push({ page: index + 1, width: actual.width, height: actual.height, meanAbsoluteChannelError: mae, fractionPixelsOver40: fraction });
    // Allow rasterizer antialiasing/dash-cap differences, but catch missing
    // backgrounds, wrong scales/rotation, opacity, or visible hidden layers.
    assert.ok(mae < 5 && fraction < .04, `PDF comparison page ${index + 1}: MAE ${mae}, differing ${fraction}`);
    console.log(`PASS: PDF reference page ${index + 1}: MAE ${mae.toFixed(3)}, ${(fraction * 100).toFixed(2)}% pixels >40`);
  }
  await writeFile('test-output/comparison-results.json', JSON.stringify(results, null, 2));
  await page.setViewportSize({ width: 460, height: 800 });
  assert.ok(await page.locator('svg').evaluate(el => Math.abs(el.getBoundingClientRect().width - 460) < 1), 'fit width');
  const after = createHash('sha256').update(await readFile('test-output/comparison.gear')).digest('hex');
  assert.equal(before, after, 'source file changed');
  console.log('PASS: responsive width and unchanged source SHA-256');
} finally { await browser.close(); }
