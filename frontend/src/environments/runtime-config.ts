type SgeRuntimeWindow = typeof globalThis & {
  __SGE_CONFIG__?: {
    apiUrl?: string;
  };
};

export const runtimeApiUrl = (globalThis as SgeRuntimeWindow).__SGE_CONFIG__?.apiUrl
  ?.replace(/\/+$/, '');

export const developmentApiUrl = runtimeApiUrl || 'http://localhost:5261/api';
