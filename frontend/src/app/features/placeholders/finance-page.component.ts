import { Component } from '@angular/core';
import { PlaceholderPageComponent } from './placeholder-page.component';

@Component({
  standalone: true,
  imports: [PlaceholderPageComponent],
  template: '<app-placeholder-page title="Financeiro" />'
})
export class FinancePageComponent {
}
