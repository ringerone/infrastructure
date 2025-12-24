# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Accept build argument for API URL
ARG API_URL=http://localhost:8080/api
ENV API_URL=$API_URL

# Copy package files
COPY package*.json ./
COPY angular.json ./
COPY tsconfig*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY src ./src
COPY *.json ./

# Update environment file with API URL using node
RUN node -e "const fs=require('fs');const content=fs.readFileSync('src/environments/environment.prod.ts','utf8');fs.writeFileSync('src/environments/environment.prod.ts',content.replace(/apiUrl:.*/, \"apiUrl: '${API_URL}'\"))"

# Build the Angular application
RUN npm run build

# Production stage
FROM nginx:alpine AS final

# Copy built files from build stage
# Angular outputs to dist/project-name, check if browser subdirectory exists
COPY --from=build /app/dist/infrastructure-configuration-ui /tmp/dist
RUN if [ -d /tmp/dist/browser ]; then \
        cp -r /tmp/dist/browser/* /usr/share/nginx/html/; \
    else \
        cp -r /tmp/dist/* /usr/share/nginx/html/; \
    fi && \
    rm -rf /tmp/dist

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

