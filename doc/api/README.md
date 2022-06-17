# APIs

This folder will contain documentation for working with the APIs and developing the APIs.

- [Conventions](conventions.md)

## PUBLIC

### Auth Domain

- GET [/api/auth/oidc/login](auth.md#oidc-login)
- GET [/api/auth/oidc/login/callback](auth.md#oidc-login-callback)
- POST [/api/auth/invalidate](auth.md#oidc-invalidate)
- POST [/api/auth/logout](auth.md#logout)
- GET [/api/auth/profile](auth.md#profile)
- GET [/api/auth/context](auth.md#context)
- GET [/api/auth/terms](auth.md#terms)
- POST [/api/auth/terms/accept](auth.md#accept-terms)

### Meteringpoints Domain

- GET [/api/meteringpoints](meteringpoint.md#get-meteringpoints)

### Emissions Domains

- GET [/api/emissions](emissions.md#get-emissions)
- GET [/api/sources](emissions.md#get-sources-of-energy)

### Measurements Domains

- GET [/api/measurements/consumptions](measurements.md##get-measurements-for-consumption-data)

## INTERNAL

### Auth-internal

- GET [/token/forward-auth](auth.md#forward-auth)

### DataSync-internal

- POST [http://datasync/relations](datasync.md#create-relations)
- GET [http://datasync/meteringpoints](datasync.md#get-metering-points)
- GET [http://datasync/measurements](datasync.md#get-measurements)
