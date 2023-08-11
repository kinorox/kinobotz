# Use an official NGINX image as the base image
FROM nginx:alpine

# Remove default NGINX configuration
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom NGINX configuration
COPY nginx.conf /etc/nginx/conf.d/

# Create a directory to hold the built Vue.js application files
WORKDIR /usr/share/nginx/html

# Copy the built application files from the 'public' directory to the NGINX serving directory
COPY public .

# Expose the default NGINX port
EXPOSE $PORT

# Start NGINX
CMD ["nginx", "-g", "daemon off;"]