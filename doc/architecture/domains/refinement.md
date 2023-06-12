Epic: https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1463

# Questions - with Martin:

## JWT bearer token - check with Martin that we should just pass EO JWT to PO Wallet

- "Receive Slice" endpoint has no authentication
- For other endpoints we should use the incoming JWT in energy-origin

## Wallet(section) creation - when does that occur?

[Task](https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1545): Update Terms & Conditions to include that a wallet will be created for the user in Energinet's "default" wallet system

Think of it as: Wallet section is an "obfuscated" IBAN number - only relevant for receiving slices. Not relevant for queries, claims.

One wallet section per Owner:

- Con: Cannot deterministically calculate the "løbe nummer"
- Con: Many metering points "drains" the possible number of certificates

One wallet section per GSRN:

- Con: How should we handle change of metering point owner?

One wallet section per Certificate Issuing Contract:

- Pro: Contract should (somehow) automatically stop/close in case of new owner, which will also stop certificates to be issued

## "Løbenummer" for HD keys. How do we do that?

- uint (maybe int) is used for the sequence number. We cannot get a long or ulong
- Converting UTC timestamp to a "løbe nummer" based 15-minutes interval will hit the upper limit (int max) in 61,000 years.
- A downside of this approach is that it should be maintained in case the energy markets time resolution changes
- There is task to investigate what we currently know about the market resolution

## How do we handle "bring your own wallet"-scenario? Do we ignore it for now? Is there something like a "default" wallet for Energinet, which is automatically used

- UI should inform the user that a wallet (and wallet section) will be automatically created when creating a contract. Make this a task

- WalletSectionReference can be given as input to creating the contract and this is where issued certificates should be sent to.

## How do we get the proto-files from PO Wallet?

https://github.com/project-origin/wallet/issues/32

## Should we have an event from Wallet indicating if the received slice was verified successfully or not?

Since the certificate is issued in registry first, it will always be verified succesfully unless bogus data is sent (e.g. evil parties).

## Number of wallets

For Genesis release: Only allowed one per user for now, later multiple wallets per user will be allowed.

## Calculation of R-value - and where should it be stored

TODO... https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1517

Saving the R value in cert. domain makes it possible to verify what is published to the blockchain. Without that we cannot verify anything.

SecretCommitmentInfo - will be available in a Pedersen Commitment library (no Github issue avaible in PO registry) that is currently not published. Commitment can be created from SecretCommitmentInfo.

Registry only needs Commitment C
Wallet needs SecretCommitmentInfo (m, R)

# Tasks - internal:

* DataSyncSyncer Sync State issue. It depends on the SyncState-projection that is based on events stored in MartenDB. (link to zenhub issue)
* MartenDB Migration scripts - is auto-migration ([see zenhub issue](https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/986))
* Get rid of the name "Query API"
* Kill/close down existing transfer cert API endpoints
* Wallet Query does not return time period, GSRN, GridArea, tech+fuel code for the certificates/slices.
* Replace Registry client with a new one generated from Protobuf-filer for registry (will be available when https://github.com/project-origin/registry/pull/116 is done and released)
* Migration of existing Contracts to have a WalletSectionReference

# Tasks - for Project Origin:

To be created as issues in EO Zenhub:

- Setup Wallet in Energy Origin
  - In the wallet, there is currently no validation of the JWT token (true until https://github.com/project-origin/wallet/issues/19 is completed)
- Configure Wallet to use Registry in Energy Origin

# Something that is outside the wallet integration task:

* Ensure only one cert per measurement ([see zenhub issue](https://app.zenhub.com/workspaces/team-atlas-633199659e255a37cd1d144f/issues/gh/energinet-datahub/energy-origin-issues/1114))
* We have multiple services depending on the same Marten-database making it a single point of failure. Is that something we want in the future? Is there too much functionality in the cert domain? (create zenhub issue for this)
