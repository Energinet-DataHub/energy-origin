FROM cgr.dev/chainguard/wolfi-base:latest AS build

RUN apk add --no-cache \
        nodejs-24 \
        npm \
        chromium

WORKDIR /app
COPY package.json .
RUN npm install --omit=dev
COPY server.js .

FROM cgr.dev/chainguard/glibc-dynamic:latest

COPY --from=build /usr /usr
COPY --from=build /lib /lib

COPY --from=build /app /app

WORKDIR /app
USER 65532:65532

ENV LANG=C.UTF-8
EXPOSE 8080
CMD ["node", "server.js"]
