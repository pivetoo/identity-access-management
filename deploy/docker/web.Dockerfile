FROM node:20-bookworm-slim AS build
WORKDIR /src

COPY . /src/system/identity-access-management
COPY --from=archon-ui . /src/frameworks/archon-ui

WORKDIR /src/system/identity-access-management/IdentityManagement/IdentityManagement.Web

ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

RUN npm ci
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY deploy/nginx/web.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/system/identity-access-management/IdentityManagement/IdentityManagement.Web/dist /usr/share/nginx/html

EXPOSE 80
