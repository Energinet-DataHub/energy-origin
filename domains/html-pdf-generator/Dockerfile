FROM cgr.dev/chainguard/wolfi-base:latest AS build

RUN apk update && \
    apk add --no-cache \
    nodejs-24 \
    npm

WORKDIR /app

COPY package.json .

RUN npm install

COPY server.js .

FROM cgr.dev/chainguard/wolfi-base:latest

RUN apk update && \
    apk add --no-cache \
    nodejs-24 \
    chromium

RUN adduser -D -u 1000 node && \
    mkdir -p /app && chown node:node /app

RUN mkdir -p /usr/lib/chromium && \
    chown -R node:node /usr/lib/chromium

WORKDIR /app

COPY --from=build --chown=node:node /app/node_modules /app/node_modules
COPY --from=build --chown=node:node /app/server.js /app/server.js

USER node

EXPOSE 8080

CMD ["node", "server.js"]
