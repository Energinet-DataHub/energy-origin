FROM datafolklabs/markdown AS builder

WORKDIR /static-files
COPY src/ .

RUN find . -type f -iname "*.md" -exec sh -c 'markdown $0 > ${0/.md/.html}' {} \;

FROM alpine:3

WORKDIR /static-files
COPY --from=builder /static-files/ .
