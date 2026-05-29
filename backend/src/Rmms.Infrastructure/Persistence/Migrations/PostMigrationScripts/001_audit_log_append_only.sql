-- =====================================================================
-- RMMS post-migration script — append-only enforcement for audit_log.
-- Run ONCE after `dotnet ef database update` applies the
-- Init_M01_M02_Foundation migration.
--
-- Why a separate SQL file (not in EF migration):
--   - EF Core has no native primitive for `REVOKE`/`GRANT` (works on DDL only).
--   - This grant depends on the DB user used by the app — value differs across
--     Dev / CI / Staging / Prod, so it lives outside the schema migration.
--
-- Spec ref:
--   - knowledge-base/decisions/ADR-004-soft-delete-interceptor.md
--   - knowledge-base/06-business-rules.md  CR-1 (Audit log mandatory)
--   - knowledge-base/PROJECT-STATE.md "Audit log is append-only at DB level"
--
-- Idempotent — safe to run multiple times. Adjust the role name if your
-- environment uses a different DB user.
-- =====================================================================

-- Replace `rmms` below with the actual application DB user if different
-- (in Dev / CI we run as the `rmms` superuser, so the REVOKE on superuser
--  is a no-op — but kept for explicit documentation).

DO $$
DECLARE
    app_role TEXT := current_setting('rmms.app_role', true);
BEGIN
    IF app_role IS NULL OR app_role = '' THEN
        app_role := 'rmms';
    END IF;

    EXECUTE format('REVOKE UPDATE, DELETE ON audit_log FROM %I', app_role);
    EXECUTE format('GRANT INSERT, SELECT ON audit_log TO %I', app_role);

    RAISE NOTICE 'audit_log permissions hardened for role: %', app_role;
END $$;
