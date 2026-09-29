FROM node:22-alpine AS build
WORKDIR /workspace
COPY frontend/package.json frontend/package-lock.json ./frontend/
RUN npm ci --prefix frontend
COPY frontend/ ./frontend/
RUN npm run build --prefix frontend -- --configuration production

FROM nginx:stable-alpine AS runtime
ENV PORT=8080 NGINX_ENVSUBST_FILTER=^PORT$
COPY frontend/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY frontend/docker-entrypoint-runtime-config.sh /docker-entrypoint.d/40-sge-runtime-config.sh
COPY --from=build /workspace/frontend/dist/sge-erp-frontend/browser/ /usr/share/nginx/html/
RUN chmod +x /docker-entrypoint.d/40-sge-runtime-config.sh
EXPOSE 8080
