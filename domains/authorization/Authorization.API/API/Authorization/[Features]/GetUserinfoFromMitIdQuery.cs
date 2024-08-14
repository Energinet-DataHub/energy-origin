using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Authorization.Controllers;
using API.Services;
using MediatR;

namespace API.Authorization._Features_;

public class GetUserinfoFromMitIdQueryHandler(IMitIDService MitIdService) : IRequestHandler<GetUserinfoFromMitIdQuery, GetUserinfoFromMitIdQueryResult>
{
    public async Task<GetUserinfoFromMitIdQueryResult> Handle(GetUserinfoFromMitIdQuery request, CancellationToken cancellationToken)
    {
        var userinfo = await MitIdService.GetUserinfo(request.Bearertoken);

        return new GetUserinfoFromMitIdQueryResult(userinfo.Sub, userinfo.NemloginName, userinfo.NemloginEmail, userinfo.NemloginEmail, userinfo.NemloginOrgName);
    }
}

public record GetUserinfoFromMitIdQuery(string Bearertoken) : IRequest<GetUserinfoFromMitIdQueryResult>;

public record GetUserinfoFromMitIdQueryResult(string Sub, string Name, string Email, string OrgCvr, string OrgName);
