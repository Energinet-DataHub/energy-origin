#!/bin/bash
set -e

echo "Step 1: Generate signing key for melange"
docker run --rm -v "${PWD}":/work cgr.dev/chainguard/melange keygen

echo "Step 2: Create melange.yaml"
cat > melange.yaml << 'EOF'
package:
  name: html-pdf-generator
  version: 1.0.0
  description: HTML to PDF generator using Playwright
  target-architecture:
    - all
  copyright:
    - license: MIT
  dependencies:
    runtime:
      - nodejs-24
      - chromium
      - dumb-init
      - ca-certificates-bundle
      - fontconfig
      - freetype
      - harfbuzz
      - nss

environment:
  contents:
    keyring:
      - https://packages.wolfi.dev/os/wolfi-signing.rsa.pub
    repositories:
      - https://packages.wolfi.dev/os
    packages:
      - busybox
      - ca-certificates-bundle
      - nodejs-24
      - npm

pipeline:
  - name: Build application
    runs: |
      # Create destination directories
      APP_DIR="${{targets.destdir}}/usr/lib/html-pdf-generator"
      BIN_DIR="${{targets.destdir}}/usr/bin"
      mkdir -p "${APP_DIR}" "${BIN_DIR}"
      
      # Create package.json
      cat > "${APP_DIR}/package.json" << 'EOFINNER'
      {
        "name": "html-pdf-generator",
        "version": "1.0.0",
        "private": true,
        "dependencies": {
          "express": "^5.1.0",
          "playwright-core": "^1.52.0"
        }
      }
      EOFINNER
      
      # Create server.js
      cat > "${APP_DIR}/server.js" << 'EOFINNER'
      const express = require('express');
      const { chromium } = require('playwright-core');
      const app = express();

      app.use(express.json({limit: '10mb'}));

      app.get('/health', (_, res) => {
          res.sendStatus(200);
      });

      app.post('/generate-pdf', async (req, res) => {
          const { html } = req.body;
          let browser;

          if (!html) {
              return res.status(400).end('HTML content is required');
          }

          try {
              browser = await chromium.launch({ 
                  headless: true,
                  executablePath: '/usr/bin/chromium-browser'
              });
              
              const context = await browser.newContext({
                  offline: true,
                  javaScriptEnabled: false,
                  bypassCSP: false,
                  acceptDownloads: false,
                  ignoreHTTPSErrors: false,
                  serviceWorkers: 'block',
                  permissions: []
              });
              
              const page = await context.newPage();
              await page.setContent(html, { waitUntil: 'networkidle' });

              const buffer = await page.pdf({
                  format: 'A4',
                  printBackground: true,
                  margin: { top: '30px', right: '30px', bottom: '30px', left: '30px' }
              });

              res.set({
                  'Content-Type': 'application/pdf',
                  'Content-Length': buffer.length
              });

              res.end(buffer);
          } catch (error) {
              console.error(error);
              res.status(500).end('An error occurred while generating the PDF');
          } finally {
              if (browser) {
                  await browser.close();
              }
          }
      });

      app.listen(8080, '0.0.0.0', () => console.log('PDF generation service started on port 8080'));
      EOFINNER
      
      # Create executable wrapper
      cat > "${BIN_DIR}/html-pdf-generator" << 'EOFINNER'
      #!/bin/sh
      exec node /usr/lib/html-pdf-generator/server.js "$@"
      EOFINNER
      
      # Make sure the executable has proper permissions
      chmod +x "${BIN_DIR}/html-pdf-generator"
      
      # Install dependencies - THIS HAPPENS ONLY DURING BUILD
      cd "${APP_DIR}"
      npm install --omit=dev --no-package-lock
      
      # Remove npm cache and unnecessary files to keep the APK small
      rm -rf ~/.npm
EOF

echo "Step 3: Create apko.yaml"
cat > apko.yaml << 'EOF'
contents:
  keyring:
    - https://packages.wolfi.dev/os/wolfi-signing.rsa.pub
    - ./melange.rsa.pub
  repositories:
    - https://packages.wolfi.dev/os
    - "@local /work/packages"
  packages:
    - html-pdf-generator@local

entrypoint:
  command: /usr/bin/dumb-init

cmd: node /usr/lib/html-pdf-generator/server.js

work-dir: /

accounts:
  groups:
    - groupname: nonroot
      gid: 65532
  users:
    - username: nonroot
      uid: 65532
  run-as: nonroot

environment:
  NODE_ENV: "production"
  PLAYWRIGHT_BROWSERS_PATH: "/usr/bin"

paths:
  - path: /tmp
    type: directory
    uid: 65532
    gid: 65532
    permissions: 0o1777
EOF

echo "Step 4: Build the APK package with melange"
docker run --privileged --rm -v "${PWD}":/work \
  cgr.dev/chainguard/melange build melange.yaml \
  --arch amd64 \
  --signing-key melange.rsa

echo "Step 5: Build the container image with apko"
docker run --rm -v "${PWD}":/work -w /work cgr.dev/chainguard/apko build \
  apko.yaml \
  html-pdf-generator:latest \
  html-pdf-generator.tar \
  --arch amd64

echo "Step 6: Load the image into Docker"
docker load < html-pdf-generator.tar

# Add this step to tag the image without architecture suffix
echo "Step 7: Tag the image without architecture suffix"
docker tag html-pdf-generator:latest-amd64 html-pdf-generator:latest

echo "Build complete! Run with: docker run -p 8080:8080 html-pdf-generator:latest"
