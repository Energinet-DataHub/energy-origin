# How to run Structurizr locally

Note additional DataHub documentation on [C4 and Structurizr](https://energinet.atlassian.net/wiki/spaces/D3/pages/411926533/Visualizing+software+architecture+and+design).

## Prerequisites

You will need to have Docker installed.

## Using Structurizr Lite

### Starting

While in the `doc/diagrams/c4-model` directory, run the following command:
```
docker compose up -d
```

### Viewing
With started, you can head to [http://localhost:8080](http://localhost:8080) in your web browser to view the rendered outputs.

### Stopping

While in the `doc/diagrams/c4-model` directory, run the following command:
```
docker compose down
```
