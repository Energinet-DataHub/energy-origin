# Authorization: Acceptance of terms - Integration Event

## Purpose

The OrgAcceptedTerms record is for establishing integration between the Datahub system,
which stores the organization's metering point data, and the Energy Track & Trace™ domain.
When an organization accepts the terms and conditions, through the API served by the Authorization subsystem,
this record is sent to a RabbitMQ message broker and then consumed by the Datahub 2 (DH2) system.
This is to ensure that the organization's metering point data can be accessed, and processed,
by establishing a relation in Datahub 2 (DH2) system.

## Event Details

Upon successful acceptance of terms and conditions, the authorization subsystem publishes an OrgAcceptedTerms event,
with the following information:

```csharp
public record OrgAcceptedTerms : IntegrationEvent
{
    public Guid SubjectId { get; }
    public string? Tin { get; }
    public Guid Actor { get; }
}
```

***Note:** Read more about how the Authorization subsystem uses this integration event record
[here](../../../../../../doc/architecture/adr/0025-integration-events.md)

## Field Descriptions

1. **SubjectId:** The unique identifier of the Organization that accepted the terms on behalf of the organization.
2. **Tin:** The organization's danish CVR number. A unique identifier for the organization,
in [the Danish business registry](https://businessindenmark.virk.dk/).
Used for linking the organization across Datahub and the Energy Track & Trace™ domains.
3. **Actor:** The unique identifier of the user who accepted the terms on behalf of the organization.
Used for audit trails and accountability.

## Integration Impact

When this event is published:

1. The Datahub system can use the organization's CVR to associate metering point data with the organization,
in the Energy Track & Trace™ domain.
2. The Energy Track & Trace™ domain can initiate processes related to the organization,
knowing that they have accepted the necessary terms.
