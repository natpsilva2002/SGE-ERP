import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { formatCurrency } from '../../../quotations/services/formatters';
import { WorkCosts } from '../../models/dashboard.models';
import { DashboardService } from '../../services/dashboard.service';

@Component({
  selector: 'app-work-costs-page',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, RouterLink],
  templateUrl: './work-costs-page.component.html',
  styleUrl: './work-costs-page.component.css'
})
export class WorkCostsPageComponent implements OnInit {
  private readonly service = inject(DashboardService);

  readonly data = signal<WorkCosts | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly selectedWork = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.getWorkCosts({
      workId: this.selectedWork() || undefined,
      startDate: this.startDate() || undefined,
      endDate: this.endDate() || undefined
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (data) => this.data.set(data),
      error: (error) => this.error.set(getApiErrorMessage(error))
    });
  }

  changeWork(value: string): void {
    this.selectedWork.set(value);
    this.load();
  }

  changeStartDate(value: string): void {
    this.startDate.set(value);
    this.load();
  }

  changeEndDate(value: string): void {
    this.endDate.set(value);
    this.load();
  }

  currency(value: number): string {
    return formatCurrency(value);
  }

  maxEvolution(data: WorkCosts): number {
    return Math.max(...data.evolution.map((item) => item.total), 1);
  }

  barWidth(value: number, data: WorkCosts): number {
    return Math.max((value / this.maxEvolution(data)) * 100, value > 0 ? 4 : 0);
  }

  percentage(value: number, total: number): number {
    return total > 0 ? Math.round((value / total) * 100) : 0;
  }

  paymentMethod(value: string): string {
    const labels: Record<string, string> = {
      Pix: 'Pix',
      BankTransfer: 'Transferência bancária',
      Boleto: 'Boleto',
      CreditCard: 'Cartão de crédito',
      DebitCard: 'Cartão de débito',
      Cash: 'Dinheiro',
      Other: 'Outro'
    };
    return labels[value] ?? value;
  }
}
