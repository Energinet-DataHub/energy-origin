# eo-auth

This repository contains the codebase for the Auth domain which is a part of [Energy Origin](https://github.com/Energinet-DataHub/energy-origin).

## Notes

### Private/Public Key Generation

At present, there is no support for elliptic curve based keys, so we are going with an RSA key.

Generating new key pairs are done by executing the following:

```
openssl genrsa -out private.pem 2048
openssl rsa -in private.pem -pubout -out public.pem
```

To have a key in appsettings, we will need to convert it it to Base64 format, which can be done for each key file by executing the following:

```
base64 <filename.pem | tr -d \\n | tr \\n \\\\n
```

__Note__: You will need to change `filename.pem` to the filename of the key you want to convert.

__Note__: You will need to base64 encode the key again when using it in a kubernetes secret.

### Migration generation

Here is an example of how to generate migrations SQL for the API project:

```shell
dotnet ef migrations script --idempotent --project Auth.API/API/API.csproj --output migrations/API.sql
```

#Test Auth
