using System;

namespace API.Authorization.Exceptions;

public class WalletNotEnabled(string message) : Exception(message);
