Questions:

Questions - with Martin:
* JWT bearer token - check with Martin that we should just pass EO JWT to PO Wallet
* Wallet(section) creation - when does that occur?
* "Løbenummer" for HD keys. How do we do that?
* How do we get the proto-files from PO Wallet?
* How do we handle "bring your own wallet"-scenario? Do we ignore it for now? Is there something like a "default" wallet for Energinet, which is automatically used
* Should we have an event from Wallet indicating if the received slice was verified successfully or not?

Tasks - internal:

* DataSyncSyncer Sync State issue. It depends on the SyncState-projection that is based on events stored in MartenDB. (link to zenhub issue)
* MartenDB Migration scripts - is auto-migration (link to zenhub issue)
*

Tasks - for Project Origin:

* Setup Wallet in Energy Origin
* Configure Wallet to use Registry in Energy Origin

Something that is outside the wallet integration task:

* Ensure only one cert per measurement (link to zenhub issue)
* We have multiple services depending on the same Marten-database making it a single point of failure. Is that something we want in the future? Is there too much functionality in the cert domain? (create zenhub issue for this)
