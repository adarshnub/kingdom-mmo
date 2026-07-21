CREATE TABLE kingdoms (
  id uuid PRIMARY KEY,
  code text NOT NULL UNIQUE,
  status text NOT NULL CHECK (status IN ('hidden', 'open', 'closed', 'maintenance')),
  map_size integer NOT NULL CHECK (map_size BETWEEN 100 AND 10000),
  opened_at timestamptz NOT NULL
);

CREATE TABLE accounts (
  id uuid PRIMARY KEY,
  installation_id uuid NOT NULL UNIQUE,
  status text NOT NULL CHECK (status IN ('active', 'disabled', 'deletion_pending')),
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE sessions (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  refresh_token_hash text NOT NULL UNIQUE,
  expires_at timestamptz NOT NULL,
  revoked_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE INDEX sessions_account_active_idx ON sessions (account_id, expires_at) WHERE revoked_at IS NULL;

CREATE TABLE players (
  id uuid PRIMARY KEY,
  account_id uuid NOT NULL UNIQUE REFERENCES accounts(id),
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  display_name text NOT NULL CHECK (char_length(display_name) BETWEEN 3 AND 24),
  power bigint NOT NULL DEFAULT 0 CHECK (power >= 0),
  vip_level integer NOT NULL DEFAULT 0 CHECK (vip_level BETWEEN 0 AND 25),
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE UNIQUE INDEX players_kingdom_name_idx ON players (kingdom_id, lower(display_name));

CREATE TABLE cities (
  id uuid PRIMARY KEY,
  player_id uuid NOT NULL UNIQUE REFERENCES players(id),
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  x integer NOT NULL CHECK (x BETWEEN 0 AND 1199),
  y integer NOT NULL CHECK (y BETWEEN 0 AND 1199),
  resources jsonb NOT NULL,
  shield_until timestamptz,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  UNIQUE (kingdom_id, x, y),
  CHECK (jsonb_typeof(resources) = 'object')
);

CREATE TABLE buildings (
  id uuid PRIMARY KEY,
  city_id uuid NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
  slot_key text NOT NULL,
  building_type text NOT NULL,
  level integer NOT NULL CHECK (level BETWEEN 0 AND 40),
  version integer NOT NULL DEFAULT 1,
  UNIQUE (city_id, slot_key)
);

CREATE TABLE troop_stacks (
  city_id uuid NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
  troop_type text NOT NULL CHECK (troop_type IN ('infantry', 'cavalry', 'archer', 'siege')),
  tier integer NOT NULL CHECK (tier BETWEEN 1 AND 10),
  count bigint NOT NULL CHECK (count >= 0),
  wounded_count bigint NOT NULL DEFAULT 0 CHECK (wounded_count >= 0),
  PRIMARY KEY (city_id, troop_type, tier)
);

CREATE TABLE queue_jobs (
  id uuid PRIMARY KEY,
  city_id uuid NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
  job_type text NOT NULL,
  ref_id uuid,
  start_at timestamptz NOT NULL,
  finish_at timestamptz NOT NULL,
  status text NOT NULL CHECK (status IN ('pending', 'processing', 'completed', 'cancelled', 'failed')),
  payload jsonb NOT NULL DEFAULT '{}'::jsonb,
  lease_until timestamptz,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  CHECK (finish_at >= start_at)
);
CREATE INDEX queue_jobs_due_idx ON queue_jobs (finish_at) WHERE status IN ('pending', 'processing');
CREATE UNIQUE INDEX queue_jobs_one_build_idx ON queue_jobs (city_id)
  WHERE job_type = 'building_upgrade' AND status IN ('pending', 'processing');

CREATE TABLE map_objects (
  id uuid PRIMARY KEY,
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  object_type text NOT NULL,
  x integer NOT NULL CHECK (x BETWEEN 0 AND 1199),
  y integer NOT NULL CHECK (y BETWEEN 0 AND 1199),
  level integer NOT NULL CHECK (level BETWEEN 1 AND 100),
  state jsonb NOT NULL DEFAULT '{}'::jsonb,
  respawn_at timestamptz,
  UNIQUE (kingdom_id, x, y)
);
CREATE INDEX map_objects_viewport_idx ON map_objects (kingdom_id, x, y);

CREATE TABLE marches (
  id uuid PRIMARY KEY,
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  player_id uuid NOT NULL REFERENCES players(id),
  city_id uuid NOT NULL REFERENCES cities(id),
  march_type text NOT NULL CHECK (march_type IN ('attack', 'scout', 'gather', 'reinforce', 'occupy', 'rally_join')),
  target_id uuid NOT NULL REFERENCES map_objects(id),
  origin_x integer NOT NULL,
  origin_y integer NOT NULL,
  target_x integer NOT NULL,
  target_y integer NOT NULL,
  depart_at timestamptz NOT NULL,
  arrive_at timestamptz NOT NULL,
  status text NOT NULL CHECK (status IN ('outbound', 'resolving', 'returning', 'completed', 'cancelled')),
  payload jsonb NOT NULL,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE INDEX marches_due_idx ON marches (arrive_at) WHERE status IN ('outbound', 'returning');
CREATE INDEX marches_player_active_idx ON marches (player_id, arrive_at) WHERE status IN ('outbound', 'resolving', 'returning');

CREATE TABLE alliances (
  id uuid PRIMARY KEY,
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  tag text NOT NULL,
  name text NOT NULL,
  language text NOT NULL DEFAULT 'en',
  power bigint NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  UNIQUE (kingdom_id, tag),
  UNIQUE (kingdom_id, name)
);

CREATE TABLE alliance_members (
  alliance_id uuid NOT NULL REFERENCES alliances(id) ON DELETE CASCADE,
  player_id uuid NOT NULL UNIQUE REFERENCES players(id),
  role smallint NOT NULL CHECK (role BETWEEN 1 AND 5),
  contribution bigint NOT NULL DEFAULT 0,
  joined_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  PRIMARY KEY (alliance_id, player_id)
);

CREATE TABLE rallies (
  id uuid PRIMARY KEY,
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  alliance_id uuid NOT NULL REFERENCES alliances(id),
  leader_player_id uuid NOT NULL REFERENCES players(id),
  target_id uuid NOT NULL,
  launch_at timestamptz NOT NULL,
  status text NOT NULL,
  payload jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE battle_reports (
  id uuid NOT NULL,
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  owner_player_id uuid NOT NULL REFERENCES players(id),
  source_march_id uuid NOT NULL REFERENCES marches(id),
  rules_version integer NOT NULL,
  seed bigint NOT NULL,
  summary jsonb NOT NULL,
  details jsonb NOT NULL,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);
CREATE TABLE battle_reports_default PARTITION OF battle_reports DEFAULT;
CREATE INDEX battle_reports_owner_idx ON battle_reports (owner_player_id, created_at DESC);

CREATE TABLE mails (
  id uuid PRIMARY KEY,
  owner_player_id uuid NOT NULL REFERENCES players(id),
  mail_type text NOT NULL,
  title_key text NOT NULL,
  body jsonb NOT NULL,
  attachments jsonb NOT NULL DEFAULT '[]'::jsonb,
  read_at timestamptz,
  expires_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE INDEX mails_owner_idx ON mails (owner_player_id, created_at DESC);

CREATE TABLE chat_messages (
  id uuid NOT NULL,
  kingdom_id uuid NOT NULL REFERENCES kingdoms(id),
  channel_type text NOT NULL,
  channel_id uuid,
  sender_player_id uuid NOT NULL REFERENCES players(id),
  body text NOT NULL CHECK (char_length(body) BETWEEN 1 AND 500),
  moderation_state text NOT NULL DEFAULT 'visible',
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  PRIMARY KEY (id, created_at)
) PARTITION BY RANGE (created_at);
CREATE TABLE chat_messages_default PARTITION OF chat_messages DEFAULT;
CREATE INDEX chat_messages_channel_idx ON chat_messages (channel_type, channel_id, created_at DESC);

CREATE TABLE event_definitions (
  id uuid PRIMARY KEY,
  kingdom_id uuid REFERENCES kingdoms(id),
  event_type text NOT NULL,
  config jsonb NOT NULL,
  starts_at timestamptz NOT NULL,
  ends_at timestamptz NOT NULL,
  CHECK (ends_at > starts_at)
);

CREATE TABLE event_progress (
  event_id uuid NOT NULL REFERENCES event_definitions(id) ON DELETE CASCADE,
  player_id uuid NOT NULL REFERENCES players(id),
  score bigint NOT NULL DEFAULT 0,
  claimed jsonb NOT NULL DEFAULT '[]'::jsonb,
  PRIMARY KEY (event_id, player_id)
);

CREATE TABLE purchases (
  id uuid PRIMARY KEY,
  player_id uuid NOT NULL REFERENCES players(id),
  platform text NOT NULL CHECK (platform IN ('apple', 'google', 'local')),
  sku text NOT NULL,
  platform_transaction_id text NOT NULL,
  state text NOT NULL,
  payload jsonb NOT NULL,
  verified_at timestamptz,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  UNIQUE (platform, platform_transaction_id)
);

CREATE TABLE idempotency_keys (
  player_id uuid NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  key uuid NOT NULL,
  request_hash text NOT NULL,
  status_code integer NOT NULL,
  response jsonb NOT NULL,
  expires_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  PRIMARY KEY (player_id, key)
);
CREATE INDEX idempotency_expiry_idx ON idempotency_keys (expires_at);

CREATE TABLE economy_ledger (
  id uuid PRIMARY KEY,
  player_id uuid NOT NULL REFERENCES players(id),
  resource_key text NOT NULL,
  delta bigint NOT NULL,
  reason text NOT NULL,
  ref_type text NOT NULL,
  ref_id uuid NOT NULL,
  correlation_id text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);
CREATE INDEX economy_ledger_player_idx ON economy_ledger (player_id, created_at DESC);

CREATE TABLE outbox_events (
  sequence bigserial PRIMARY KEY,
  event_id uuid NOT NULL UNIQUE,
  aggregate_type text NOT NULL,
  aggregate_id uuid NOT NULL,
  event_type text NOT NULL,
  payload jsonb NOT NULL,
  occurred_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  published_at timestamptz
);
CREATE INDEX outbox_unpublished_idx ON outbox_events (sequence) WHERE published_at IS NULL;

CREATE TABLE processed_events (
  consumer text NOT NULL,
  event_id uuid NOT NULL,
  processed_at timestamptz NOT NULL DEFAULT clock_timestamp(),
  PRIMARY KEY (consumer, event_id)
);

CREATE TABLE admin_audit_log (
  id uuid PRIMARY KEY,
  admin_subject text NOT NULL,
  action text NOT NULL,
  target_type text NOT NULL,
  target_id text NOT NULL,
  reason text NOT NULL,
  before_state jsonb,
  after_state jsonb,
  created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);

