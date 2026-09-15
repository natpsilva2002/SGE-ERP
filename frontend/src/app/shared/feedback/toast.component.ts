import { AsyncPipe, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [AsyncPipe, NgIf],
  template: `
    <div *ngIf="toast.message$ | async as message" class="toast" [class]="message.type">
      {{ message.text }}
    </div>
  `,
  styles: [`
    .toast {
      border-radius: 6px;
      bottom: 24px;
      box-shadow: var(--shadow);
      color: #ffffff;
      font-size: 14px;
      font-weight: 700;
      max-width: min(420px, calc(100vw - 32px));
      padding: 12px 16px;
      position: fixed;
      right: 24px;
      z-index: 50;
    }

    .success {
      background: #067647;
    }

    .error {
      background: #b42318;
    }

    .info {
      background: #175cd3;
    }

    @media (max-width: 430px) {
      .toast {
        bottom: 14px;
        left: 14px;
        max-width: none;
        right: 14px;
      }
    }
  `]
})
export class ToastComponent {
  readonly toast = inject(ToastService);
}
