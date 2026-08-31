using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTagSlugPostExcerptAndUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Tags",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excerpt",
                table: "Posts",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill slugs for tags that predate the column, so existing tag links keep
            // working. Collisions get a numeric suffix, matching what SlugHelper does.
            migrationBuilder.Sql("""
                WITH slugged AS (
                    SELECT "Id",
                           trim(both '-' from regexp_replace(lower("Name"), '[^a-z0-9]+', '-', 'g')) AS base
                    FROM "Tags"
                ),
                numbered AS (
                    SELECT "Id", base, row_number() OVER (PARTITION BY base ORDER BY "Id") AS rn
                    FROM slugged
                )
                UPDATE "Tags" t
                SET "Slug" = CASE WHEN n.rn = 1
                                  THEN left(n.base, 64)
                                  ELSE left(n.base, 58) || '-' || (n.rn - 1)
                             END
                FROM numbered n
                WHERE t."Id" = n."Id" AND n.base <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Slug",
                table: "Tags",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Slug",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Excerpt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Posts");
        }
    }
}
