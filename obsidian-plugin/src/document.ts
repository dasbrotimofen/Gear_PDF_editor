// SPDX-License-Identifier: AGPL-3.0-or-later
// Specification: src/GearPdf.Core/Document.cs. No changes are written back.
export interface Vec { X: number; Y: number }
export interface Mark {
  Tool: number; Colour: string; WidthMm: number; Opacity: number; Points: Vec[];
  RotationDegrees: number; StrokeStyle: number; Visible: boolean; Locked: boolean;
  ImageAssetId?: string | null;
}
export interface Page {
  WidthMm: number; HeightMm: number; GridSpacingMm: number; GridOpacity: number;
  Marks: Mark[]; BackgroundImageId?: string | null;
}
export interface Asset { bytes: Uint8Array; PixelWidth: number; PixelHeight: number }
export interface GearDocument { Pages: Page[]; Assets: Map<string, Asset> }
export const MAX_FILE_BYTES = 200 * 1024 * 1024;

function fail(message: string): never { throw new Error(message); }
function object(value: unknown, name: string): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) fail(`Invalid ${name}.`);
  return value as Record<string, unknown>;
}
function number(value: unknown, min: number, max: number, name: string): number {
  if (typeof value !== 'number' || !Number.isFinite(value) || value < min || value > max) fail(`Invalid ${name}.`);
  return value;
}
function integer(value: unknown, min: number, max: number, name: string): number {
  const result = number(value, min, max, name);
  if (!Number.isInteger(result)) fail(`Invalid ${name}.`);
  return result;
}
function bool(value: unknown, fallback: boolean): boolean {
  if (value === undefined) return fallback;
  if (typeof value !== 'boolean') fail('Invalid layer visibility or lock.');
  return value;
}
function list(value: unknown, name: string): unknown[] {
  if (!Array.isArray(value)) fail(`Invalid ${name}.`);
  return value;
}
function assetId(value: unknown, assets: Map<string, Asset>): string | null {
  if (value === undefined || value === null) return null;
  if (typeof value !== 'string' || !assets.has(value)) fail('Missing embedded image or PDF background.');
  return value;
}

export function parseDocument(text: string): GearDocument {
  // Obsidian's file stat is checked before reading; also guard direct callers.
  if (text.length > MAX_FILE_BYTES) fail('Document exceeds the 200 MB GEAR limit.');
  let input: unknown;
  try { input = JSON.parse(text.replace(/^\uFEFF/, '')); }
  catch { fail('Malformed .gear file: expected JSON.'); }
  const root = object(input, 'document');
  const version = root.Version === undefined ? 3 : root.Version;
  if (![1, 2, 3].includes(version as number)) fail(`Unsupported GEAR document version: ${String(version)}. Supported versions: 1, 2 and 3.`);
  const assets = new Map<string, Asset>();
  let total = 0;
  for (const [id, value] of Object.entries(root.Assets === undefined ? {} : object(root.Assets, 'image assets'))) {
    if (!id.trim()) fail('Invalid embedded image ID.');
    const a = object(value, 'embedded image');
    const width = integer(a.PixelWidth === undefined ? 0 : a.PixelWidth, 1, 10000, 'image width');
    const height = integer(a.PixelHeight === undefined ? 0 : a.PixelHeight, 1, 10000, 'image height');
    if (width * height > 40000000 || typeof a.Png !== 'string') fail('Invalid embedded PNG.');
    // System.Text.Json encodes byte[] as base64, allowing whitespace on read.
    const base64 = a.Png.replace(/\s/g, '');
    const padding = base64.endsWith('==') ? 2 : base64.endsWith('=') ? 1 : 0;
    // Avoid a repeated-group regex: native PNGs can be many megabytes long.
    if (base64.length % 4 !== 0 || /[^A-Za-z0-9+/=]/.test(base64)
      || base64.slice(0, base64.length - padding).includes('=')) fail('Invalid embedded PNG base64.');
    total += base64.length / 4 * 3 - (base64.endsWith('==') ? 2 : base64.endsWith('=') ? 1 : 0);
    if (total > 128 * 1024 * 1024) fail('Embedded images exceed the 128 MB GEAR limit.');
    const binary = atob(base64);
    const bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
    const header = new DataView(bytes.buffer);
    if (bytes.length < 24 || ![137, 80, 78, 71, 13, 10, 26, 10].every((n, i) => bytes[i] === n)
      || header.getUint32(16) !== width || header.getUint32(20) !== height) fail('Invalid embedded PNG header or dimensions.');
    assets.set(id, { bytes, PixelWidth: width, PixelHeight: height });
  }
  const pages = list(root.Pages === undefined ? [{}] : root.Pages, 'pages');
  if (pages.length < 1 || pages.length > 1000) fail('Document must contain 1–1000 pages.');
  return { Assets: assets, Pages: pages.map(value => {
    const p = object(value, 'page');
    return {
      WidthMm: number(p.WidthMm === undefined ? 210 : p.WidthMm, 10, 2000, 'page width'),
      HeightMm: number(p.HeightMm === undefined ? 297 : p.HeightMm, 10, 2000, 'page height'),
      GridSpacingMm: number(p.GridSpacingMm === undefined ? 5 : p.GridSpacingMm, 1, 100, 'grid spacing'),
      GridOpacity: number(p.GridOpacity === undefined ? .25 : p.GridOpacity, 0, 1, 'grid opacity'),
      BackgroundImageId: assetId(p.BackgroundImageId, assets),
      Marks: list(p.Marks === undefined ? [] : p.Marks, 'marks').map(value => {
        const m = object(value, 'annotation');
        const tool = integer(m.Tool === undefined ? 0 : m.Tool, 0, 7, 'tool');
        if (tool === 5 || tool === 6) fail('Selection and eraser are not stored annotations.');
        const points = list(m.Points === undefined ? [] : m.Points, 'annotation points');
        if (points.length < 1 || points.length > 1000000) fail('Invalid annotation point count.');
        const result: Mark = {
          Tool: tool,
          Colour: m.Colour === undefined ? '#e34444' : m.Colour as string,
          WidthMm: number(m.WidthMm === undefined ? .5 : m.WidthMm, Number.MIN_VALUE, 50, 'stroke width'),
          Opacity: number(m.Opacity === undefined ? 1 : m.Opacity, 0, 1, 'stroke opacity'),
          RotationDegrees: number(m.RotationDegrees === undefined ? 0 : m.RotationDegrees, -360000, 360000, 'image rotation'),
          StrokeStyle: integer(m.StrokeStyle === undefined ? 0 : m.StrokeStyle, 0, 3, 'stroke style'),
          Visible: bool(m.Visible, true), Locked: bool(m.Locked, false),
          Points: points.map(value => {
            const v = object(value, 'point');
            return { X: number(v.X === undefined ? 0 : v.X, -Number.MAX_VALUE, Number.MAX_VALUE, 'point X'),
              Y: number(v.Y === undefined ? 0 : v.Y, -Number.MAX_VALUE, Number.MAX_VALUE, 'point Y') };
          }),
        };
        if (typeof result.Colour !== 'string' || !/^#[0-9a-fA-F]{6}$/.test(result.Colour)) fail('Invalid stroke colour.');
        if (tool === 7) {
          result.ImageAssetId = assetId(m.ImageAssetId, assets);
          if (!result.ImageAssetId || result.Points.length !== 2 || result.Points[1].X <= result.Points[0].X
            || result.Points[1].Y <= result.Points[0].Y) fail('Invalid image placement.');
        }
        return result;
      }),
    };
  }) };
}
