DO
$$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM   pg_constraint
        WHERE  conname = 'FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId')
    THEN
ALTER TABLE public."OutboxMessage"
    ADD CONSTRAINT "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId"
        FOREIGN KEY ("InboxMessageId", "InboxConsumerId")
            REFERENCES  public."InboxState" ("MessageId", "ConsumerId");
END IF;
END;
$$;

DO
$$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM   pg_constraint
        WHERE  conname = 'FK_OutboxMessage_OutboxState_OutboxId')
    THEN
ALTER TABLE public."OutboxMessage"
    ADD CONSTRAINT "FK_OutboxMessage_OutboxState_OutboxId"
        FOREIGN KEY ("OutboxId")
            REFERENCES  public."OutboxState" ("OutboxId");
END IF;
END;
$$;
