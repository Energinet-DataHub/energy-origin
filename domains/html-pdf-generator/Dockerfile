FROM node:24-slim

WORKDIR /app

ENV PLAYWRIGHT_BROWSERS_PATH=/app/ms-playwright

COPY package.json .
COPY server.js .


RUN npm install

RUN npx playwright install chromium --with-deps --only-shell

EXPOSE 8080

USER node

CMD ["node", "server.js"]
