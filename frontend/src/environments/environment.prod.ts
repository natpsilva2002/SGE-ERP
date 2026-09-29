import { runtimeApiUrl } from './runtime-config';

if (!runtimeApiUrl) {
  throw new Error('API_URL nao foi configurada para o frontend em producao.');
}

export const environment = {
  production: true,
  apiUrl: runtimeApiUrl
};
