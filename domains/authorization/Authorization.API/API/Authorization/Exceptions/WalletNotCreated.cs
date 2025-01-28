using System;

namespace API.Authorization.Exceptions;

public class WalletNotCreated(string message) : Exception(message);
