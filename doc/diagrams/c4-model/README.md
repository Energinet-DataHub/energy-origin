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

# Viewing diagrams

The diagrams are published with GitHub Pages and are available [here](https://energinet-datahub.github.io/energy-origin/), but also rendered below:

<details closed>
  <summary>Energy Origin System Context</summary>
  
  ![SystemContextEO](https://energinet-datahub.github.io/energy-origin/doc/diagrams/c4-model/views/SystemContextEO.png)
</details>
<details closed>
  <summary>Auth Container Context</summary>
  
  ![Auth](https://energinet-datahub.github.io/energy-origin/doc/diagrams/c4-model/views/Auth.png)
</details>
<details closed>
  <summary>Certificate Container Context</summary>
  
  ![Certificate](https://energinet-datahub.github.io/energy-origin/doc/diagrams/c4-model/views/Certificate.png)
</details>
