// SPDX-License-Identifier: AGPL-3.0-or-later
import type { GearDocument, Mark, Page } from './document';

const NS = 'http://www.w3.org/2000/svg';
const DASHES = [[], [.01, 3], [4, 3], [4, 3, .01, 3]];
function svg(doc: Document, tag: string, attrs: Record<string, string | number>): SVGElement {
  const node = doc.createElementNS(NS, tag);
  for (const [key, value] of Object.entries(attrs)) node.setAttribute(key, String(value));
  return node;
}
function image(doc: Document, href: string, x: number, y: number, width: number, height: number): SVGElement {
  return svg(doc, 'image', { href, x, y, width, height, preserveAspectRatio: 'none' });
}
function markNode(doc: Document, m: Mark, urls: Map<string, string>): SVGElement {
  const a = m.Points[0], b = m.Points[m.Points.length - 1];
  const x = Math.min(a.X, b.X), y = Math.min(a.Y, b.Y);
  const width = Math.abs(b.X - a.X), height = Math.abs(b.Y - a.Y);
  if (m.Tool === 7) {
    const node = image(doc, urls.get(m.ImageAssetId!)!, x, y, width, height);
    node.setAttribute('transform', `rotate(${m.RotationDegrees} ${x + width / 2} ${y + height / 2})`);
    // GEAR's canvas/export ignore mark opacity for images; PNG alpha is preserved.
    return node;
  }
  const attrs: Record<string, string | number> = {
    fill: 'none', stroke: m.Colour, 'stroke-width': m.WidthMm,
    opacity: m.Opacity, 'stroke-linecap': 'round', 'stroke-linejoin': 'round',
  };
  if (m.StrokeStyle) attrs['stroke-dasharray'] = DASHES[m.StrokeStyle].map(v => v * m.WidthMm).join(' ');
  if (m.Tool === 3) return svg(doc, 'rect', { ...attrs, x, y, width, height });
  if (m.Tool === 4) return svg(doc, 'ellipse', { ...attrs, cx: x + width / 2, cy: y + height / 2, rx: width / 2, ry: height / 2 });
  if (m.Points.length === 1) return svg(doc, 'circle', { cx: a.X, cy: a.Y, r: m.WidthMm / 2, fill: m.Colour, opacity: m.Opacity });
  return svg(doc, 'polyline', { ...attrs, points: m.Points.map(p => `${p.X},${p.Y}`).join(' ') });
}

function pageNode(doc: Document, p: Page, index: number, urls: Map<string, string>): SVGElement {
  const root = svg(doc, 'svg', {
    class: 'gear-viewer-page', viewBox: `0 0 ${p.WidthMm} ${p.HeightMm}`,
    width: p.WidthMm, height: p.HeightMm, preserveAspectRatio: 'xMidYMid meet',
    role: 'img', 'aria-label': `Page ${index + 1}`, overflow: 'hidden',
  });
  root.append(svg(doc, 'rect', { width: p.WidthMm, height: p.HeightMm, fill: '#ffffff' }));
  if (p.BackgroundImageId) root.append(image(doc, urls.get(p.BackgroundImageId)!, 0, 0, p.WidthMm, p.HeightMm));
  if (p.GridOpacity > 0) {
    for (let axis = 0; axis < 2; axis++) {
      const limit = axis === 0 ? p.WidthMm : p.HeightMm;
      for (let i = 1; i * p.GridSpacingMm < limit; i++) {
        const v = i * p.GridSpacingMm, major = i % 5 === 0;
        // PdfExporter.cs uses .3/.18 PDF points. Convert to mm for this viewBox.
        root.append(svg(doc, 'line', {
          x1: axis === 0 ? v : 0, y1: axis === 0 ? 0 : v,
          x2: axis === 0 ? v : p.WidthMm, y2: axis === 0 ? p.HeightMm : v,
          stroke: '#6d91ad', 'stroke-width': (major ? .3 : .18) * 25.4 / 72,
          opacity: Math.min(1, p.GridOpacity * (major ? 1.5 : 1)),
        }));
      }
    }
  }
  for (const mark of p.Marks) if (mark.Visible) root.append(markNode(doc, mark, urls));
  return root;
}

function abortError(): DOMException { return new DOMException('Preview cancelled.', 'AbortError'); }
function decodeImage(doc: Document, url: string, signal: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const img = doc.createElement('img');
    const clean = () => { img.onload = null; img.onerror = null; signal.removeEventListener('abort', cancel); img.removeAttribute('src'); };
    const cancel = () => { clean(); reject(abortError()); };
    img.onload = () => { clean(); resolve(); };
    img.onerror = () => { clean(); reject(new Error('An embedded PNG could not be decoded.')); };
    signal.addEventListener('abort', cancel, { once: true });
    if (signal.aborted) cancel(); else img.src = url;
  });
}

export interface Preview { element: HTMLElement; dispose(): void }
export async function renderDocument(document: GearDocument, dom: Document, signal: AbortSignal): Promise<Preview> {
  const urls = new Map<string, string>();
  // Use the view's document for Obsidian pop-out window compatibility.
  const urlApi = dom.defaultView!.URL;
  const dispose = () => { for (const url of urls.values()) urlApi.revokeObjectURL(url); urls.clear(); };
  try {
    for (const [id, asset] of document.Assets) {
      if (signal.aborted) throw abortError();
      const url = urlApi.createObjectURL(new Blob([new Uint8Array(asset.bytes)], { type: 'image/png' }));
      urls.set(id, url);
      await decodeImage(dom, url, signal);
    }
    const element = dom.createElement('div');
    element.className = 'gear-viewer-pages';
    for (const [index, page] of document.Pages.entries()) {
      if (signal.aborted) throw abortError();
      element.append(pageNode(dom, page, index, urls));
    }
    return { element, dispose };
  } catch (error) { dispose(); throw error; }
}
