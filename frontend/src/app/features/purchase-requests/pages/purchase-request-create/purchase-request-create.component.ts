import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AppRoles } from '../../../../core/auth/app-roles';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../shared/feedback/toast.service';
import { CatalogItem, Work } from '../../models/purchase-request.models';
import { UnitOfMeasure } from '../../../../shared/models/unit-of-measure.models';
import {
  PurchaseRequestType,
  getPurchaseRequestTypeLabel,
  purchaseRequestTypeOptions
} from '../../models/purchase-request-type';
import { getApiErrorMessage } from '../../services/api-error';
import { PurchaseRequestService } from '../../services/purchase-request.service';

interface MaterialDraftItem {
  itemId: string;
  quantity: number;
  unit: string;
  description: string;
}

@Component({
  selector: 'app-purchase-request-create',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, RouterLink],
  templateUrl: './purchase-request-create.component.html',
  styleUrl: './purchase-request-create.component.css'
})
export class PurchaseRequestCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(PurchaseRequestService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly works = signal<Work[]>([]);
  readonly catalogItems = signal<CatalogItem[]>([]);
  readonly units = signal<UnitOfMeasure[]>([]);
  readonly materialDraftItems = signal<MaterialDraftItem[]>([]);
  readonly loadingRefs = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly typeOptions = purchaseRequestTypeOptions;
  readonly PurchaseRequestType = PurchaseRequestType;
  readonly availableTypeOptions = computed(() =>
    this.typeOptions.filter((option) => this.canCreateType(option.value))
  );
  readonly activeCatalogItems = computed(() =>
    this.catalogItems().filter((item) => item.isActive !== false)
  );
  readonly selectedMaterialUnit = computed(() => {
    const itemId = this.materialItemForm.controls.itemId.value;
    return this.catalogItems().find((item) => item.id === itemId)?.unit ?? '';
  });

  readonly form = this.fb.nonNullable.group({
    type: [PurchaseRequestType.Material, Validators.required],
    workId: ['', Validators.required],
    description: ['', Validators.required],
    serviceDescription: [''],
    serviceSpecification: [''],
    serviceQuantity: [null as number | null],
    serviceUnitOfMeasureId: ['']
  });

  readonly materialItemForm = this.fb.nonNullable.group({
    quantity: [1, [Validators.required, Validators.min(1), Validators.pattern(/^[1-9]\d*$/)]],
    itemId: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadRefs();
    const firstAllowedType = this.availableTypeOptions()[0]?.value;

    if (firstAllowedType) {
      this.form.controls.type.setValue(firstAllowedType);
    }

    this.configureTypeValidators(this.form.controls.type.value);
    this.form.controls.type.valueChanges.subscribe((type) => this.configureTypeValidators(type));
  }

  loadRefs(): void {
    this.loadingRefs.set(true);
    this.error.set('');

    forkJoin({
      works: this.service.getWorks(),
      catalogItems: this.service.getCatalogItems(),
      units: this.service.getActiveUnits()
    }).pipe(finalize(() => this.loadingRefs.set(false)))
      .subscribe({
        next: ({ works, catalogItems, units }) => {
          this.works.set(works.filter(work => work.isActive));
          this.catalogItems.set(catalogItems);
          this.units.set(units);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  addMaterialItem(): void {
    if (this.materialItemForm.invalid) {
      this.materialItemForm.markAllAsTouched();
      return;
    }

    const value = this.materialItemForm.getRawValue();
    const item = this.catalogItems().find((candidate) => candidate.id === value.itemId);

    if (!item) {
      this.toast.error('Material nao encontrado.');
      return;
    }

    this.materialDraftItems.update((items) => [
      ...items,
      {
        itemId: item.id,
        quantity: value.quantity,
        unit: item.unit,
        description: item.description
      }
    ]);
    this.materialItemForm.reset({ quantity: 1, itemId: '' });
  }

  removeMaterialItem(index: number): void {
    this.materialDraftItems.update((items) => items.filter((_, currentIndex) => currentIndex !== index));
  }

  submit(): void {
    this.configureTypeValidators(this.form.controls.type.value);

    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    if (value.type === PurchaseRequestType.Material && this.materialDraftItems().length === 0) {
      this.toast.error('Adicione pelo menos um material a solicitacao.');
      return;
    }

    if (value.type === PurchaseRequestType.Material && !value.description.trim()) {
      this.form.controls.description.markAsTouched();
      this.toast.error('Informe a descricao da solicitacao.');
      return;
    }

    if (value.type === PurchaseRequestType.Service && !value.serviceDescription.trim()) {
      this.form.controls.serviceDescription.markAsTouched();
      this.toast.error('Informe a descricao do servico.');
      return;
    }

    if (!this.authService.getCurrentUser()) {
      this.error.set('Sessao invalida.');
      return;
    }

    const work = this.works().find((candidate) => candidate.id === value.workId);

    if (!work) {
      this.error.set('Obra nao encontrada.');
      return;
    }

    this.saving.set(true);
    this.error.set('');

    this.service.create({
      workId: work.id,
      description: value.type === PurchaseRequestType.Material
        ? value.description.trim()
        : value.serviceDescription.trim(),
      type: value.type,
      serviceSpecification: value.type === PurchaseRequestType.Service
        ? value.serviceSpecification.trim() || null
        : null,
      serviceQuantity: value.type === PurchaseRequestType.Service
        ? value.serviceQuantity
        : null,
      serviceUnitOfMeasureId: value.type === PurchaseRequestType.Service
        ? value.serviceUnitOfMeasureId || null
        : null,
      items: value.type === PurchaseRequestType.Material
        ? this.materialDraftItems().map((item) => ({
            itemId: item.itemId,
            quantity: item.quantity,
            observation: null
          }))
        : []
    }).pipe(
      finalize(() => this.saving.set(false))
    ).subscribe({
      next: (request) => {
        this.toast.success('Solicitacao criada com sucesso.');
        void this.router.navigate(['/app/solicitacoes', request.id]);
      },
      error: (error) => this.error.set(getApiErrorMessage(error))
    });
  }

  canCreateType(type: PurchaseRequestType): boolean {
    if (type === PurchaseRequestType.Material) {
      return this.authService.hasRole([AppRoles.Warehouse, AppRoles.Buyer, AppRoles.Admin]);
    }

    if (type === PurchaseRequestType.Service) {
      return this.authService.hasRole([AppRoles.Warehouse, AppRoles.Buyer, AppRoles.Admin]);
    }

    return false;
  }

  typeLabel(type: PurchaseRequestType): string {
    return getPurchaseRequestTypeLabel(type);
  }

  private configureTypeValidators(type: PurchaseRequestType): void {
    if (type === PurchaseRequestType.Service) {
      this.form.controls.description.clearValidators();
      this.form.controls.serviceDescription.setValidators([Validators.required]);
      this.form.controls.serviceQuantity.setValidators([Validators.required, Validators.min(0.0001)]);
      this.form.controls.serviceUnitOfMeasureId.setValidators([Validators.required]);
    } else {
      this.form.controls.description.setValidators([Validators.required]);
      this.form.controls.serviceDescription.clearValidators();
      this.form.controls.serviceQuantity.clearValidators();
      this.form.controls.serviceUnitOfMeasureId.clearValidators();
    }

    this.form.controls.description.updateValueAndValidity({ emitEvent: false });
    this.form.controls.serviceDescription.updateValueAndValidity({ emitEvent: false });
    this.form.controls.serviceQuantity.updateValueAndValidity({ emitEvent: false });
    this.form.controls.serviceUnitOfMeasureId.updateValueAndValidity({ emitEvent: false });
  }
}
