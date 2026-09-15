export enum PurchaseRequestStatus {
  Draft = 1,
  WaitingQuotation = 2,
  WaitingApproval = 3,
  Approved = 4,
  Rejected = 5,
  PurchaseOrderGenerated = 6,
  Finished = 7,
  Cancelled = 8,
  QuotationInProgress = 9
}

const statusLabels: Record<PurchaseRequestStatus, string> = {
  [PurchaseRequestStatus.Draft]: 'Rascunho',
  [PurchaseRequestStatus.WaitingQuotation]: 'Aguardando cotacao',
  [PurchaseRequestStatus.WaitingApproval]: 'Aguardando aprovacao',
  [PurchaseRequestStatus.Approved]: 'Aprovada',
  [PurchaseRequestStatus.Rejected]: 'Rejeitada',
  [PurchaseRequestStatus.PurchaseOrderGenerated]: 'Ordem de compra gerada',
  [PurchaseRequestStatus.Finished]: 'Finalizada',
  [PurchaseRequestStatus.Cancelled]: 'Cancelada',
  [PurchaseRequestStatus.QuotationInProgress]: 'Em cotacao'
};

export const purchaseRequestStatusOptions = Object.entries(statusLabels).map(
  ([value, label]) => ({
    value: Number(value),
    label
  })
);

export function getPurchaseRequestStatusLabel(status: PurchaseRequestStatus): string {
  return statusLabels[status] ?? 'Status desconhecido';
}
