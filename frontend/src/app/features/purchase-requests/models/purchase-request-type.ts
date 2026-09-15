export enum PurchaseRequestType {
  Material = 1,
  Service = 2
}

export const purchaseRequestTypeOptions = [
  { value: PurchaseRequestType.Material, label: 'Material' },
  { value: PurchaseRequestType.Service, label: 'Servico' }
];

export function getPurchaseRequestTypeLabel(type: PurchaseRequestType): string {
  const option = purchaseRequestTypeOptions.find((item) => item.value === type);

  return option?.label ?? 'Nao informado';
}
