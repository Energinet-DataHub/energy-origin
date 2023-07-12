## Transfer Agreement Automation flow

wallet://mywallet.sdaugaard.dk/?exchangeToken=123515621b35252453

First the deposit endpoint is exchanged between receiver and sender. The receiver creates an endpoint in his wallet. Then a Base64 (or what we choose) encoded string of the wallet deposit endpoint is handed to the sender, which then is created on the senders wallet. A reference guid to the receiver wallet is returned to the sender which is used by the transfer automation robot.

```mermaid
sequenceDiagram
    actor receiver as Receiver
    actor sender as Sender
    participant reg as Registry
    participant senderWallet as Sender Wallet
    participant wallet as receiver Wallet

    receiver ->>+ wallet: CreateWalletDepositEndpoint()
    wallet -->>- receiver: base64 WalletDepositEndpoint

    receiver ->> sender: base64 WalletDepositEndpoint (over coffee)

    sender ->>+ senderWallet: CreateReceiverDepositEndpoint(base64 WalletDepositEndpoint, reference: string)

    senderWallet -->>- sender: Reference: Guid

```

When the wallet deposit endpoint has been exchanged the transfer agreement automation flow can begin. The TA robot gets all certificates owned and transfer them all one by one to the receiver.

```mermaid
sequenceDiagram
    actor robot as TA Robot
    participant reg as Registry
    participant senderWallet as Sender Wallet

    robot ->> robot: GetTransferAgreements

    loop pr transfer agreement
        robot ->> robot: Get owner and make JWT
        robot ->> senderWallet: QueryCertificates with owner JWT
        senderWallet -->> robot: certificates

        loop until no more certificates
            robot ->> senderWallet: TransferCertificate(CertificateId, quantity, ReceiveGuid) with owner JWT
            senderWallet -->> robot: return
        end
    end
```
