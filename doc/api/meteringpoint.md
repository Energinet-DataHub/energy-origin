# Metering point domain


## Get meteringpoints




### Request

```
GET /api/meteringpoints
```

### Parameters

- dateFrom: [UNIX timestamp](best-practices.md#date-from-and-to)
- dateTo: [UNIX timestamp](best-practices.md#date-from-and-to)
- aggregation: [aggregation ENUM](best-practices.md#aggregation)

### Response

```json
{
    "meteringpoints": [
        {
            "gsrn": 57131300000000001, 
        }
    ]
}
```
