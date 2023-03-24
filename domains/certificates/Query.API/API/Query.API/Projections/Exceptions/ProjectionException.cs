using System;

namespace API.Query.API.Projections.Exceptions;

public class ProjectionException : Exception
{

    public ProjectionException(Guid certificateId, string message) : base(message)
    {
    }
}
