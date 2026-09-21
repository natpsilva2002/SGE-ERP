import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import {
  PurchaseOrder,
  PurchaseOrderStatus,
  hasReceivingDivergence,
  getPurchaseOrderReceivingStatusLabel,
  getPurchaseOrderStatusLabel
} from '../../../purchase-orders/models/purchase-order.models';
import { PurchaseOrderService } from '../../../purchase-orders/services/purchase-order.service';

type ReceivingFilter = 'all' | 'not-received' | 'partial' | 'received' | 'divergent';

@Component({
  selector: 'app-receipts-page',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './receipts-page.component.html',
  styleUrl: './receipts-page.component.css'
})
export class ReceiptsPageComponent implements OnInit {
  private readonly purchaseOrderService = inject(PurchaseOrderService);

  readonly searchControl = new FormControl('', { nonNullable: true });
  readonly receivingStatusControl = new FormControl<ReceivingFilter>('all', { nonNullable: true });

  readonly loading = signal(false);
  readonly error = signal('');
  readonly purchaseOrders = signal<PurchaseOrder[]>([]);

  readonly receivingOrders = computed(() =>
    this.purchaseOrders().filter((order) => this.isReceivingRelevant(order))
  );

  readonly filteredOrders = computed(() => {
    const search = this.searchControl.value.trim().toLowerCase();
    const status = this.receivingStatusControl.value;

    return this.receivingOrders().filter((order) => {
      const matchesSearch = !search ||
        order.number.toLowerCase().includes(search) ||
        (order.supplierName ?? '').toLowerCase().includes(search) ||
        (order.workName ?? '').toLowerCase().includes(search);
      const matchesReceiving = status === 'all' ||
        this.receivingKey(order) === status;

      return matchesSearch && matchesReceiving;
    });
  });

  ngOnInit(): void {
    this.load();
    this.searchControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
    this.receivingStatusControl.valueChanges.subscribe(() => this.purchaseOrders.update((items) => [...items]));
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

  receivingStatusLabel(order: PurchaseOrder): string {
    return getPurchaseOrderReceivingStatusLabel(order);
  }

  hasDivergence(order: PurchaseOrder): boolean {
    return hasReceivingDivergence(order);
  }

  itemCount(order: PurchaseOrder): number {
    return order.items.length;
  }

  totalPending(order: PurchaseOrder): number {
    return order.items.reduce((sum, item) => sum + item.quantityPending, 0);
  }

  private isReceivingRelevant(order: PurchaseOrder): boolean {
    return order.status === PurchaseOrderStatus.Approved ||
      order.status === PurchaseOrderStatus.Sent ||
      order.status === PurchaseOrderStatus.PartiallyReceived ||
      order.status === PurchaseOrderStatus.Received ||
      order.status === PurchaseOrderStatus.PartiallyCompleted;
  }

  private receivingKey(order: PurchaseOrder): ReceivingFilter {
    if (this.hasDivergence(order)) {
      return 'divergent';
    }

    if (order.status === PurchaseOrderStatus.Received) {
      return 'received';
    }

    if (order.status === PurchaseOrderStatus.PartiallyReceived ||
      order.items.some((item) => item.quantityReceived > 0)) {
      return 'partial';
    }

    return 'not-received';
  }
}
