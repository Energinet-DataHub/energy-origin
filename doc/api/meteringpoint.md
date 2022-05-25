# Metering point domain


## Get meteringpoints


### Request

```text
GET /api/meteringpoints
```

### Parameters

- dateFrom: [UNIX timestamp](conventions.md#date-from-and-to)
- dateTo: [UNIX timestamp](conventions.md#date-from-and-to)
- aggregation: [aggregation ENUM](conventions.md#aggregation)

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
