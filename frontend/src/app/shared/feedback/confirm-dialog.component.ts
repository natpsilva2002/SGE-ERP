import { AsyncPipe, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ConfirmService } from './confirm.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [AsyncPipe, NgIf],
  template: `
    <div *ngIf="confirm.state$ | async as state" class="overlay">
      <section class="dialog" role="dialog" aria-modal="true">
        <h2>{{ state.title }}</h2>
        <p>{{ state.message }}</p>
        <div class="actions">
          <button class="secondary" type="button" (click)="confirm.close(false)">
            {{ state.cancelLabel }}
          </button>
          <button class="primary" type="button" (click)="confirm.close(true)">
            {{ state.confirmLabel }}
          </button>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .overlay {
      align-items: center;
      background: rgba(15, 23, 42, 0.42);
      display: flex;
      inset: 0;
      justify-content: center;
      padding: 20px;
      position: fixed;
      overflow-y: auto;
      z-index: 40;
    }

    .dialog {
      background: var(--surface);
      border-radius: 8px;
      box-shadow: var(--shadow);
      max-width: 420px;
      max-height: calc(100vh - 40px);
      overflow-y: auto;
      padding: 24px;
      width: 100%;
    }

    h2 {
      font-size: 20px;
      margin: 0 0 10px;
    }

    p {
      color: var(--muted);
      line-height: 1.5;
      margin: 0 0 22px;
    }

    .actions {
      display: flex;
      gap: 10px;
      justify-content: flex-end;
    }

    button {
      border-radius: 6px;
      font-weight: 800;
      min-height: 44px;
      padding: 0 14px;
    }

    .secondary {
      background: var(--surface-muted);
      color: var(--text);
    }

    .primary {
      background: var(--primary);
      color: #ffffff;
    }

    @media (max-width: 430px) {
      .overlay {
        align-items: flex-end;
        padding: 12px;
      }

      .dialog {
        max-height: calc(100vh - 24px);
        padding: 18px;
      }

      .actions {
        display: grid;
        grid-template-columns: 1fr;
      }

      button {
        width: 100%;
      }
    }
  `]
})
export class ConfirmDialogComponent {
  readonly confirm = inject(ConfirmService);
}
