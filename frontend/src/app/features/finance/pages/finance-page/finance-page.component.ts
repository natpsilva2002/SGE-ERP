import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { formatCurrency } from '../../../quotations/services/formatters';
import {
  PurchaseOrder,
  PurchaseOrderPaymentStatus,
  PurchaseOrderStatus,
  getPurchaseOrderFinancialStatusLabel,
  getPurchaseOrderStatusLabel
} from '../../../purchase-orders/models/purchase-order.models';
import { PurchaseOrderService } from '../../../purchase-orders/services/purchase-order.service';

@Component({
  selector: 'app-finance-page',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './finance-page.component.html',
  styleUrl: './finance-page.component.css'
})
export class FinancePageComponent implements OnInit {
  private readonly purchaseOrderService = inject(PurchaseOrderService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly statusControl = new FormControl<PurchaseOrderPaymentStatus | 'all'>('all', { nonNullable: true });

  readonly loading = signal(false);
  readonly error = signal('');
  readonly purchaseOrders = signal<PurchaseOrder[]>([]);

  readonly financeOrders = computed(() =>
    this.purchaseOrders().filter((order) => this.isFinanceRelevant(order))
  );

  readonly filteredOrders = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();
    const paymentStatus = this.statusControl.value;

    return this.financeOrders().filter((order) => {
      const matchesSearch = !search ||
        order.number.toLowerCase().includes(search) ||
        (order.supplierName ?? '').toLowerCase().includes(search) ||
        (order.workName ?? '').toLowerCase().includes(search);
      const matchesPaymentStatus = paymentStatus === 'all' ||
        order.paymentStatus === paymentStatus;

      return matchesSearch && matchesPaymentStatus;
    });
  });

  readonly pendingApprovalCount = computed(() =>
    this.financeOrders().filter((order) =>
      order.paymentStatus !== PurchaseOrderPaymentStatus.Paid &&
      !order.isPaymentApproved &&
      !order.paymentApprovedAt
    ).length
  );

  readonly approvedForPaymentCount = computed(() =>
    this.financeOrders().filter((order) =>
      order.paymentStatus === PurchaseOrderPaymentStatus.Unpaid &&
      (order.isPaymentApproved || !!order.paymentApprovedAt)
    ).length
  );

  readonly partiallyPaidCount = computed(() =>
    this.financeOrders().filter((order) =>
      order.paymentStatus === PurchaseOrderPaymentStatus.PartiallyPaid
    ).length
  );

  readonly paidCount = computed(() =>
    this.financeOrders().filter((order) =>
      order.paymentStatus === PurchaseOrderPaymentStatus.Paid
    ).length
  );

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
    this.statusControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.purchaseOrderService.getAll()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (orders) => this.purchaseOrders.set(orders),
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  orderStatusLabel(status: PurchaseOrderStatus): string {
    return getPurchaseOrderStatusLabel(status);
  }

  financialStatusLabel(order: PurchaseOrder): string {
    return getPurchaseOrderFinancialStatusLabel(order);
  }

  money(value: number): string {
    return formatCurrency(value);
  }

  private isFinanceRelevant(order: PurchaseOrder): boolean {
    return order.status === PurchaseOrderStatus.Approved ||
      order.status === PurchaseOrderStatus.Sent ||
      order.status === PurchaseOrderStatus.PartiallyReceived ||
      order.status === PurchaseOrderStatus.Received ||
      order.status === PurchaseOrderStatus.PartiallyCompleted;
  }
}
