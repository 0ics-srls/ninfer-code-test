import { Component, signal, ChangeDetectionStrategy, inject, CUSTOM_ELEMENTS_SCHEMA, isDevMode } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ThemeService } from './core/theme.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly theme = inject(ThemeService);
  protected readonly title = signal('my-app');
  protected readonly isDark = this.theme.isDark;
  protected readonly isDev = isDevMode();
  protected toggleTheme = () => this.theme.toggle();
}
