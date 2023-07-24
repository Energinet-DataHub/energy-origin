-- Auth admin role. May NOT BE DELETED!
INSERT INTO public."Roles" ("Id", "Key", "Name", "IsDefault")
VALUES ('b8019791-3a98-4f01-a405-e9e316f52b91', 'AuthAdmin', 'AuthAdmin', false)
ON CONFLICT ("Id")
DO UPDATE SET "Key" = 'AuthAdmin', "Name" = 'AuthAdmin', "IsDefault" = false;

INSERT INTO public."Roles" ("Id", "Key", "Name", "IsDefault"
VALUES ('a25fbd87-5300-4fa0-93fa-b6ba0be0ef20', 'Viewer', 'Viewer', true);
ON CONFLICT ("Id")
DO UPDATE SET "Key" = 'Viewer', "Name" = 'Viewer', "IsDefault" = true;

INSERT INTO public."Roles" ("Id", "Key", "Name", "IsDefault")
VALUES ('6070485d-1081-4d2e-8d5e-3f35e4f69388', 'Editor', 'Editor', false);
ON CONFLICT ("Id")
DO UPDATE SET "Key" = 'Editor', "Name" = 'Editor', "IsDefault" = false;


