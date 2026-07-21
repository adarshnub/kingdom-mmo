INSERT INTO kingdoms (id, code, status, map_size, opened_at)
VALUES ('00000000-0000-7000-8000-000000000001', 'K1', 'open', 1200, '2026-07-21T00:00:00Z')
ON CONFLICT (code) DO UPDATE SET status = EXCLUDED.status, map_size = EXCLUDED.map_size;

INSERT INTO map_objects (id, kingdom_id, object_type, x, y, level, state)
SELECT
  (substr(md5('monster-' || value), 1, 8) || '-' || substr(md5('monster-' || value), 9, 4) || '-7' || substr(md5('monster-' || value), 14, 3) || '-8' || substr(md5('monster-' || value), 18, 3) || '-' || substr(md5('monster-' || value), 21, 12))::uuid,
  '00000000-0000-7000-8000-000000000001'::uuid,
  'monster',
  100 + ((value * 37) % 1000),
  100 + ((value * 73) % 1000),
  1 + (value % 10),
  jsonb_build_object('defender_troops', ((1 + (value % 10)) * 100)::text, 'monster_key', 'stone_guardian')
FROM generate_series(1, 50) AS value
ON CONFLICT (id) DO UPDATE SET level = EXCLUDED.level, state = EXCLUDED.state;

INSERT INTO map_objects (id, kingdom_id, object_type, x, y, level, state)
SELECT
  (substr(md5('node-' || value), 1, 8) || '-' || substr(md5('node-' || value), 9, 4) || '-7' || substr(md5('node-' || value), 14, 3) || '-8' || substr(md5('node-' || value), 18, 3) || '-' || substr(md5('node-' || value), 21, 12))::uuid,
  '00000000-0000-7000-8000-000000000001'::uuid,
  CASE value % 4 WHEN 0 THEN 'food_node' WHEN 1 THEN 'wood_node' WHEN 2 THEN 'iron_node' ELSE 'silver_node' END,
  100 + ((value * 53) % 1000),
  100 + ((value * 97) % 1000),
  1 + (value % 8),
  jsonb_build_object('remaining', ((1 + (value % 8)) * 10000)::text)
FROM generate_series(1, 50) AS value
ON CONFLICT (id) DO UPDATE SET level = EXCLUDED.level, state = EXCLUDED.state;

INSERT INTO event_definitions (id, kingdom_id, event_type, config, starts_at, ends_at)
VALUES (
  '00000000-0000-7000-8000-000000000101',
  '00000000-0000-7000-8000-000000000001',
  'daily_growth',
  '{"milestones":[100,500,1000]}'::jsonb,
  date_trunc('day', clock_timestamp()),
  date_trunc('day', clock_timestamp()) + interval '1 day'
)
ON CONFLICT (id) DO UPDATE SET starts_at = EXCLUDED.starts_at, ends_at = EXCLUDED.ends_at;

