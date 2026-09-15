export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL'
  }).format(value);
}

export function formatDeliveryDays(days: number): string {
  return days === 1 ? '1 dia' : `${days} dias`;
}
