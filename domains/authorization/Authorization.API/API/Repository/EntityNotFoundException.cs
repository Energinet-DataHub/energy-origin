using System;
using API.Authorization.Exceptions;

namespace API.Repository;

public class EntityNotFoundException(Guid id, string s) : NotFoundException($"Entity of type {s} with id {id} not found.");
