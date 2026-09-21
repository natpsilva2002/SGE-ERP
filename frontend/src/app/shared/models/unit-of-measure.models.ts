export interface UnitOfMeasure {
  id: string;
  code: string;
  description: string;
  isActive: boolean;
}

export interface CreateUnitOfMeasure {
  code: string;
  description: string;
}

export interface UpdateUnitOfMeasure {
  code: string;
  description: string;
  isActive: boolean;
}
