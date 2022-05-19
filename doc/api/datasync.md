# Data Sync Domain

The emissions api should take three query parameters

# Create relations

## Request

```text
POST /relations

{
    "ssn": null,
    "tin": 12345678
}
```

## Parameters

- ssn: Social security number
- tin: Company tin number

***ssn and tin are mutual exclusive, one must be set.***

## Response

```json
{
    "success": true
}
```

## Create relations for company

```mermaid
sequenceDiagram
    autonumber
    participant spa as Single Page Application
    participant auth as Auth Domain
    participant datasync as DataSync Domain

    spa->>+auth: login
        auth ->>+ datasync: POST: /relation  {tin=12345678}
        datasync -->>- auth: Success
    auth-->>-spa: jwt / opaque token (cookie)

```

# Get Metering points


## Request

```text
GET /meteringpoints
```

## Parameters

\-

## Response

```json
{
    "meteringpoints": [
        {
            "meteringPointId": 57131300000000001
        }
    ]
}
```



# Get Measurements

## Request

```text
GET /measurements
    ?meteringpoint=123;456;789
    &dateFrom=1514826000
    &dateTo=1514864000
```

## Parameters

- meteringpoint: 17 digit integer id of a meteringpoint, multiple can be seperated with ;
- dateFrom: [UNIX timestamp](best-practices.md#date-from-and-to)
- dateTo: [UNIX timestamp](best-practices.md#date-from-and-to)


## Response

```json
{
    "measurements": [
        {
            "meteringPointId": 57131300000000001,
            "dateFrom": 1514826000,
            "dateTo": 1514864000,
            "quantity": 1865880,
            "quality": 1 // 1=measured 2=estimated
        },...
    ]
}
```
