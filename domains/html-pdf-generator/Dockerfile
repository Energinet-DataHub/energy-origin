FROM cgr.dev/chainguard/wolfi-base:latest AS build

RUN apk add --no-cache \
    nodejs-24 \
    npm \
    chromium

RUN mkdir -p /home/nonroot/.config/chromium && \
    chown -R 65532:65532 /home/nonroot && \
    chmod 755 /home/nonroot /home/nonroot/.config /home/nonroot/.config/chromium

WORKDIR /app
COPY package.json .
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=true
RUN npm install --omit=dev
COPY server.js .

RUN mkdir -p /runtime-root/usr/lib /runtime-root/usr/bin /runtime-root/app /runtime-root/home && \
    cp -a /usr/lib /runtime-root/usr/ && \
    cp -a /usr/bin /runtime-root/usr/ && \
    cp -a /home/nonroot /runtime-root/home/ && \
    ln -sf chromium /runtime-root/usr/bin/chromium-browser && \
    cp -a /app/* /runtime-root/app && \
    rm -rf /runtime-root/usr/lib/node_modules /runtime-root/usr/lib/apk

RUN rm -f /runtime-root/usr/bin/busybox && \
    rm -f /runtime-root/usr/bin/npm || true && \
    rm -rf /runtime-root/usr/share/busybox \
           /runtime-root/usr/share/doc/busybox \
           /runtime-root/usr/lib/busybox* \
               /runtime-root/usr/share/npm \
               /runtime-root/usr/lib/node_modules/npm

FROM cgr.dev/chainguard/glibc-dynamic:latest AS final

COPY --from=build /runtime-root/usr /usr
COPY --from=build /runtime-root/home /home
COPY --from=build /lib /lib
COPY --from=build /runtime-root/app /app

WORKDIR /app
USER 65532:65532

ENV HOME=/home/nonroot
ENV CRASHPAD_DB_DIR=/home/nonroot/.config/chromium/Crash\ Reports
ENV LANG=C.UTF-8
EXPOSE 8080
CMD ["node", "server.js"]
