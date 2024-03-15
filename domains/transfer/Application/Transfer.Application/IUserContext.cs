namespace Transfer.Application;

public interface IUserContext
{
    Guid Subject { get; }
    string Name { get; }
    Guid OrganizationId { get; }
    string OrganizationTin { get; }
    string OrganizationName { get; }
}
