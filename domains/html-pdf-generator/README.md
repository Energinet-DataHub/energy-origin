# HTML PDF GENERATOR

Run this command

```shell
docker build -t html-pdf-generator:latest . \
&& docker run -d -p 8080:8080 html-pdf-generator:latest
```

Then try sending it some html to render

```shell
curl -X POST http://localhost:8080/generate-pdf \
  -H "Content-Type: application/json" \
  -d "$(jq -n --arg html "$(cat example.html)" '{html: $html}')" \
  --output example.pdf
```
