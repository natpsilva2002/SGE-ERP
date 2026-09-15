import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
}

interface ConfirmState extends ConfirmDialogData {
  result: Subject<boolean>;
}

@Injectable({
  providedIn: 'root'
})
export class ConfirmService {
  private readonly stateSubject = new BehaviorSubject<ConfirmState | null>(null);
  readonly state$ = this.stateSubject.asObservable();

  confirm(data: Partial<ConfirmDialogData>): Observable<boolean> {
    const result = new Subject<boolean>();

    this.stateSubject.next({
      title: data.title ?? 'Confirmar acao',
      message: data.message ?? 'Deseja continuar?',
      confirmLabel: data.confirmLabel ?? 'Confirmar',
      cancelLabel: data.cancelLabel ?? 'Cancelar',
      result
    });

    return result.asObservable();
  }

  close(value: boolean): void {
    const state = this.stateSubject.value;

    if (!state) {
      return;
    }

    state.result.next(value);
    state.result.complete();
    this.stateSubject.next(null);
  }
}
