namespace API.Authorization.Exceptions;

public class EntityNotFoundException(string key, string s)
    : NotFoundException($"Entity of type {s} with id(s) {key} not found.");
