using System;

namespace API.Repository;

public class EntityNotFoundException(Guid id, string s) : Exception($"Entity of type {s} with id {id} not found.");
