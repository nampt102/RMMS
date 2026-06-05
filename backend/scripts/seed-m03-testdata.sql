-- ============================================================================
-- M03 Organization & Assignment — test data seed (idempotent).
--
-- Safe to run multiple times: every INSERT is guarded so re-runs are no-ops.
-- Masters (areas / stores / categories) have NO user dependency.
-- Assignments pick whatever PG / Leader already exist in the DB (by role); if
-- none exist yet, the assignment block is skipped silently — create users via
-- the app/admin first, then re-run this script (or assign via the web panel).
--
-- Usage (Mac, against your own DB):
--   docker exec -i <postgres-container> psql -U rmms -d rmms < backend/scripts/seed-m03-testdata.sql
--   -- or:  psql "<connection-string>" -f backend/scripts/seed-m03-testdata.sql
-- ============================================================================

SET client_encoding TO 'UTF8';

-- ---------- AREAS (top-level + 2 sub-areas under HCM) ----------
INSERT INTO areas (id, code, name, parent_area_id, created_at)
SELECT gen_random_uuid(), v.code, v.name, NULL, now()
FROM (VALUES ('HCM','Hồ Chí Minh'), ('HN','Hà Nội'), ('DN','Đà Nẵng')) AS v(code, name)
WHERE NOT EXISTS (SELECT 1 FROM areas a WHERE a.code = v.code);

INSERT INTO areas (id, code, name, parent_area_id, created_at)
SELECT gen_random_uuid(), v.code, v.name, (SELECT id FROM areas WHERE code = 'HCM'), now()
FROM (VALUES ('HCM-Q1','HCM - Quận 1'), ('HCM-Q3','HCM - Quận 3')) AS v(code, name)
WHERE NOT EXISTS (SELECT 1 FROM areas a WHERE a.code = v.code);

-- ---------- STORES (with GPS; ST-0006 intentionally inactive) ----------
INSERT INTO stores (id, code, name, address, latitude, longitude, area_id, status, created_at)
SELECT gen_random_uuid(), v.code, v.name, v.address, v.lat, v.lon,
       (SELECT id FROM areas WHERE code = v.area_code), v.status, now()
FROM (VALUES
  ('ST-0001','Coopmart Q1','123 Lê Lợi',                 10.7769, 106.7009, 'HCM',    'active'),
  ('ST-0002','Coopmart Q3','189 Nam Kỳ Khởi Nghĩa',      10.7860, 106.6890, 'HCM-Q3', 'active'),
  ('ST-0003','Bách hóa Xanh Lê Văn Sỹ','312 Lê Văn Sỹ',  10.7910, 106.6720, 'HCM-Q3', 'active'),
  ('ST-0004','VinMart Nguyễn Huệ','45 Nguyễn Huệ',        10.7740, 106.7040, 'HCM-Q1', 'active'),
  ('ST-0005','Coopmart Hà Đông','2 Quang Trung, Hà Đông', 20.9710, 105.7780, 'HN',     'active'),
  ('ST-0006','BigC Đà Nẵng','255 Hùng Vương',            16.0670, 108.2120, 'DN',     'inactive')
) AS v(code, name, address, lat, lon, area_code, status)
WHERE NOT EXISTS (SELECT 1 FROM stores s WHERE s.code = v.code);

-- ---------- CATEGORIES ----------
INSERT INTO categories (id, code, name, created_at)
SELECT gen_random_uuid(), v.code, v.name, now()
FROM (VALUES
  ('BEV','Nước giải khát'),
  ('SNACK','Bánh kẹo'),
  ('DAIRY','Sữa & sản phẩm từ sữa'),
  ('PCARE','Chăm sóc cá nhân')
) AS v(code, name)
WHERE NOT EXISTS (SELECT 1 FROM categories c WHERE c.code = v.code);

-- ---------- ASSIGNMENTS (uses any existing PG + Leader) ----------
DO $$
DECLARE
  v_pg     uuid := (SELECT id FROM users WHERE role = 'pg'     AND deleted_at IS NULL ORDER BY created_at LIMIT 1);
  v_leader uuid := (SELECT id FROM users WHERE role = 'leader' AND deleted_at IS NULL ORDER BY created_at LIMIT 1);
  v_st1    uuid := (SELECT id FROM stores WHERE code = 'ST-0001');
  v_st2    uuid := (SELECT id FROM stores WHERE code = 'ST-0002');
  v_bev    uuid := (SELECT id FROM categories WHERE code = 'BEV');
  v_snack  uuid := (SELECT id FROM categories WHERE code = 'SNACK');
BEGIN
  IF v_pg IS NULL THEN
    RAISE NOTICE 'No PG user found — skipping assignment seed. Create a PG (and Leader) then re-run.';
    RETURN;
  END IF;

  -- PG -> Leader (1:1 active)
  IF v_leader IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM user_leader_assignments WHERE pg_user_id = v_pg AND effective_to IS NULL) THEN
    INSERT INTO user_leader_assignments (id, pg_user_id, leader_user_id, effective_from, created_at)
    VALUES (gen_random_uuid(), v_pg, v_leader, current_date, now());
  END IF;

  -- PG -> Stores
  IF v_st1 IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM user_store_assignments WHERE user_id = v_pg AND store_id = v_st1 AND effective_to IS NULL) THEN
    INSERT INTO user_store_assignments (id, user_id, store_id, effective_from, created_at)
    VALUES (gen_random_uuid(), v_pg, v_st1, current_date, now());
  END IF;
  IF v_st2 IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM user_store_assignments WHERE user_id = v_pg AND store_id = v_st2 AND effective_to IS NULL) THEN
    INSERT INTO user_store_assignments (id, user_id, store_id, effective_from, created_at)
    VALUES (gen_random_uuid(), v_pg, v_st2, current_date, now());
  END IF;

  -- PG -> Categories
  IF v_bev IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM user_category_assignments WHERE user_id = v_pg AND category_id = v_bev) THEN
    INSERT INTO user_category_assignments (id, user_id, category_id, created_at)
    VALUES (gen_random_uuid(), v_pg, v_bev, now());
  END IF;
  IF v_snack IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM user_category_assignments WHERE user_id = v_pg AND category_id = v_snack) THEN
    INSERT INTO user_category_assignments (id, user_id, category_id, created_at)
    VALUES (gen_random_uuid(), v_pg, v_snack, now());
  END IF;

  RAISE NOTICE 'M03 assignment seed applied for PG % (leader=%).', v_pg, v_leader;
END $$;

-- ---------- Summary ----------
SELECT 'areas' AS entity, count(*) FROM areas
UNION ALL SELECT 'stores', count(*) FROM stores
UNION ALL SELECT 'categories', count(*) FROM categories
UNION ALL SELECT 'active_leader_assignments', count(*) FROM user_leader_assignments WHERE effective_to IS NULL
UNION ALL SELECT 'active_store_assignments', count(*) FROM user_store_assignments WHERE effective_to IS NULL
UNION ALL SELECT 'category_assignments', count(*) FROM user_category_assignments;
