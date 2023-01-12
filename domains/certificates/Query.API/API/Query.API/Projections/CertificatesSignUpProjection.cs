using System;
using System.Collections.Generic;
using System.Linq;
using API.Query.API.ApiModels;
using CertificateSignupEvents;
using Marten.Events.Projections;
using Marten.Schema;

namespace API.Query.API.Projections;

public class CertificatesSignUpProjection : MultiStreamAggregation<>
{
    public CertificatesSignUpProjection()
    {
        Identity<CertificateSignup>(e => e.MeteringPointOwner);
        Identity<CertificateSignup>(e => e.ShieldedGSRN);
        Identity<CertificateSignup>(e => e.signedUp);
    }

}
