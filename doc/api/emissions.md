
# Emissions domain

# Get emissions

The emissions api should take three query parameters

## Request

```text
GET /api/emissions
        ?dateFrom=1514826000
        &dateTo=1514864000
        &aggregation=TOTAL   
```

## Parameters

- dateFrom: [UNIX timestamp](best-practices.md#date-from-and-to)
- dateTo: [UNIX timestamp](best-practices.md#date-from-and-to)
- aggregation: [aggregation ENUM](best-practices.md#aggregation)

## Response

```json
{
    "emissions": [
        {
            "dateFrom": 1514826000, 
            "dateTo": 1514864000,  
            "co2": 1241245534.213
        }
    ]
  
}
```


## Internal call structure

[Link to EDS](https://www.energidataservice.dk/tso-electricity/declarationemissionhour)

```mermaid
sequenceDiagram
    participant spa as Single Page Application
    participant em as Emissions Domain
    participant ds as DataSync Domain
    participant eds as EnergiDataService

    spa->>+ em: GET /api/emissions/total  ?from:DateTime  &to: DateTime
    
    em->>+ ds: GET /meteringpoint
    ds--)- em: List<MeteringPointDTO>
    
    loop For each mp
      em ->>+ ds: GET /meteringpoint/{gsrn}/measurements  ?from:DateTime  &to: DateTime 
        
      ds --)- em: List< MeasurementDTO>
    end
    em->>+ eds: GET https://www.energidataservice.dk/tso-electricity/declarationemissionhour
    eds--)- em: {...}

    em->> em: calculateEmissions

    em->>- spa: TotalEmissionDTO

```
