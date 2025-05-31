FROM node:24-slim

WORKDIR /app

ENV PLAYWRIGHT_BROWSERS_PATH=/app/ms-playwright

COPY . .

RUN npm ci

RUN npx playwright install chromium --with-deps --only-shell

HEALTHCHECK --interval=10s --timeout=5s --start-period=5s CMD \
  wget -qO- http://localhost:8080/health || exit 1

EXPOSE 8080

USER node

CMD ["node", "server.js"]
