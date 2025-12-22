# Build stage - Generate static documentation
FROM python:3.11-slim as builder

WORKDIR /docs

# Copy requirements and install dependencies
COPY requirements-docs.txt .
RUN pip install --no-cache-dir -r requirements-docs.txt

# Copy documentation source
COPY . .

# Build the static site
RUN mkdocs build

# Production stage - Serve with nginx
FROM nginx:alpine

# Copy built site from builder
COPY --from=builder /docs/site /usr/share/nginx/html

# Copy nginx configuration
RUN echo 'server { \
    listen 80; \
    server_name _; \
    root /usr/share/nginx/html; \
    index index.html; \
    location / { \
        try_files $uri $uri/ /index.html; \
    } \
    # Enable gzip compression \
    gzip on; \
    gzip_vary on; \
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/xml+rss application/json; \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
