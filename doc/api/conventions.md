# Conventions

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

- QUARTERHOUR
- HOUR
- DAY
- WEEK
- MONTH
- YEAR
- ACTUAL
- TOTAL

```text
?aggregation=HOUR
```

### Time Zone

The API will need information about the time zone to generate output correctly.
The time zone is expected to be supplied as an IANA/Olson Time Zone Identifier.
The API will use the UTC/GMT time zone to generate output if a time zone not provided. Note that the identifier must be URL encoded.

```text
?timeZone=Europe%2FCopenhagen
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
