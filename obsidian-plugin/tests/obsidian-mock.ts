// SPDX-License-Identifier: AGPL-3.0-or-later
// Contract stub only: these tests do not prove behaviour in Obsidian itself.
export class FileView {
  app: any;
  contentEl: HTMLElement;
  refs: any[] = [];
  constructor(leaf: any) { this.app = leaf.app; this.contentEl = document.createElement('div'); }
  registerEvent(ref: any): void { this.refs.push(ref); }
}
export class Plugin {
  app: any;
  views = new Map();
  extensions = new Map();
  registerView(type: string, factory: any): void { this.views.set(type, factory); }
  registerExtensions(extensions: string[], type: string): void {
    for (const extension of extensions) this.extensions.set(extension, type);
  }
}
export class TFile {}
