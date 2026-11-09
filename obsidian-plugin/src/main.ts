// SPDX-License-Identifier: AGPL-3.0-or-later
import { FileView, Plugin, TFile, type EventRef, type WorkspaceLeaf } from 'obsidian';
import { MAX_FILE_BYTES, parseDocument } from './document';
import { renderDocument, type Preview } from './render';

export const VIEW_TYPE = 'gear-viewer';

export class GearView extends FileView {
  private current: TFile | null = null;
  private modifyRef?: EventRef;
  private timer?: ReturnType<typeof setTimeout>;
  private controller?: AbortController;
  private preview?: Preview;
  private revision = 0;

  constructor(leaf: WorkspaceLeaf) { super(leaf); }
  getViewType(): string { return VIEW_TYPE; }
  getIcon(): string { return 'file-image'; }
  canAcceptExtension(extension: string): boolean { return extension === 'gear'; }

  async onOpen(): Promise<void> {
    this.contentEl.classList.add('gear-viewer');
    this.modifyRef = this.app.vault.on('modify', file => {
      if (this.current && file.path === this.current.path) {
        // Invalidate in-flight reads immediately, before the debounce fires.
        this.cancel();
        this.timer = setTimeout(() => { if (this.current) void this.refresh(this.current); }, 100);
      }
    });
    this.registerEvent(this.modifyRef);
  }

  async onLoadFile(file: TFile): Promise<void> {
    this.current = file;
    this.clear();
    await this.refresh(file);
  }

  private cancel(): void {
    this.revision++;
    clearTimeout(this.timer);
    this.timer = undefined;
    this.controller?.abort();
    this.controller = undefined;
  }
  private clear(): void {
    this.cancel();
    this.preview?.dispose();
    this.preview = undefined;
    this.contentEl.replaceChildren();
  }
  private async refresh(file: TFile): Promise<void> {
    this.cancel();
    const revision = this.revision;
    const controller = this.controller = new AbortController();
    const scroll = this.contentEl.scrollTop;
    try {
      if (file.stat.size > MAX_FILE_BYTES) throw new Error('Document exceeds the 200 MB GEAR limit.');
      // No cachedRead: an external save must always reload the current file.
      const text = await this.app.vault.read(file);
      if (revision !== this.revision) return;
      const next = await renderDocument(parseDocument(text), this.contentEl.ownerDocument, controller.signal);
      if (revision !== this.revision) { next.dispose(); return; }
      this.preview?.dispose();
      this.preview = next;
      this.contentEl.replaceChildren(next.element);
      this.contentEl.scrollTop = scroll;
    } catch (error) {
      if (revision !== this.revision || controller.signal.aborted) return;
      this.preview?.dispose();
      this.preview = undefined;
      const message = this.contentEl.ownerDocument.createElement('p');
      message.className = 'gear-viewer-error';
      message.setAttribute('role', 'alert');
      message.textContent = `Unable to preview ${file.name}\n${error instanceof Error ? error.message : String(error)}`;
      this.contentEl.replaceChildren(message);
    }
  }
  async onUnloadFile(): Promise<void> { this.current = null; this.clear(); }
  async onClose(): Promise<void> {
    this.current = null;
    this.clear();
    if (this.modifyRef) this.app.vault.offref(this.modifyRef);
    this.modifyRef = undefined;
  }
}

export default class GearViewerPlugin extends Plugin {
  onload(): void {
    this.registerView(VIEW_TYPE, leaf => new GearView(leaf));
    this.registerExtensions(['gear'], VIEW_TYPE);
  }
  onunload(): void { this.app.workspace.detachLeavesOfType(VIEW_TYPE); }
}
