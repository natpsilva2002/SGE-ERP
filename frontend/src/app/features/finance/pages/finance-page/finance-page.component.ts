import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { formatCurrency } from '../../../quotations/services/formatters';
import {
  FinanceQueueDocumentType,
  FinanceQueueItem,
  FinanceQueuePaymentStatus
} from '../../models/finance-queue.models';
import { FinanceQueueService } from '../../services/finance-queue.service';

@Component({
  selector: 'app-finance-page',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './finance-page.component.html',
  styleUrl: './finance-page.component.css'
})
export class FinancePageComponent implements OnInit {
  private readonly financeQueueService = inject(FinanceQueueService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl<FinanceQueuePaymentStatus | 'all'>('all', { nonNullable: true });

  readonly loading = signal(false);
  readonly error = signal('');
  readonly financeItems = signal<FinanceQueueItem[]>([]);

  readonly filteredItems = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();
    const paymentStatus = this.statusControl.value;

    return this.financeItems().filter((item) => {
      const matchesSearch = !search ||
        item.number.toLowerCase().includes(search) ||
        item.supplierName.toLowerCase().includes(search) ||
        item.workName.toLowerCase().includes(search);
      const matchesPaymentStatus = paymentStatus === 'all' ||
        item.paymentStatus === paymentStatus;

      return matchesSearch && matchesPaymentStatus;
    });
  });

  readonly pendingApprovalCount = computed(() =>
    this.financeItems().filter((item) => item.paymentStatus === FinanceQueuePaymentStatus.AwaitingApproval).length
  );

  readonly approvedForPaymentCount = computed(() =>
    this.financeItems().filter((item) => item.paymentStatus === FinanceQueuePaymentStatus.Authorized).length
  );

  readonly partiallyPaidCount = computed(() =>
    this.financeItems().filter((item) => item.paymentStatus === FinanceQueuePaymentStatus.PartiallyPaid).length
  );

  readonly paidCount = computed(() =>
    this.financeItems().filter((item) => item.paymentStatus === FinanceQueuePaymentStatus.Paid).length
  );

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() => this.financeItems.update((items) => [...items]));
    this.statusControl.valueChanges.subscribe(() => this.financeItems.update((items) => [...items]));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.financeQueueService.getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (items) => this.financeItems.set(items),
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  documentTypeLabel(type: FinanceQueueDocumentType): string {
    return type === FinanceQueueDocumentType.Service ? 'Servico' : 'Material';
  }

  financialStatusLabel(item: FinanceQueueItem): string {
    switch (item.paymentStatus) {
      case FinanceQueuePaymentStatus.AwaitingApproval:
        return 'Aguardando aprovacao';
      case FinanceQueuePaymentStatus.Authorized:
        return 'Autorizada';
      case FinanceQueuePaymentStatus.PartiallyPaid:
        return 'Parcialmente pago';
      case FinanceQueuePaymentStatus.Paid:
        return 'Pago';
      default:
        return 'Nao pago';
    }
  }

  money(value: number): string {
    return formatCurrency(value);
  }

  detailsLink(item: FinanceQueueItem): string[] {
    const route = item.documentType === FinanceQueueDocumentType.Service
      ? 'ordens-servico'
      : 'ordens-compra';

    return ['/app', route, item.id];
  }
}
