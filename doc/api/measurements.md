
# Measurements domain

# Get Measurements for consumption data

The measurements api should take three query parameters and returns a date from, to and a value given in Wh.

## Request

```text
GET /api/measurements/consumption
        ?dateFrom=1514826000
        &dateTo=1514864000
        &aggregation=TOTAL
```
```text
GET /api/measurements/production
        ?dateFrom=1514826000
        &dateTo=1514864000
        &aggregation=TOTAL
```

## Parameters

- dateFrom: [UNIX timestamp](conventions.md#date-from-and-to)
- dateTo: [UNIX timestamp](conventions.md#date-from-and-to)
- aggregation: [aggregation ENUM](conventions.md#aggregation)

## Response

```jsonc
{
    "measurements": [
        {
            "dateFrom": 1514826000, // as unix time stamp 
            "dateTo": 1514864000, // as unix time stamp 
            "value": 123154 // in Wh
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

    spa->>+ em: GET /api/measurements/consumption  ?dateFrom:DateTime  &dateTo: DateTime &aggregation: Aggregation
    
    em->>+ ds: GET /meteringpoints
    ds--)- em: List<Meteringpoint>
    
    loop For each mp of type consumption
      em ->>+ ds: GET /measurements ?gsrn:long &dateFrom:DateTime &dateTo: DateTime
        
      ds --)- em: List< Measurement>
    end
    em->> em: aggregateMeasurements

    em->>- spa: MeasurementsDTO

```
