# Redoc Domain Overview

The `redoc` domain is responsible for serving API documentation using Redoc. Its structure includes:

## Structure

- **api-info.yaml**: OpenAPI specification or API information for documentation.
- **configuration.yaml**: Configuration file for the Redoc service.
- **health.html**: Health check or status page for the service.
- **nginx.conf**: NGINX configuration for serving the documentation.
- **redoc-template.hbs**: Handlebars template for customizing the Redoc UI.
- **Dockerfile**: Containerization support for running the documentation service in Docker.

## Key Responsibilities
- Serving interactive API documentation based on OpenAPI specs.
- Providing configuration and templating for custom documentation experiences.
- Supporting containerized deployment with Docker and NGINX.

---

For more details, see the configuration and template files in this domain.

