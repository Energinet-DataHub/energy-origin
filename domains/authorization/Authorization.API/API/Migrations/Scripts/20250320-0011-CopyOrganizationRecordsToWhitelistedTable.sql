DO
$$
    BEGIN
        INSERT INTO public."Whitelisted" ("Id", "Tin")
        SELECT "Id", "Tin"
        FROM public."Organizations"
        WHERE "Tin" IS NOT NULL
        ON CONFLICT ON CONSTRAINT "PK_Whitelisted" DO NOTHING;
    END;
$$;
