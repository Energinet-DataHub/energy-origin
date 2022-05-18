
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

    spa->>+ em: GET /api/emissions  ?from:DateTime  &to: DateTime
    
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



# Get Sources of Energy

This endpoint returns the personal mix of energy for the period.
## Request

```text
GET /api/sources
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
    "energysources": [
        {
            "dateFrom": 1514826000, 
            "dateTo": 1514864000, 
            "renewable": 0.69,
            "source" : {
                "wood": 0.12,
                "waste": 0, 
                "straw": 0,
                "oil": 0,
                "natural-gas": 0,
                "coal": 0.05,
                "biogas": 0,
                "solar": 0,
                "wind-onshore": 0,
                "wind-offshore": 0.56
            }
        }
    ]
}
```

## Internal call structure

[Link to EDS](https://www.energidataservice.dk/tso-electricity/declarationproduction)

```mermaid
sequenceDiagram
    participant spa as Single Page Application
    participant em as Emissions Domain
    participant ds as DataSync Domain
    participant eds as EnergiDataService

    spa->>+ em: GET /api/sources ?from:DateTime  &to: DateTime
    
    em->>+ ds: GET /meteringpoints
    ds--)- em: List<MeteringPointDTO>
    
    loop For each mp
      em ->>+ ds: GET /measurements  ?from:DateTime  &to: DateTime  &meteringpoint=
        
      ds --)- em: List< MeasurementDTO>
    end
    em->>+ eds: GET https://www.energidataservice.dk/tso-electricity/declarationproduction
    eds--)- em: {...}

    em->> em: calculate sources

    em->>- spa: energysources

```
