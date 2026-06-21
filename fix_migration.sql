-- Run this script to fix the migration issue
-- This inserts the initial migration record into the migration history table

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260607020605_InitialCreate', '6.0.0');
