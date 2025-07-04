# Quickstart Guide

This guide will help you set up your development environment, build, and run the Energy Origin system locally.

## Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (version specified in `global.json`)
- [Node.js](https://nodejs.org/) (for JavaScript/TypeScript services)
- [Docker](https://www.docker.com/) (for containerized services)
- [Git](https://git-scm.com/)

## Setup Steps
1. **Clone the repository:**
   ```sh
   git clone <repo-url>
   cd energy-origin
   ```
2. **Restore dependencies:**
   ```sh
   dotnet restore
   npm install # For JS/TS services, run in relevant folders
   ```
3. **Build the solution:**
   ```sh
   dotnet build
   # For JS/TS services, use npm run build if needed
   ```
4. **Run services locally:**
   - Use `dotnet run` for .NET APIs (e.g., in `domains/transfer/Transfer.API/`)
   - Use `npm start` or `node server.js` for Node.js services (e.g., `html-pdf-generator`)
   - Use Docker Compose or individual Dockerfiles for containerized services

## Environment Variables
- Check each domain's README or configuration files for required environment variables.

## Additional Resources
- [CI/CD Workflows](../doc/workflows/)
- [API Documentation](../doc/api/)
- [Architecture Overview](architecture.md)

---

For more details, see the README files in each domain or consult the documentation in the `doc/` folder.

