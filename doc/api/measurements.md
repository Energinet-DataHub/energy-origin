
# Measurements domain

# Get Measurements for consumption data

The measurements api should take three query parameters.


## Request

```text
GET /api/consumption
        ?dateFrom=1514826000
        &dateTo=1514864000
        &aggregation=TOTAL   
```

## Parameters

- dateFrom: [UNIX timestamp](conventions.md#date-from-and-to)
- dateTo: [UNIX timestamp](conventions.md#date-from-and-to)
- aggregation: [aggregation ENUM](conventions.md#aggregation)

## Response

```json
{
    "measurements": [
        {
            "dateFrom": 1514826000, 
            "dateTo": 1514864000,  
            "value": 123154,
            "unit": "kWh",
        }
    ]
}
```

## Internal call structure

```mermaid
sequenceDiagram
    participant spa as Single Page Application
    participant em as Measurements Domain
    participant ds as DataSync Domain

    spa->>+ em: GET /api/consumption  ?dateFrom:DateTime  &dateTo: DateTime &aggregation: Aggregation
    
    em->>+ ds: GET /measurements
    ds--)- em: List<MeasurementsDTO>
    
    loop For each mp
      em ->>+ ds: GET /measurements ?gsrn:long &dateFrom:DateTime &dateTo: DateTime
        
      ds --)- em: List< MeasurementDTO>
    end
    em->> em: calculateMeasurements

    em->>- spa: MeasurementsDTO

```
