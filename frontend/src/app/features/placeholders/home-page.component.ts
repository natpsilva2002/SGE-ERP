import { NgIf } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DashboardPageComponent } from '../dashboard/pages/dashboard-page/dashboard-page.component';
import { WorkCostsPageComponent } from '../dashboard/pages/work-costs-page/work-costs-page.component';
import { AuthService } from '../../core/auth/auth.service';
import { AppRoles } from '../../core/auth/app-roles';

@Component({
  standalone: true,
  imports: [DashboardPageComponent, NgIf, RouterLink, WorkCostsPageComponent],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.css'
})
export class HomePageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly tab = signal<'overview' | 'work-costs'>('overview');

  get canSeeWorkCosts(): boolean {
    const role = this.authService.getCurrentUser()?.role;
    return role === AppRoles.Admin || role === AppRoles.Finance;
  }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.tab.set(params.get('tab') === 'work-costs' && this.canSeeWorkCosts ? 'work-costs' : 'overview');
    });
  }
}
