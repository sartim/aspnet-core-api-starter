using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreApiStarter.Migrations
{
    /// <inheritdoc />
    public partial class DotNet10Upgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Users' AND column_name = 'FailedLoginAttempts') THEN
                        ALTER TABLE "Users" ADD COLUMN "FailedLoginAttempts" integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Users' AND column_name = 'LastLoginAt') THEN
                        ALTER TABLE "Users" ADD COLUMN "LastLoginAt" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Users' AND column_name = 'LockoutEnd') THEN
                        ALTER TABLE "Users" ADD COLUMN "LockoutEnd" timestamp with time zone;
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Users' AND column_name = 'EmailVerifiedAt') THEN
                        ALTER TABLE "Users" ADD COLUMN "EmailVerifiedAt" timestamp with time zone;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'RefreshTokens') THEN
                        CREATE TABLE "RefreshTokens" (
                            "Id" uuid NOT NULL,
                            "UserId" uuid NOT NULL,
                            "TokenHash" text NOT NULL,
                            "CreatedAt" timestamp with time zone NOT NULL,
                            "ExpiresAt" timestamp with time zone NOT NULL,
                            "RevokedAt" timestamp with time zone,
                            "ReplacedByTokenHash" text,
                            CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
                            CONSTRAINT "FK_RefreshTokens_Users_UserId"
                                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                        );
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'RevokedAccessTokens') THEN
                        CREATE TABLE "RevokedAccessTokens" (
                            "Id" uuid NOT NULL,
                            "JwtId" text NOT NULL,
                            "ExpiresAt" timestamp with time zone NOT NULL,
                            "RevokedAt" timestamp with time zone NOT NULL,
                            CONSTRAINT "PK_RevokedAccessTokens" PRIMARY KEY ("Id")
                        );
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'EmailActionTokens') THEN
                        CREATE TABLE "EmailActionTokens" (
                            "Id" uuid NOT NULL,
                            "IsDeleted" boolean NOT NULL,
                            "CreatedAt" timestamp with time zone NOT NULL,
                            "UpdatedAt" timestamp with time zone NOT NULL,
                            "DeletedAt" timestamp with time zone,
                            "UserId" uuid NOT NULL,
                            "Purpose" text NOT NULL,
                            "TokenHash" text NOT NULL,
                            "ExpiresAt" timestamp with time zone NOT NULL,
                            "UsedAt" timestamp with time zone,
                            CONSTRAINT "PK_EmailActionTokens" PRIMARY KEY ("Id"),
                            CONSTRAINT "FK_EmailActionTokens_Users_UserId"
                                FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                        );
                    END IF;
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = 'OutboxMessages') THEN
                        CREATE TABLE "OutboxMessages" (
                            "Id" uuid NOT NULL,
                            "IsDeleted" boolean NOT NULL,
                            "CreatedAt" timestamp with time zone NOT NULL,
                            "UpdatedAt" timestamp with time zone NOT NULL,
                            "DeletedAt" timestamp with time zone,
                            "EventType" text NOT NULL,
                            "Payload" text NOT NULL,
                            "OccurredAt" timestamp with time zone NOT NULL,
                            "ProcessedAt" timestamp with time zone,
                            "Attempts" integer NOT NULL,
                            "LastError" text,
                            CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
                        );
                    END IF;

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshTokens_TokenHash"
                        ON "RefreshTokens" ("TokenHash");
                    CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId"
                        ON "RefreshTokens" ("UserId");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_RevokedAccessTokens_JwtId"
                        ON "RevokedAccessTokens" ("JwtId");
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmailActionTokens_TokenHash"
                        ON "EmailActionTokens" ("TokenHash");
                    CREATE INDEX IF NOT EXISTS "IX_EmailActionTokens_UserId_Purpose_UsedAt"
                        ON "EmailActionTokens" ("UserId", "Purpose", "UsedAt");
                    CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_ProcessedAt_OccurredAt"
                        ON "OutboxMessages" ("ProcessedAt", "OccurredAt");
                    DROP INDEX IF EXISTS "IX_EmailActionTokens_UserId";
                END $$;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_EmailActionTokens_UserId\" ON \"EmailActionTokens\" (\"UserId\");");
        }
    }
}
