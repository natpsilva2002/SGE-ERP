export interface Dashboard {
  role: string;
  cards: DashboardCard[];
  pending: DashboardEntry[];
  recentActivities: DashboardEntry[];
}

export interface DashboardCard {
  key: string;
  label: string;
  value?: number | null;
  amount?: number | null;
  route?: string | null;
}

export interface DashboardEntry {
  type: string;
  title: string;
  description: string;
  date: string;
  responsible?: string | null;
  status?: string | null;
  route?: string | null;
}

export interface DashboardWorkOption {
  id: string;
  code: string;
  name: string;
}

export interface WorkCostsSummary {
  contracted: number;
  paid: number;
  pending: number;
  materials: number;
  services: number;
}

export interface WorkCostEvolution {
  period: string;
  materials: number;
  services: number;
  total: number;
}

export interface WorkSupplierCost {
  supplierId?: string | null;
  supplier: string;
  type: string;
  contracted: number;
  paid: number;
  pending: number;
  share: number;
  route?: string | null;
}

export interface WorkTopCost {
  description: string;
  type: string;
  amount: number;
  route?: string | null;
}

export interface WorkCostDetail {
  type: string;
  document: string;
  supplier: string;
  description: string;
  contracted: number;
  paid: number;
  pending: number;
  status: string;
  date: string;
  route?: string | null;
}

export interface WorkPayment {
  date: string;
  work: string;
  type: string;
  document: string;
  supplier: string;
  amount: number;
  paymentMethod: string;
  responsible: string;
  route?: string | null;
}

export interface WorkCosts {
  workId?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  works: DashboardWorkOption[];
  summary: WorkCostsSummary;
  evolution: WorkCostEvolution[];
  suppliers: WorkSupplierCost[];
  topCosts: WorkTopCost[];
  details: WorkCostDetail[];
  payments: WorkPayment[];
}
