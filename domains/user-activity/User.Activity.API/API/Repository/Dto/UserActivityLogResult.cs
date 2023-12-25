using System.Collections.Generic;
using API.Models;

namespace API.Repository.Dto;

public record UserActivityLogResult(int TotalCount, List<UserActivityLog> Items);
