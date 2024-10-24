using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationConsentInsertOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlAddGuid = """
                             DO $$
                             BEGIN
                                IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'uuid-ossp') THEN
                                   CREATE EXTENSION "uuid-ossp";
                                END IF;
                             END $$;
                             """;
            migrationBuilder.Sql(sqlAddGuid);

            var sql = """
                      WITH inserted_orgs AS (
                          INSERT INTO public."Organizations" ("Id", "Tin", "Name", "TermsAcceptanceDate", "TermsAccepted", "TermsVersion")
                          SELECT
                              (SELECT uuid_generate_v4()) AS "Id",
                              null AS "Tin",
                              "Name" AS "Name",
                              null AS "TermsAcceptanceDate",
                              false AS "TermsAccepted",
                              null AS "TermsVersion"
                          FROM public."Clients"
                          RETURNING "Id", "Name"
                      )

                      UPDATE
                      	public."Clients" c
                      SET
                      	"OrganizationId" = i."Id"
                      FROM
                      	inserted_orgs i
                      WHERE
                      	c."Name" = i."Name";
                      """;
            migrationBuilder.Sql(sql);

            var moreSql = """
                          INSERT INTO public."OrganizationConsents" ("Id", "ConsentGiverOrganizationId", "ConsentReceiverOrganizationId", "ConsentDate")
                          SELECT
                          	uuid_generate_v4() AS "Id",
                          	con."OrganizationId" AS "ConsentGiverOrganizationId",
                          	cli."OrganizationId" AS "ConsentReceiverOrganizationId",
                          	con."ConsentDate" AS "ConsentDate"
                          FROM
                          	public."Consents" as con
                          INNER JOIN
                          	public."Clients" AS cli
                          ON
                          	con."ClientId" = cli."Id";
                          """;

            migrationBuilder.Sql(moreSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
