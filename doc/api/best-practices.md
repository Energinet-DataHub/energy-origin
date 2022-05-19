# Best Practices

## Query Parameter Naming

### Date from and to

On APIs best practice is to use the parameter names ***dateFrom*** and ***dateTo***
each parameter shall take its parameter as a **UNIX timestamp** in **UTC**.

```text
?dateFrom=1640991600
&dateTo=1643670000
```

### Aggregation

Is a enum to define the aggregation level for the response.

Possible values:

- 15MIN
- HOUR
- DAY
- WEEK
- MONTH
- QUARTER
- YEAR
- ACTUAL
- TOTAL

```text
?aggregation=HOUR
```

## Return object

JSON reponses **must** be a json object and not an array.
If the response contains a list, this should be wrapped in an object.

```json
{
    "result": [
        {...},
        {...}
    ]
}
```
