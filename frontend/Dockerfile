# Stage 1: Build Vue.js app and generate static files
FROM node:22-alpine AS build-stage
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: Serve static files using NGINX
FROM nginx:1.23.3
COPY --from=build-stage /app/dist /usr/share/nginx/html
COPY default.conf.template /etc/nginx/conf.d/default.conf.template
COPY generate-config.sh /docker-entrypoint.d/generate-config.sh
RUN chmod +x /docker-entrypoint.d/generate-config.sh
CMD /bin/bash -c "\
  /docker-entrypoint.d/generate-config.sh && \
  envsubst '\$PORT' < /etc/nginx/conf.d/default.conf.template > /etc/nginx/conf.d/default.conf && \
  nginx -g 'daemon off;'"