import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'my-app-theme';
  readonly isDark = signal<boolean>(this.readInitial());

  constructor() {
    document.documentElement.classList.toggle('app-dark', this.isDark());
  }

  toggle(): void {
    const next = !this.isDark();
    this.isDark.set(next);
    localStorage.setItem(this.storageKey, next ? 'dark' : 'light');
    document.documentElement.classList.toggle('app-dark', next);
  }

  private readInitial(): boolean {
    const stored = localStorage.getItem(this.storageKey);
    if (stored) return stored === 'dark';
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}
