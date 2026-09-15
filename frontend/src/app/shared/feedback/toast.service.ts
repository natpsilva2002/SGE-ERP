import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info';

export interface ToastMessage {
  text: string;
  type: ToastType;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private readonly messageSubject = new BehaviorSubject<ToastMessage | null>(null);
  readonly message$ = this.messageSubject.asObservable();
  private timeoutId: ReturnType<typeof setTimeout> | null = null;

  success(text: string): void {
    this.show(text, 'success');
  }

  error(text: string): void {
    this.show(text, 'error');
  }

  info(text: string): void {
    this.show(text, 'info');
  }

  private show(text: string, type: ToastType): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }

    this.messageSubject.next({ text, type });
    this.timeoutId = setTimeout(() => this.messageSubject.next(null), 4200);
  }
}
