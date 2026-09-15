import { NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../shared/feedback/toast.service';
import { getApiErrorMessage } from '../purchase-requests/services/api-error';
import {
  CatalogItem,
  CreateCatalogItem,
  UpdateCatalogItem
} from '../purchase-requests/models/purchase-request.models';
import { CreateSupplier, Supplier, UpdateSupplier } from '../quotations/models/quotation.models';

type RegistrationTab = 'materials' | 'suppliers';

@Component({
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule],
  template: `
    <section class="page-header">
      <div>
        <span class="eyebrow">Cadastros</span>
        <h1>Cadastros</h1>
      </div>
    </section>

    <section class="tabs" role="tablist" aria-label="Cadastros">
      <button type="button" [class.active]="activeTab() === 'materials'" (click)="activeTab.set('materials')">Materiais</button>
      <button type="button" [class.active]="activeTab() === 'suppliers'" (click)="activeTab.set('suppliers')">Fornecedores</button>
    </section>

    <div *ngIf="loading()" class="state">Carregando cadastros...</div>
    <div *ngIf="error()" class="state error">{{ error() }}</div>

    <section class="panel" *ngIf="!loading() && activeTab() === 'materials'">
      <div class="section-title">
        <div>
          <h2>Materiais cadastrados</h2>
          <p>Materiais usados nas solicitacoes de obra.</p>
        </div>
        <button type="button" (click)="startNewMaterial()">+ Adicionar material</button>
      </div>

      <label class="search">
        <span>Buscar material</span>
        <input type="search" [value]="materialSearch()" (input)="materialSearch.set($any($event.target).value)" placeholder="Buscar material...">
      </label>

      <form class="editor" *ngIf="showMaterialForm()" [formGroup]="materialForm" (ngSubmit)="saveMaterial()">
        <label>
          <span>Nome / Descricao do material</span>
          <input type="text" formControlName="description">
        </label>
        <label>
          <span>Unidade de medida</span>
          <select formControlName="unit">
            <option *ngFor="let unit of unitOptions" [value]="unit">{{ unit }}</option>
          </select>
        </label>
        <label class="checkbox">
          <input type="checkbox" formControlName="isActive">
          <span>Ativo</span>
        </label>
        <div class="form-actions">
          <button type="submit" [disabled]="materialForm.invalid || saving()">{{ editingMaterialId() ? 'Salvar material' : 'Cadastrar material' }}</button>
          <button type="button" class="secondary" (click)="cancelMaterialEdit()">Cancelar</button>
        </div>
      </form>

      <div class="table-wrap desktop-list" *ngIf="filteredMaterials().length > 0; else noMaterials">
        <table>
          <thead>
            <tr>
              <th>Material</th>
              <th>Unidade</th>
              <th>Status</th>
              <th>Acoes</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let material of filteredMaterials()">
              <td>{{ material.description }}</td>
              <td>{{ material.unit }}</td>
              <td><span class="badge" [class.inactive]="!material.isActive">{{ material.isActive ? 'Ativo' : 'Inativo' }}</span></td>
              <td><button type="button" class="secondary" (click)="editMaterial(material)">Editar</button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mobile-list" *ngIf="filteredMaterials().length > 0">
        <article class="list-card" *ngFor="let material of filteredMaterials()">
          <strong>{{ material.description }}</strong>
          <span>Unidade: {{ material.unit }}</span>
          <span class="badge" [class.inactive]="!material.isActive">{{ material.isActive ? 'Ativo' : 'Inativo' }}</span>
          <button type="button" class="secondary" (click)="editMaterial(material)">Editar</button>
        </article>
      </div>

      <ng-template #noMaterials>
        <div class="state">Nenhum material encontrado.</div>
      </ng-template>
    </section>

    <section class="panel" *ngIf="!loading() && activeTab() === 'suppliers'">
      <div class="section-title">
        <div>
          <h2>Fornecedores cadastrados</h2>
          <p>Cadastro comercial usado nas cotacoes.</p>
        </div>
        <button type="button" (click)="startNewSupplier()">+ Adicionar fornecedor</button>
      </div>

      <label class="search">
        <span>Buscar fornecedor</span>
        <input type="search" [value]="supplierSearch()" (input)="supplierSearch.set($any($event.target).value)" placeholder="Buscar nome ou CNPJ...">
      </label>

      <form class="editor supplier-editor" *ngIf="showSupplierForm()" [formGroup]="supplierForm" (ngSubmit)="saveSupplier()">
        <label>
          <span>Razao Social / Nome</span>
          <input type="text" formControlName="corporateName">
        </label>
        <label>
          <span>Nome fantasia</span>
          <input type="text" formControlName="tradeName">
        </label>
        <label>
          <span>CNPJ</span>
          <input type="text" formControlName="document" maxlength="18" (input)="formatSupplierDocument()">
        </label>
        <label>
          <span>Inscricao Estadual</span>
          <input type="text" formControlName="stateRegistration">
        </label>
        <label>
          <span>Contato</span>
          <input type="text" formControlName="contactName">
        </label>
        <label>
          <span>Telefone</span>
          <input type="text" formControlName="phone">
        </label>
        <label>
          <span>E-mail</span>
          <input type="email" formControlName="email">
        </label>
        <label>
          <span>Endereco</span>
          <input type="text" formControlName="address">
        </label>
        <label>
          <span>Numero</span>
          <input type="text" formControlName="number">
        </label>
        <label>
          <span>Complemento</span>
          <input type="text" formControlName="complement">
        </label>
        <label>
          <span>Bairro</span>
          <input type="text" formControlName="district">
        </label>
        <label>
          <span>Cidade</span>
          <input type="text" formControlName="city">
        </label>
        <label>
          <span>UF</span>
          <input type="text" formControlName="state" maxlength="2">
        </label>
        <label>
          <span>CEP</span>
          <input type="text" formControlName="zipCode">
        </label>
        <label class="checkbox">
          <input type="checkbox" formControlName="isActive">
          <span>Ativo</span>
        </label>
        <div class="form-actions">
          <button type="submit" [disabled]="supplierForm.invalid || saving()">{{ editingSupplierId() ? 'Salvar fornecedor' : 'Cadastrar fornecedor' }}</button>
          <button type="button" class="secondary" (click)="cancelSupplierEdit()">Cancelar</button>
        </div>
      </form>

      <div class="table-wrap desktop-list" *ngIf="filteredSuppliers().length > 0; else noSuppliers">
        <table>
          <thead>
            <tr>
              <th>Fornecedor</th>
              <th>CNPJ</th>
              <th>Contato</th>
              <th>Telefone</th>
              <th>Status</th>
              <th>Acoes</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let supplier of filteredSuppliers()">
              <td>{{ supplier.tradeName || supplier.corporateName }}</td>
              <td>{{ formatCnpj(supplier.document) }}</td>
              <td>{{ supplier.contactName || '-' }}</td>
              <td>{{ supplier.phone || '-' }}</td>
              <td><span class="badge" [class.inactive]="!supplier.isActive">{{ supplier.isActive ? 'Ativo' : 'Inativo' }}</span></td>
              <td><button type="button" class="secondary" (click)="editSupplier(supplier)">Editar</button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="mobile-list" *ngIf="filteredSuppliers().length > 0">
        <article class="list-card" *ngFor="let supplier of filteredSuppliers()">
          <strong>{{ supplier.tradeName || supplier.corporateName }}</strong>
          <span>CNPJ: {{ formatCnpj(supplier.document) }}</span>
          <span>Contato: {{ supplier.contactName || '-' }}</span>
          <span>Telefone: {{ supplier.phone || '-' }}</span>
          <span class="badge" [class.inactive]="!supplier.isActive">{{ supplier.isActive ? 'Ativo' : 'Inativo' }}</span>
          <button type="button" class="secondary" (click)="editSupplier(supplier)">Editar</button>
        </article>
      </div>

      <ng-template #noSuppliers>
        <div class="state">Nenhum fornecedor encontrado.</div>
      </ng-template>
    </section>
  `,
  styles: [`
    .page-header,
    .tabs,
    .panel {
      background: var(--surface);
      border: 1px solid var(--border);
      border-radius: 8px;
      max-width: 100%;
      min-width: 0;
    }

    .page-header {
      margin-bottom: 16px;
      padding: 22px;
    }

    .eyebrow,
    label span,
    .section-title p {
      color: var(--muted);
      font-size: 12px;
      font-weight: 800;
    }

    .eyebrow {
      text-transform: uppercase;
    }

    h1 {
      font-size: 24px;
      margin: 4px 0 0;
    }

    h2 {
      font-size: 18px;
      margin: 0;
    }

    .tabs {
      display: flex;
      gap: 8px;
      margin-bottom: 16px;
      padding: 8px;
    }

    button {
      align-items: center;
      background: var(--primary);
      border-radius: 6px;
      color: #ffffff;
      display: inline-flex;
      font-weight: 800;
      justify-content: center;
      min-height: 40px;
      padding: 0 14px;
    }

    .tabs button,
    button.secondary {
      background: var(--surface-muted);
      color: var(--text);
    }

    .tabs button.active {
      background: var(--primary);
      color: #ffffff;
    }

    .panel {
      display: grid;
      gap: 16px;
      padding: 20px;
    }

    .section-title {
      align-items: center;
      display: flex;
      gap: 12px;
      justify-content: space-between;
      min-width: 0;
    }

    .section-title p {
      font-weight: 600;
      margin: 4px 0 0;
    }

    .search,
    label {
      display: grid;
      gap: 6px;
      min-width: 0;
    }

    input,
    select {
      border: 1px solid var(--border);
      border-radius: 6px;
      max-width: 100%;
      min-height: 40px;
      min-width: 0;
      padding: 0 10px;
      width: 100%;
    }

    .editor {
      border: 1px solid var(--border);
      border-radius: 8px;
      display: grid;
      gap: 12px;
      grid-template-columns: minmax(0, 1fr) 160px 120px;
      padding: 14px;
    }

    .supplier-editor {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }

    .checkbox {
      align-items: center;
      display: flex;
      gap: 8px;
    }

    .checkbox input {
      min-height: auto;
      width: auto;
    }

    .form-actions {
      display: flex;
      gap: 10px;
      grid-column: 1 / -1;
    }

    .table-wrap {
      max-width: 100%;
      overflow-x: auto;
    }

    table {
      border-collapse: collapse;
      min-width: 720px;
      width: 100%;
    }

    th,
    td {
      border-bottom: 1px solid var(--border);
      padding: 12px;
      text-align: left;
    }

    th {
      background: var(--surface-muted);
      color: var(--muted);
      font-size: 12px;
      text-transform: uppercase;
    }

    td {
      overflow-wrap: anywhere;
    }

    .badge {
      background: #dcfae6;
      border-radius: 999px;
      color: #067647;
      display: inline-flex;
      font-size: 12px;
      font-weight: 800;
      padding: 5px 9px;
      width: fit-content;
    }

    .badge.inactive {
      background: #fee4e2;
      color: #b42318;
    }

    .mobile-list {
      display: none;
    }

    .list-card {
      border: 1px solid var(--border);
      border-radius: 8px;
      display: grid;
      gap: 8px;
      max-width: 100%;
      min-width: 0;
      padding: 14px;
      width: 100%;
    }

    .list-card strong,
    .list-card span {
      overflow-wrap: anywhere;
    }

    .state {
      color: var(--muted);
      padding: 18px;
    }

    .error {
      color: var(--danger);
    }

    @media (max-width: 900px) {
      .editor,
      .supplier-editor {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 640px) {
      .page-header,
      .panel {
        padding: 16px;
      }

      .tabs {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .section-title,
      .form-actions {
        align-items: stretch;
        display: grid;
        grid-template-columns: 1fr;
      }

      button,
      .form-actions button {
        min-height: 44px;
        width: 100%;
      }

      .desktop-list {
        display: none;
      }

      .mobile-list {
        display: grid;
        gap: 12px;
      }
    }

    @media (max-width: 390px) {
      .page-header,
      .panel,
      .editor {
        padding: 14px;
      }
    }
  `]
})
export class RegistrationsPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly apiUrl = environment.apiUrl;

  readonly unitOptions = ['UN', 'PC', 'CX', 'SC', 'KG', 'G', 'T', 'M', 'M²', 'M³', 'L', 'ML', 'RL', 'BD', 'LT'];
  readonly activeTab = signal<RegistrationTab>('materials');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly materials = signal<CatalogItem[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly materialSearch = signal('');
  readonly supplierSearch = signal('');
  readonly showMaterialForm = signal(false);
  readonly showSupplierForm = signal(false);
  readonly editingMaterialId = signal<string | null>(null);
  readonly editingSupplierId = signal<string | null>(null);

  readonly filteredMaterials = computed(() => {
    const term = this.materialSearch().trim().toLowerCase();
    return this.materials().filter((item) =>
      item.description.toLowerCase().includes(term) ||
      item.unit.toLowerCase().includes(term)
    );
  });

  readonly filteredSuppliers = computed(() => {
    const term = this.supplierSearch().trim().toLowerCase();
    const digits = term.replace(/\D/g, '');

    return this.suppliers().filter((supplier) =>
      supplier.corporateName.toLowerCase().includes(term) ||
      supplier.tradeName.toLowerCase().includes(term) ||
      supplier.document.includes(digits)
    );
  });

  readonly materialForm = this.fb.nonNullable.group({
    description: ['', Validators.required],
    unit: ['UN', Validators.required],
    isActive: [true]
  });

  readonly supplierForm = this.fb.nonNullable.group({
    corporateName: ['', Validators.required],
    tradeName: [''],
    document: ['', [Validators.required, Validators.pattern(/^\d{2}\.\d{3}\.\d{3}\/\d{4}-\d{2}$/)]],
    stateRegistration: [''],
    contactName: [''],
    phone: [''],
    email: [''],
    address: [''],
    number: [''],
    complement: [''],
    district: [''],
    city: [''],
    state: [''],
    zipCode: [''],
    isActive: [true]
  });

  ngOnInit(): void {
    const tab = this.route.snapshot.queryParamMap.get('tab');
    if (tab === 'suppliers' || tab === 'materials') {
      this.activeTab.set(tab);
    }

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      materials: this.http.get<CatalogItem[]>(`${this.apiUrl}/Item`),
      suppliers: this.http.get<Supplier[]>(`${this.apiUrl}/Supplier`)
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ materials, suppliers }) => {
          this.materials.set(materials);
          this.suppliers.set(suppliers);
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  startNewMaterial(): void {
    this.editingMaterialId.set(null);
    this.materialForm.reset({ description: '', unit: 'UN', isActive: true });
    this.showMaterialForm.set(true);
  }

  editMaterial(material: CatalogItem): void {
    this.editingMaterialId.set(material.id);
    this.materialForm.setValue({
      description: material.description,
      unit: material.unit,
      isActive: material.isActive
    });
    this.showMaterialForm.set(true);
  }

  cancelMaterialEdit(): void {
    this.editingMaterialId.set(null);
    this.showMaterialForm.set(false);
  }

  saveMaterial(): void {
    if (this.materialForm.invalid || this.saving()) {
      this.materialForm.markAllAsTouched();
      return;
    }

    const value = this.materialForm.getRawValue();
    const editingId = this.editingMaterialId();
    const code = editingId
      ? this.materials().find((material) => material.id === editingId)?.code ?? this.generateMaterialCode()
      : this.generateMaterialCode();

    const operation = editingId
      ? this.http.put<CatalogItem>(`${this.apiUrl}/Item/${editingId}`, {
          code,
          description: value.description,
          unit: value.unit,
          isActive: value.isActive
        } satisfies UpdateCatalogItem)
      : this.http.post<CatalogItem>(`${this.apiUrl}/Item`, {
          categoryId: null,
          code,
          description: value.description,
          unit: value.unit,
          isActive: value.isActive
        } satisfies CreateCatalogItem);

    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (material) => {
          this.materials.update((items) => editingId
            ? items.map((item) => item.id === material.id ? material : item)
            : [...items, material]);
          this.cancelMaterialEdit();
          this.toast.success(editingId ? 'Material atualizado.' : 'Material cadastrado.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  startNewSupplier(): void {
    this.editingSupplierId.set(null);
    this.supplierForm.reset({
      corporateName: '',
      tradeName: '',
      document: '',
      stateRegistration: '',
      contactName: '',
      phone: '',
      email: '',
      address: '',
      number: '',
      complement: '',
      district: '',
      city: '',
      state: '',
      zipCode: '',
      isActive: true
    });
    this.showSupplierForm.set(true);
  }

  editSupplier(supplier: Supplier): void {
    this.editingSupplierId.set(supplier.id);
    this.supplierForm.setValue({
      corporateName: supplier.corporateName,
      tradeName: supplier.tradeName,
      document: this.formatCnpj(supplier.document),
      stateRegistration: supplier.stateRegistration ?? '',
      contactName: supplier.contactName,
      phone: supplier.phone,
      email: supplier.email,
      address: supplier.address,
      number: supplier.number,
      complement: supplier.complement ?? '',
      district: supplier.district,
      city: supplier.city,
      state: supplier.state,
      zipCode: supplier.zipCode,
      isActive: supplier.isActive
    });
    this.showSupplierForm.set(true);
  }

  cancelSupplierEdit(): void {
    this.editingSupplierId.set(null);
    this.showSupplierForm.set(false);
  }

  saveSupplier(): void {
    if (this.supplierForm.invalid || this.saving()) {
      this.supplierForm.markAllAsTouched();
      return;
    }

    const value = this.supplierForm.getRawValue();
    const editingId = this.editingSupplierId();
    const dto = {
      corporateName: value.corporateName,
      tradeName: value.tradeName,
      document: value.document,
      stateRegistration: value.stateRegistration || null,
      email: value.email,
      phone: value.phone,
      contactName: value.contactName,
      address: value.address,
      number: value.number,
      complement: value.complement || null,
      district: value.district,
      city: value.city,
      state: value.state,
      zipCode: value.zipCode,
      pixKey: null,
      bank: null,
      agency: null,
      account: null,
      isActive: value.isActive
    };

    const operation = editingId
      ? this.http.put<Supplier>(`${this.apiUrl}/Supplier/${editingId}`, dto satisfies UpdateSupplier)
      : this.http.post<Supplier>(`${this.apiUrl}/Supplier`, {
          companyId: null,
          ...dto
        } satisfies CreateSupplier);

    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (supplier) => {
          this.suppliers.update((items) => editingId
            ? items.map((item) => item.id === supplier.id ? supplier : item)
            : [...items, supplier]);
          this.cancelSupplierEdit();
          this.toast.success(editingId ? 'Fornecedor atualizado.' : 'Fornecedor cadastrado.');
        },
        error: (error) => this.toast.error(getApiErrorMessage(error))
      });
  }

  formatSupplierDocument(): void {
    const value = this.supplierForm.controls.document.value;
    this.supplierForm.controls.document.setValue(this.formatCnpj(value), { emitEvent: false });
  }

  formatCnpj(document: string): string {
    const digits = (document || '').replace(/\D/g, '').slice(0, 14);
    if (digits.length <= 2) return digits;
    if (digits.length <= 5) return `${digits.slice(0, 2)}.${digits.slice(2)}`;
    if (digits.length <= 8) return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5)}`;
    if (digits.length <= 12) return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8)}`;
    return `${digits.slice(0, 2)}.${digits.slice(2, 5)}.${digits.slice(5, 8)}/${digits.slice(8, 12)}-${digits.slice(12)}`;
  }

  private generateMaterialCode(): string {
    return `MAT-${Date.now().toString(36).toUpperCase()}`;
  }
}
