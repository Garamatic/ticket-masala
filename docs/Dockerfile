# Build stage - Generate static documentation
FROM python:3.11-slim as builder

WORKDIR /build

# Copy requirements and install dependencies
COPY requirements-docs.txt .
RUN pip install --no-cache-dir -r requirements-docs.txt

# Copy documentation source
COPY . .

# Build the static site
RUN mkdocs build --config-file mkdocs.yml

# Production stage - Serve with nginx
FROM nginx:alpine

# Copy built site from builder (site dir is now ../site relative to /build)
COPY --from=builder /site /usr/share/nginx/html

# Create optimized nginx configuration
RUN echo 'server { \
    listen 8080; \
    server_name _; \
    root /usr/share/nginx/html; \
    index index.html; \
    \
    # Enable gzip compression \
    gzip on; \
    gzip_vary on; \
    gzip_min_length 1024; \
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json; \
    \
    # Security headers \
    add_header X-Frame-Options "SAMEORIGIN" always; \
    add_header X-Content-Type-Options "nosniff" always; \
    add_header X-XSS-Protection "1; mode=block" always; \
    \
    # Cache static assets \
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ { \
    expires 1y; \
    add_header Cache-Control "public, immutable"; \
    } \
    \
    # SPA fallback \
    location / { \
    try_files $uri $uri/ /index.html; \
    } \
    \
    # Health check endpoint \
    location /health { \
    access_log off; \
    return 200 "healthy\n"; \
    add_header Content-Type text/plain; \
    } \
    }' > /etc/nginx/conf.d/default.conf

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
