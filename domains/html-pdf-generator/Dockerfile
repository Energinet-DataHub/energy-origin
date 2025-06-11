FROM node:22-slim

WORKDIR /app

ENV PLAYWRIGHT_BROWSERS_PATH=/app/ms-playwright

COPY . .

RUN npm ci

RUN npx playwright install chromium --with-deps

EXPOSE 8080

USER node

CMD ["node", "server.js"]
