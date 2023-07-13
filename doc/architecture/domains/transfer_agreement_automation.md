## Transfer Agreement Automation flow

First the deposit endpoint is exchanged between receiver and sender. The receiver creates an endpoint in his wallet. Then a Base64 (or what we choose) encoded string of the wallet deposit endpoint is handed to the sender, which then is created on the senders wallet. A reference guid to the receiver wallet is returned to the sender which is used by the transfer agreement automation background service.

```mermaid
sequenceDiagram
    actor receiver as Receiver
    actor sender as Sender
    participant ta as Transfer Agreement API
    box grey Same wallet system
        participant senderWallet as Sender Wallet
        participant wallet as receiver Wallet
    end

    receiver ->>+ ta: POST api/transfer-agreement/create-wallet-deposit-endpoint (OBS! should be moved to a wallet backend in the future)

    ta ->>+ wallet: gRPC CreateWalletDepositEndpoint()
    wallet -->>- ta: WalletDepositEndpoint

    ta -->>- receiver: base64 WalletDepositEndpoint

    receiver ->> sender: base64 WalletDepositEndpoint (over coffee)

    sender ->>+ ta: POST api/transfer-agreement (base64 WalletDepositEndpoint)

    ta ->>+ senderWallet: gRPC CreateReceiverDepositEndpoint(base64 WalletDepositEndpoint, reference: string)

    senderWallet -->>- ta: Reference: Guid

    ta ->> ta: Persist Reference guid and owner

    ta -->>- sender: TransferAgreement
```

When the wallet deposit endpoint has been exchanged the transfer agreement automation flow can begin. The TA background service gets all certificates owned and transfer them all one by one to the receiver.

```mermaid
sequenceDiagram
    participant robot as TA BackgroundService
    participant senderWallet as Sender Wallet

    robot ->> robot: GetTransferAgreements

    loop over transfer agreements
        robot ->> robot: Get owner and make JWT
        robot ->> senderWallet: gRPC QueryCertificates() with owner JWT
        senderWallet -->> robot: certificates

        loop until no more certificates
            robot ->> senderWallet: gRPC TransferCertificate(CertificateId, quantity, ReceiveGuid) with owner JWT
            senderWallet -->> robot: return
        end
    end
```
