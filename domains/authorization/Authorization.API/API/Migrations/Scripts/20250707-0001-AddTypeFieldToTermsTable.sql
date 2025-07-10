DO
$$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name   = 'Terms'
          AND column_name  = 'Type')
    THEN
ALTER TABLE public."Terms"
    ADD COLUMN "Type" text NOT NULL DEFAULT 'Normal';
END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename  = 'Terms'
          AND indexname  = 'IX_Terms_Version')
    THEN
DROP INDEX public."IX_Terms_Version";
END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND tablename  = 'Terms'
          AND indexname  = 'IX_Terms_Version_Type')
    THEN
CREATE UNIQUE INDEX "IX_Terms_Version_Type"
    ON public."Terms" USING btree ("Version", "Type");
END IF;
END;
$$;
