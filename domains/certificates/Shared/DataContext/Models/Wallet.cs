using System;

namespace DataContext.Models;

public class Wallet
{
    public Guid OwnerSubject { get; private set; }
    public Guid WalletId { get; private set; }
    protected Wallet() { }
    public Wallet(Guid ownerSubject, Guid walletId)
    {
        OwnerSubject = ownerSubject;
        WalletId = walletId;
    }
}
