# Azure AD B2C: Extracting and Validating `login_type` from OAuth2 Query Parameter

This configuration enables Azure AD B2C to:

- Accept a custom OAuth2 query parameter called `ettLoginType`
- Validate its structure using regex
- Extract into a `login_type` claim with a value of either `trial` or `normal`
- Store that extracted value for further logic or token output

---

## 🔁 OAuth2 Query Parameter Format

The frontend sends a query parameter in the authorization request:

```
...?ettLoginType=ett:login:type:normal
```

This is a custom parameter captured using:

```
{OAUTH-KV:ettLoginType}
```

---

## 🏷️ ClaimType Definitions

These claimTypes handle storage and processing of the query parameter key:

```xml
<ClaimType Id="ettLoginType">
  <DisplayName>{OAUTH-KV:ettLoginType}</DisplayName>
  <DataType>string</DataType>
  <UserHelpText>Identifier for the login type</UserHelpText>
  <UserInputType>Readonly</UserInputType>
</ClaimType>

<ClaimType Id="login_type">
  <DisplayName>Login Type</DisplayName>
  <DataType>string</DataType>
</ClaimType>

<ClaimType Id="login_type_regex_validation">
  <DisplayName>Login Type Regex Validation</DisplayName>
  <DataType>string</DataType>
</ClaimType>

<ClaimType Id="login_type_regex_compare_result">
  <DisplayName>Login Type Regex Compare Result</DisplayName>
  <DataType>boolean</DataType>
</ClaimType>
```

---

## 🔧 ClaimsTransformation: Extract and Validate

This transformation both **validates and extracts** into the `login_type` ClaimType from the `ettLoginType` query parameter.

```xml
<ClaimsTransformation Id="ExtractLoginTypeFromLoginTypeQueryParam" TransformationMethod="SetClaimsIfRegexMatch">
  <InputClaims>
    <InputClaim ClaimTypeReferenceId="ettLoginType" TransformationClaimType="claimToMatch" />
  </InputClaims>
  <InputParameters>
    <InputParameter Id="matchTo" DataType="string"
      Value="^(?!(?:.*(?<!\S)ett:login:type:(?:trial|normal)(?!\S).*(?<!\S)ett:login:type:(?:trial|normal)(?!\S)|.*(?<!\S)ett:login:type:(?!(?:trial|normal)(?!\S))\S*(?!\S))).*?(?<!\S)ett:login:type:(?<login_type>trial|normal)(?!\S).*$" />
    <InputParameter Id="outputClaimIfMatched" DataType="string" Value="valid" />
    <InputParameter Id="extractGroups" DataType="boolean" Value="true" />
  </InputParameters>
  <OutputClaims>
    <OutputClaim ClaimTypeReferenceId="login_type_regex_validation" TransformationClaimType="outputClaim" />
    <OutputClaim ClaimTypeReferenceId="login_type_regex_compare_result" TransformationClaimType="regexCompareResultClaim" />
    <OutputClaim ClaimTypeReferenceId="login_type" />
  </OutputClaims>
</ClaimsTransformation>
```

---

## ⚙️ TechnicalProfiles

### `InitEttLoginType`

Loads the raw query parameter into `ettLoginType`.

```xml
<TechnicalProfile Id="InitEttLoginType">
  <DisplayName>Init ettLoginType from OAUTH-KV param</DisplayName>
  <Protocol Name="Proprietary" Handler="Web.TPEngine.Providers.ClaimsTransformationProtocolProvider, Web.TPEngine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
  <OutputClaims>
    <OutputClaim ClaimTypeReferenceId="ettLoginType" DefaultValue="{OAUTH-KV:ettLoginType}" AlwaysUseDefaultValue="true" />
  </OutputClaims>
</TechnicalProfile>
```

### `ExtractLoginType`

Applies the regex transformation:

```xml
<TechnicalProfile Id="ExtractLoginType">
  <DisplayName>Extract login type from ettLoginType</DisplayName>
  <Protocol Name="Proprietary" Handler="Web.TPEngine.Providers.ClaimsTransformationProtocolProvider, Web.TPEngine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null" />
  <Metadata>
    <Item Key="DebugMode">true</Item>
    <Item Key="IncludeClaimResolvingInClaimsHandling">true</Item>
  </Metadata>
  <InputClaims>
    <InputClaim ClaimTypeReferenceId="ettLoginType" DefaultValue="{OAUTH-KV:ettLoginType}" AlwaysUseDefaultValue="true" />
  </InputClaims>
  <OutputClaims>
    <OutputClaim ClaimTypeReferenceId="login_type" />
    <OutputClaim ClaimTypeReferenceId="login_type_regex_validation" />
    <OutputClaim ClaimTypeReferenceId="login_type_regex_compare_result" />
  </OutputClaims>
  <OutputClaimsTransformations>
    <OutputClaimsTransformation ReferenceId="ExtractLoginTypeFromLoginTypeQueryParam" />
  </OutputClaimsTransformations>
</TechnicalProfile>
```

---

## 🚶 User Journey Integration

Make sure both technical profiles are called in order in your journey:

```xml
<OrchestrationStep Order="1" Type="ClaimsExchange">
  <ClaimsExchange Id="InitEttLoginTypeExchange" TechnicalProfileReferenceId="InitEttLoginType" />
</OrchestrationStep>

<OrchestrationStep Order="2" Type="ClaimsExchange">
  <ClaimsExchange Id="ExtractLoginTypeExchange" TechnicalProfileReferenceId="ExtractLoginType" />
</OrchestrationStep>
```

---

## ✅ Expected Output in Statebag

After execution, one should see:

```json
{
  "ettLoginType": "ett:login:type:normal",
  "login_type_regex_validation": "valid",
  "login_type_regex_compare_result": true,
  "login_type": "normal"
}
```

Possible values for ettLoginType are only `ett:login:type:normal` or `ett:login:type:trial`

---

## 🔄 Example Extension

To add a new login type like `premium`, change this part of the regex:

```regex
(trial|normal)
```

To:

```regex
(trial|normal|premium)
```

---
