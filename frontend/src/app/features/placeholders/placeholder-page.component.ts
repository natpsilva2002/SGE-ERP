import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-placeholder-page',
  standalone: true,
  template: `
    <section class="page">
      <h1>{{ title }}</h1>
      <p>Modulo em desenvolvimento.</p>
    </section>
  `,
  styles: [`
    .page {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 28px;
    }

    h1 {
      font-size: 24px;
      margin: 0 0 8px;
    }

    p {
      color: var(--muted);
      margin: 0;
    }
  `]
})
export class PlaceholderPageComponent {
  @Input({ required: true }) title = '';
}
