-- ============================================================
-- RMMS — PostgreSQL initialization
-- Runs once on first container start (Postgres image convention).
-- ============================================================

-- pgcrypto: secure random / hash helpers
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- citext: case-insensitive text (used for email columns)
CREATE EXTENSION IF NOT EXISTS citext;

-- PostGIS: required by EF migration annotation and NetTopologySuite (BR-204)
CREATE EXTENSION IF NOT EXISTS postgis;

-- Trigram search (for fuzzy store name search later)
CREATE EXTENSION IF NOT EXISTS pg_trgm;
