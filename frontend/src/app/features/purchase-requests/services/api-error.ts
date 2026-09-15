import { HttpErrorResponse } from '@angular/common/http';

interface ApiErrorBody {
  message?: string;
}

export function getApiErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ApiErrorBody | string | null;

    if (typeof body === 'object' && body?.message) {
      return body.message;
    }

    if (typeof body === 'string' && body.trim().length > 0) {
      return body;
    }

    if (error.status === 403) {
      return 'Voce nao tem permissao para executar esta acao.';
    }

    if (error.status === 404) {
      return 'Registro nao encontrado.';
    }
  }

  return 'Nao foi possivel concluir a operacao.';
}
