using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using API.Models;

namespace API.Repository;

public class ClientRepository(ApplicationDbContext context) : GenericRepository<Client>(context);
public interface IClientRepository : IGenericRepository<Client>;
