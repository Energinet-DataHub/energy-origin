using System;
using System.Runtime.Serialization;

namespace API.Query.API.Projections.Exceptions;

[Serializable]
public class ProjectionException : Exception
{

    public ProjectionException(Guid certificateId, string message) : base(message)
    {
    }

    protected ProjectionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
