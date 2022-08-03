namespace API.Models;
public class InternalToken
{
    public DateTime Issued { get; }
    public DateTime Expires { get; }
    public string Actor { get; }
    public string Subject { get; }
    public List<string> Scope { get; }

    public InternalToken(DateTime issued, DateTime expires, string actor, string subject, List<string> scope)
    {
        Issued = issued;
        Expires = expires;
        Actor = actor;
        Subject = subject;
        Scope = scope;
    }
}
