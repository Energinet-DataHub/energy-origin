using System.Collections.Generic;
using System.Threading.Tasks;
using API.Claiming.Api.Models;

namespace API.Claiming.Api.Repositories;

public interface IClaimRepository
{
    Task<List<ClaimSubject>> GetClaimSubjects();
    Task<ClaimSubject?> GetClaimSubject(string subject);

    Task<ClaimSubject> AddClaimSubject(ClaimSubject claimSubject);
    void DeleteClaimSubject(ClaimSubject claim);
    Task<List<ClaimSubjectHistory>> GetHistory(string subject);
}
