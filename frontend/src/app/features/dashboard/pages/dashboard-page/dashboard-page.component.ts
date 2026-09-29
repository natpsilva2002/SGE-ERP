import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { getApiErrorMessage } from '../../../purchase-requests/services/api-error';
import { formatCurrency } from '../../../quotations/services/formatters';
import { Dashboard, DashboardCard, DashboardEntry } from '../../models/dashboard.models';
import { DashboardService } from '../../services/dashboard.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, RouterLink],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.css'
})
export class DashboardPageComponent implements OnInit {
  private readonly service = inject(DashboardService);
  private readonly authService = inject(AuthService);

  readonly dashboard = signal<Dashboard | null>(null);
  readonly loading = signal(false);
  readonly error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.service.get()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (dashboard) => this.dashboard.set(dashboard),
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  userRole(): string {
    return this.authService.getCurrentUser()?.role ?? 'Usuário';
  }

  cardValue(card: DashboardCard): string {
    if (card.amount !== null && card.amount !== undefined) {
      return formatCurrency(card.amount);
    }

    return String(card.value ?? 0);
  }

  entryClass(entry: DashboardEntry): string {
    const text = `${entry.type} ${entry.status ?? ''}`.toLowerCase();

    if (text.includes('diverg')) return 'entry-danger';
    if (text.includes('aprova') || text.includes('pendente')) return 'entry-warning';
    if (text.includes('receb') || text.includes('andamento')) return 'entry-info';
    return 'entry-neutral';
  }
}
