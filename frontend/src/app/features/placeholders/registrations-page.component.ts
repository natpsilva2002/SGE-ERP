import { DatePipe, NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../shared/feedback/toast.service';
import { getApiErrorMessage } from '../purchase-requests/services/api-error';
import { AdministrationService } from '../administration/services/administration.service';
import { AdminRole, AdminUser, CreateAdminUser, UpdateAdminUser } from '../administration/models/administration.models';
import {
  CatalogItem,
  CreateCatalogItem,
  UpdateCatalogItem,
  Work
} from '../purchase-requests/models/purchase-request.models';
import { CreateSupplier, Supplier, UpdateSupplier } from '../quotations/models/quotation.models';
import { UnitOfMeasure, CreateUnitOfMeasure, UpdateUnitOfMeasure } from '../../shared/models/unit-of-measure.models';
import { UnitOfMeasureService } from '../../shared/services/unit-of-measure.service';

type RegistrationTab = 'materials' | 'suppliers' | 'works' | 'users' | 'units';

@Component({
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, ReactiveFormsModule],
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
      <button type="button" [class.active]="activeTab() === 'works'" (click)="activeTab.set('works')">Obras</button>
      <button type="button" [class.active]="activeTab() === 'units'" (click)="activeTab.set('units')">Unidades de medida</button>
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
          <select formControlName="unitOfMeasureId">
            <option value="">Selecione uma unidade</option>
            <option *ngFor="let unit of activeUnits()" [value]="unit.id">{{ unit.code }} — {{ unit.description }}</option>
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

    <section class="panel" *ngIf="!loading() && activeTab() === 'works'">
      <div class="section-title">
        <div><h2>Obras cadastradas</h2><p>Use a obra existente para vincular solicitações e contratos.</p></div>
        <button type="button" (click)="startNewWork()">+ Adicionar obra</button>
      </div>
      <label class="search"><span>Buscar obra</span><input type="search" [value]="workSearch()" (input)="workSearch.set($any($event.target).value)" placeholder="Código ou nome..."></label>
      <form class="editor work-editor" *ngIf="showWorkForm()" [formGroup]="workForm" (ngSubmit)="saveWork()">
        <label><span>Código</span><input type="text" formControlName="code"></label>
        <label><span>Nome</span><input type="text" formControlName="name"></label>
        <label><span>Data de início</span><input type="date" formControlName="startDate"></label>
        <label class="wide"><span>Descrição</span><input type="text" formControlName="description"></label>
        <label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Ativa</span></label>
        <div class="form-actions"><button type="submit" [disabled]="workForm.invalid || saving()">{{ editingWorkId() ? 'Salvar obra' : 'Cadastrar obra' }}</button><button type="button" class="secondary" (click)="cancelWorkEdit()">Cancelar</button></div>
      </form>
      <div class="table-wrap desktop-list" *ngIf="filteredWorks().length > 0; else noWorks"><table><thead><tr><th>Código</th><th>Obra</th><th>Início</th><th>Status</th><th>Ações</th></tr></thead><tbody><tr *ngFor="let work of filteredWorks()"><td>{{ work.code }}</td><td>{{ work.name }}</td><td>{{ work.startDate | date:'dd/MM/yyyy' }}</td><td><span class="badge" [class.inactive]="!work.isActive">{{ work.isActive ? 'Ativa' : 'Inativa' }}</span></td><td><button type="button" class="secondary" (click)="editWork(work)">Editar</button><button *ngIf="work.isActive" type="button" class="danger-button" (click)="deactivateWork(work)">Inativar</button></td></tr></tbody></table></div>
      <div class="mobile-list" *ngIf="filteredWorks().length > 0"><article class="list-card" *ngFor="let work of filteredWorks()"><strong>{{ work.code }} — {{ work.name }}</strong><span class="badge" [class.inactive]="!work.isActive">{{ work.isActive ? 'Ativa' : 'Inativa' }}</span><button type="button" class="secondary" (click)="editWork(work)">Editar</button></article></div>
      <ng-template #noWorks><div class="state">Nenhuma obra encontrada.</div></ng-template>
    </section>

    <section class="panel" *ngIf="!loading() && activeTab() === 'users'">
      <div class="section-title"><div><h2>Usuários</h2><p>Gestão do mesmo cadastro usado na Administração.</p></div><button type="button" (click)="startNewUser()">Cadastrar usuário</button></div>
      <label class="search"><span>Buscar usuário</span><input type="search" [value]="userSearch()" (input)="userSearch.set($any($event.target).value)" placeholder="Nome ou e-mail..."></label>
      <form class="editor user-editor" *ngIf="showUserForm()" [formGroup]="userForm" (ngSubmit)="saveUser()">
        <label><span>Nome</span><input type="text" formControlName="firstName"></label><label><span>Sobrenome</span><input type="text" formControlName="lastName"></label><label><span>E-mail</span><input type="email" formControlName="email"></label><label><span>Telefone</span><input type="text" formControlName="phoneNumber"></label><label><span>Perfil</span><select formControlName="roleId"><option value="">Selecione um perfil</option><option *ngFor="let role of roles" [value]="role.id">{{ role.name }}</option></select></label><label *ngIf="!editingUserId()"><span>Senha</span><input type="password" formControlName="password"></label><label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Ativo</span></label>
        <div class="form-actions"><button type="submit" [disabled]="userForm.invalid || saving()">Salvar</button><button type="button" class="secondary" (click)="cancelUserEdit()">Cancelar</button></div>
      </form>
      <div class="table-wrap desktop-list" *ngIf="filteredUsers().length > 0; else noUsers"><table><thead><tr><th>Nome</th><th>E-mail</th><th>Perfil</th><th>Status</th><th>Ações</th></tr></thead><tbody><tr *ngFor="let user of filteredUsers()"><td>{{ user.firstName }} {{ user.lastName }}</td><td>{{ user.email }}</td><td>{{ user.role }}</td><td><span class="badge" [class.inactive]="!user.isActive">{{ user.isActive ? 'Ativo' : 'Inativo' }}</span></td><td><button type="button" class="secondary" (click)="editUser(user)">Editar</button><button *ngIf="user.isActive" type="button" class="danger-button" (click)="deactivateUser(user)">Inativar</button></td></tr></tbody></table></div>
      <div class="mobile-list" *ngIf="filteredUsers().length > 0"><article class="list-card" *ngFor="let user of filteredUsers()"><strong>{{ user.firstName }} {{ user.lastName }}</strong><span>{{ user.email }} · {{ user.role }}</span><span class="badge" [class.inactive]="!user.isActive">{{ user.isActive ? 'Ativo' : 'Inativo' }}</span><button type="button" class="secondary" (click)="editUser(user)">Editar</button></article></div>
      <ng-template #noUsers><div class="state">Nenhum usuário encontrado.</div></ng-template>
    </section>

    <section class="panel" *ngIf="!loading() && activeTab() === 'units'">
      <div class="section-title"><div><h2>Unidades de medida</h2><p>Unidades ativas ficam disponíveis em Materiais e Serviços.</p></div><button type="button" (click)="startNewUnit()">+ Adicionar unidade</button></div>
      <label class="search"><span>Buscar unidade</span><input type="search" [value]="unitSearch()" (input)="unitSearch.set($any($event.target).value)" placeholder="Código ou descrição..."></label>
      <form class="editor unit-editor" *ngIf="showUnitForm()" [formGroup]="unitForm" (ngSubmit)="saveUnit()"><label><span>Código / Sigla</span><input type="text" formControlName="code" maxlength="20"></label><label class="wide"><span>Descrição</span><input type="text" formControlName="description"></label><label class="checkbox"><input type="checkbox" formControlName="isActive"><span>Ativa</span></label><div class="form-actions"><button type="submit" [disabled]="unitForm.invalid || saving()">{{ editingUnitId() ? 'Salvar unidade' : 'Cadastrar unidade' }}</button><button type="button" class="secondary" (click)="cancelUnitEdit()">Cancelar</button></div></form>
      <div class="table-wrap desktop-list" *ngIf="filteredUnits().length > 0; else noUnits"><table><thead><tr><th>Código</th><th>Descrição</th><th>Status</th><th>Ações</th></tr></thead><tbody><tr *ngFor="let unit of filteredUnits()"><td><strong>{{ unit.code }}</strong></td><td>{{ unit.description }}</td><td><span class="badge" [class.inactive]="!unit.isActive">{{ unit.isActive ? 'Ativa' : 'Inativa' }}</span></td><td><button type="button" class="secondary" (click)="editUnit(unit)">Editar</button><button *ngIf="unit.isActive" type="button" class="danger-button" (click)="deactivateUnit(unit)">Inativar</button></td></tr></tbody></table></div>
      <div class="mobile-list" *ngIf="filteredUnits().length > 0"><article class="list-card" *ngFor="let unit of filteredUnits()"><strong>{{ unit.code }}</strong><span>{{ unit.description }}</span><span class="badge" [class.inactive]="!unit.isActive">{{ unit.isActive ? 'Ativa' : 'Inativa' }}</span><button type="button" class="secondary" (click)="editUnit(unit)">Editar</button></article></div>
      <ng-template #noUnits><div class="state">Nenhuma unidade encontrada.</div></ng-template>
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

    .work-editor,
    .unit-editor {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }

    .user-editor {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }

    .wide {
      grid-column: span 2;
    }

    .danger-button {
      background: transparent;
      color: var(--danger);
      margin-left: 8px;
      min-height: 32px;
      padding: 0 8px;
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
  private readonly administrationService = inject(AdministrationService);
  private readonly unitService = inject(UnitOfMeasureService);
  private readonly apiUrl = environment.apiUrl;

  readonly activeTab = signal<RegistrationTab>('materials');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly materials = signal<CatalogItem[]>([]);
  readonly suppliers = signal<Supplier[]>([]);
  readonly works = signal<Work[]>([]);
  readonly users = signal<AdminUser[]>([]);
  readonly units = signal<UnitOfMeasure[]>([]);
  roles: AdminRole[] = [];
  readonly materialSearch = signal('');
  readonly supplierSearch = signal('');
  readonly workSearch = signal('');
  readonly userSearch = signal('');
  readonly unitSearch = signal('');
  readonly showMaterialForm = signal(false);
  readonly showSupplierForm = signal(false);
  readonly showWorkForm = signal(false);
  readonly showUserForm = signal(false);
  readonly showUnitForm = signal(false);
  readonly editingMaterialId = signal<string | null>(null);
  readonly editingSupplierId = signal<string | null>(null);
  readonly editingWorkId = signal<string | null>(null);
  readonly editingUserId = signal<string | null>(null);
  readonly editingUnitId = signal<string | null>(null);

  readonly activeUnits = computed(() => this.units().filter((unit) => unit.isActive));

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

  readonly filteredWorks = computed(() => {
    const term = this.workSearch().trim().toLowerCase();
    return this.works().filter((work) =>
      work.code.toLowerCase().includes(term) ||
      work.name.toLowerCase().includes(term)
    );
  });

  readonly filteredUsers = computed(() => {
    const term = this.userSearch().trim().toLowerCase();
    return this.users().filter((user) =>
      `${user.firstName} ${user.lastName}`.toLowerCase().includes(term) ||
      user.email.toLowerCase().includes(term) ||
      user.role.toLowerCase().includes(term)
    );
  });

  readonly filteredUnits = computed(() => {
    const term = this.unitSearch().trim().toLowerCase();
    return this.units().filter((unit) =>
      unit.code.toLowerCase().includes(term) || unit.description.toLowerCase().includes(term)
    );
  });

  readonly materialForm = this.fb.nonNullable.group({
    description: ['', Validators.required],
    unitOfMeasureId: ['', Validators.required],
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

  readonly workForm = this.fb.nonNullable.group({
    code: ['', Validators.required],
    name: ['', Validators.required],
    description: [''],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required],
    isActive: [true]
  });

  readonly userForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    phoneNumber: [''],
    roleId: ['', Validators.required],
    isActive: [true]
  });

  readonly unitForm = this.fb.nonNullable.group({
    code: ['', Validators.required],
    description: ['', Validators.required],
    isActive: [true]
  });

  ngOnInit(): void {
    const tab = this.route.snapshot.queryParamMap.get('tab');
    if (tab === 'suppliers' || tab === 'materials' || tab === 'works' || tab === 'units') {
      this.activeTab.set(tab);
    }

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      materials: this.http.get<CatalogItem[]>(`${this.apiUrl}/Item`),
      suppliers: this.http.get<Supplier[]>(`${this.apiUrl}/Supplier`),
      works: this.http.get<Work[]>(`${this.apiUrl}/Work`),
      users: this.administrationService.getUsers(),
      roles: this.administrationService.getRoles(),
      units: this.unitService.getAll()
    }).pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ materials, suppliers, works, users, roles, units }) => {
          this.materials.set(materials);
          this.suppliers.set(suppliers);
          this.works.set(works);
          this.users.set(users);
          this.roles = roles;
          this.units.set(units);
          if (!this.materialForm.controls.unitOfMeasureId.value && units.length > 0) {
            this.materialForm.controls.unitOfMeasureId.setValue(units.find((unit) => unit.isActive)?.id ?? '');
          }
        },
        error: (error) => this.error.set(getApiErrorMessage(error))
      });
  }

  startNewMaterial(): void {
    this.editingMaterialId.set(null);
    this.materialForm.reset({ description: '', unitOfMeasureId: this.activeUnits()[0]?.id ?? '', isActive: true });
    this.showMaterialForm.set(true);
  }

  editMaterial(material: CatalogItem): void {
    this.editingMaterialId.set(material.id);
    this.materialForm.setValue({
      description: material.description,
      unitOfMeasureId: material.unitOfMeasureId ?? this.activeUnits().find((unit) => unit.code === material.unit)?.id ?? '',
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
          unit: this.activeUnits().find((unit) => unit.id === value.unitOfMeasureId)?.code ?? '',
          unitOfMeasureId: value.unitOfMeasureId,
          isActive: value.isActive
        } satisfies UpdateCatalogItem)
      : this.http.post<CatalogItem>(`${this.apiUrl}/Item`, {
          categoryId: null,
          code,
          description: value.description,
          unit: this.activeUnits().find((unit) => unit.id === value.unitOfMeasureId)?.code ?? '',
          unitOfMeasureId: value.unitOfMeasureId,
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

  startNewWork(): void {
    this.editingWorkId.set(null);
    this.workForm.reset({
      code: '',
      name: '',
      description: '',
      startDate: new Date().toISOString().slice(0, 10),
      isActive: true
    });
    this.showWorkForm.set(true);
  }

  editWork(work: Work): void {
    this.editingWorkId.set(work.id);
    this.workForm.setValue({
      code: work.code,
      name: work.name,
      description: work.description ?? '',
      startDate: work.startDate.slice(0, 10),
      isActive: work.isActive
    });
    this.showWorkForm.set(true);
  }

  cancelWorkEdit(): void {
    this.editingWorkId.set(null);
    this.showWorkForm.set(false);
  }

  saveWork(): void {
    if (this.workForm.invalid || this.saving()) {
      this.workForm.markAllAsTouched();
      return;
    }

    const value = this.workForm.getRawValue();
    const editingId = this.editingWorkId();
    const operation = editingId
      ? this.http.put<Work>(`${this.apiUrl}/Work/${editingId}`, {
          code: value.code,
          name: value.name,
          description: value.description || null,
          isActive: value.isActive
        })
      : this.http.post<Work>(`${this.apiUrl}/Work`, {
          code: value.code,
          name: value.name,
          description: value.description || null,
          startDate: `${value.startDate}T00:00:00`
        });

    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (work) => {
        this.works.update((items) => editingId
          ? items.map((item) => item.id === work.id ? work : item)
          : [...items, work]);
        this.cancelWorkEdit();
        this.toast.success(editingId ? 'Obra atualizada.' : 'Obra cadastrada.');
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  deactivateWork(work: Work): void {
    this.http.delete<void>(`${this.apiUrl}/Work/${work.id}`).subscribe({
      next: () => {
        this.works.update((items) => items.map((item) => item.id === work.id ? { ...item, isActive: false } : item));
        this.toast.success('Obra inativada.');
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  startNewUser(): void {
    this.editingUserId.set(null);
    this.userForm.reset({ firstName: '', lastName: '', email: '', password: '', phoneNumber: '', roleId: this.roles[0]?.id ?? '', isActive: true });
    this.showUserForm.set(true);
  }

  editUser(user: AdminUser): void {
    this.editingUserId.set(user.id);
    this.userForm.reset({ firstName: user.firstName, lastName: user.lastName, email: user.email, password: '', phoneNumber: user.phoneNumber ?? '', roleId: user.roleId, isActive: user.isActive });
    this.showUserForm.set(true);
  }

  cancelUserEdit(): void {
    this.editingUserId.set(null);
    this.showUserForm.set(false);
  }

  saveUser(): void {
    if (this.userForm.invalid || this.saving()) {
      this.userForm.markAllAsTouched();
      return;
    }

    const value = this.userForm.getRawValue();
    const editingId = this.editingUserId();
    if (!editingId && !value.password.trim()) {
      this.toast.error('Informe uma senha para o novo usuário.');
      return;
    }

    const base = {
      firstName: value.firstName,
      lastName: value.lastName,
      email: value.email,
      phoneNumber: value.phoneNumber || null,
      roleId: value.roleId,
      isActive: value.isActive
    };
    const operation = editingId
      ? this.administrationService.updateUser(editingId, base satisfies UpdateAdminUser)
      : this.administrationService.createUser({ ...base, password: value.password } satisfies CreateAdminUser);

    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (user) => {
        this.users.update((items) => editingId
          ? items.map((item) => item.id === user.id ? user : item)
          : [...items, user]);
        this.cancelUserEdit();
        this.toast.success(editingId ? 'Usuário atualizado.' : 'Usuário cadastrado.');
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  deactivateUser(user: AdminUser): void {
    this.administrationService.deactivateUser(user.id).subscribe({
      next: () => {
        this.users.update((items) => items.map((item) => item.id === user.id ? { ...item, isActive: false } : item));
        this.toast.success('Usuário inativado.');
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  startNewUnit(): void {
    this.editingUnitId.set(null);
    this.unitForm.reset({ code: '', description: '', isActive: true });
    this.showUnitForm.set(true);
  }

  editUnit(unit: UnitOfMeasure): void {
    this.editingUnitId.set(unit.id);
    this.unitForm.setValue({ code: unit.code, description: unit.description, isActive: unit.isActive });
    this.showUnitForm.set(true);
  }

  cancelUnitEdit(): void {
    this.editingUnitId.set(null);
    this.showUnitForm.set(false);
  }

  saveUnit(): void {
    if (this.unitForm.invalid || this.saving()) {
      this.unitForm.markAllAsTouched();
      return;
    }

    const value = this.unitForm.getRawValue();
    const editingId = this.editingUnitId();
    const operation = editingId
      ? this.unitService.update(editingId, value satisfies UpdateUnitOfMeasure)
      : this.unitService.create(value satisfies CreateUnitOfMeasure);

    this.saving.set(true);
    operation.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (unit) => {
        this.units.update((items) => editingId
          ? items.map((item) => item.id === unit.id ? unit : item)
          : [...items, unit]);
        this.cancelUnitEdit();
        this.toast.success(editingId ? 'Unidade atualizada.' : 'Unidade cadastrada.');
      },
      error: (error) => this.toast.error(getApiErrorMessage(error))
    });
  }

  deactivateUnit(unit: UnitOfMeasure): void {
    this.unitService.deactivate(unit.id).subscribe({
      next: () => {
        this.units.update((items) => items.map((item) => item.id === unit.id ? { ...item, isActive: false } : item));
        this.toast.success('Unidade inativada.');
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
