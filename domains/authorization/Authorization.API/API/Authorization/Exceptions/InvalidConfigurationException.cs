using System;

namespace API.Authorization.Exceptions;

public class InvalidConfigurationException(string message) : Exception(message);
