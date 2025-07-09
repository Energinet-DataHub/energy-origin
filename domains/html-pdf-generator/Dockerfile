FROM node:24-slim

WORKDIR /app

ENV PLAYWRIGHT_BROWSERS_PATH=/app/ms-playwright

COPY . .

RUN npm ci

RUN npx playwright-core install chromium --with-deps --only-shell

EXPOSE 8080

USER node

CMD ["node", "server.js"]
