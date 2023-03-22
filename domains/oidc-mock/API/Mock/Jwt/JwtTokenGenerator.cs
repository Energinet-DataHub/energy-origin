using Jose;

namespace Oidc.Mock.Jwt;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly Jwk jwk;

    // Key copied from https://github.com/dvsekhvalnov/jose-jwt#rs--and-ps--family
    public JwtTokenGenerator() => jwk = new Jwk(
        e: "AQAB",
        n: "qFZv0pea_jn5Mo4qEUmStuhlulso8n1inXbEotd_zTrQp9K0RK0hf7t0K4BjKVhaiqIam4tVVQvkmYeBeYr1MmnO_0N97dMBz_7fmvyv0hgHaBdQ5mR5u3LTlHo8tjRE7-GzZmGs6jMcyj7HbXobDPQJZpqNy6JjliDVXxW8nWJDetxGBlqmTj1E1fr2RCsZLreDOPSDIedG1upz9RraShsIDzeefOcKibcAaKeeVI3rkAU8_mOauLSXv37hlk0h6sStJb3qZQXyOUkVkjXIkhvNu_ve0v7LiLT4G_OxYGzpOQcCnimKdojzNP6GtVDaMPh-QkSJE32UCos9R3wI2Q",
        p: "0qaOkT174vRG3E_67gU3lgOgoT6L3pVHuu7wfrIEoxycPa5_mZVG54SgvQUofGUYEGjR0lavUAjClw9tOzcODHX8RAxkuDntAFntBxgRM-IzAy8QzeRl_cbhgVjBTAhBcxg-3VySv5GdxFyrQaIo8Oy_PPI1L4EFKZHmicBd3ts",
        q: "zJPqCDKqaJH9TAGfzt6b4aNt9fpirEcdpAF1bCedFfQmUZM0LG3rMtOAIhjEXgADt5GB8ZNK3BQl8BJyMmKs57oKmbVcODERCtPqjECXXsxH-az9nzxatPvcb7imFW8OlWslwr4IIRKdEjzEYs4syQJz7k2ktqOpYI5_UfYnw1s",
        d: "lJhwb0pKlB2ivyDFO6thajotClrMA3nxIiSkIUbvVr-TToFtha36gyF6w6e6YNXQXs4HhMRy1_b-nRQDk8G4_f5urd_q-pOn5u4KfmqN3Xw-lYD3ddi9qF0NLeTVUNVFASeP0FFqbPYfdNwD-LyvwjhtT_ggMOAw3mYvU5cBfz6-3uPdhl3CwQFCTgwOud_BA9p2MPMUHG82wMK_sNO1I0TYpjm7TnwNBwiKbMf-i5CKnuohgoYrEDYLeMg3f32eBljlCFNYaoCtT-mr1Ze0OTJND04vbfLotV-BBKulIpbOOSeVpKG7gJxZHmv7in7PE5_WzaxKFVoHW3wR6v_GzQ",
        dp: "KTWmTGmf092AA1euOmRQ5IsfIIxQ5qGDn-FgsRh4acSOGE8L7WrTrTU4EOJyciuA0qz-50xIDbs4_j5pWx1BJVTrnhBin9vNLrVo9mtR6jmFS0ko226kOUpwEVLgtdQjobWLjtiuaMW-_Iw4gKWNptxZ6T1lBD8UWHaPiEFW2-M",
        dq: "Jn0lqMkvemENEMG1eUw0c601wPOMoPD4SKTlnKWPTlQS6YISbNF5UKSuFLwoJa9HA8BifDrD-Mfpo1M1HPmnoilEWUrfwMqqdCkOlbiJQhKY8AZ16QGH50kDXhmVVa8BRWdVQWBTUzWXS5kXMaeskVzextTgymPcOAhXN-ph7MU",
        qi: "sRAPigJpl8S_vsf1zhJTrHM97xRwuB26R6Tm-J8sKRPb7p5xxNlmOBBFvWmWxdto8dBElNlydSZan373yBLxzW-bZgVp-B2RKT1B3WhTYW_Vo5DLhWi84XMncJxH7avtxtF9yksaeKe0e2n3J6TTan53mDg4KF8U0OEO2ciqO9g"
    );

    public string Generate(Dictionary<string, object> claims) => JWT.Encode(claims, jwk, JwsAlgorithm.RS256);

    public IDictionary<string, object> GetJwkProperties() => jwk.ToDictionary();
}
