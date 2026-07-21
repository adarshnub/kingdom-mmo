import type { ColumnType, Generated } from 'kysely';

type Timestamp = ColumnType<Date, Date | string, Date | string>;
type JsonObject = Record<string, unknown>;

export interface KingdomTable {
  id: string;
  code: string;
  status: string;
  map_size: number;
  opened_at: Timestamp;
}

export interface AccountTable {
  id: string;
  installation_id: string;
  status: string;
  created_at: Generated<Date>;
}

export interface SessionTable {
  id: string;
  account_id: string;
  refresh_token_hash: string;
  expires_at: Timestamp;
  revoked_at: Timestamp | null;
  created_at: Generated<Date>;
}

export interface PlayerTable {
  id: string;
  account_id: string;
  kingdom_id: string;
  display_name: string;
  power: Generated<string>;
  vip_level: Generated<number>;
  created_at: Generated<Date>;
}

export interface CityTable {
  id: string;
  player_id: string;
  kingdom_id: string;
  x: number;
  y: number;
  resources: JsonObject;
  shield_until: Timestamp | null;
  created_at: Generated<Date>;
}

export interface BuildingTable {
  id: string;
  city_id: string;
  slot_key: string;
  building_type: string;
  level: number;
  version: Generated<number>;
}

export interface TroopStackTable {
  city_id: string;
  troop_type: string;
  tier: number;
  count: string;
  wounded_count: Generated<string>;
}

export interface QueueJobTable {
  id: string;
  city_id: string;
  job_type: string;
  ref_id: string | null;
  start_at: Timestamp;
  finish_at: Timestamp;
  status: string;
  payload: JsonObject;
  lease_until: Timestamp | null;
  created_at: Generated<Date>;
}

export interface MapObjectTable {
  id: string;
  kingdom_id: string;
  object_type: string;
  x: number;
  y: number;
  level: number;
  state: JsonObject;
  respawn_at: Timestamp | null;
}

export interface MarchTable {
  id: string;
  kingdom_id: string;
  player_id: string;
  city_id: string;
  march_type: string;
  target_id: string;
  origin_x: number;
  origin_y: number;
  target_x: number;
  target_y: number;
  depart_at: Timestamp;
  arrive_at: Timestamp;
  status: string;
  payload: JsonObject;
  created_at: Generated<Date>;
}

export interface BattleReportTable {
  id: string;
  kingdom_id: string;
  owner_player_id: string;
  source_march_id: string;
  rules_version: number;
  seed: number;
  summary: JsonObject;
  details: JsonObject;
  created_at: Generated<Date>;
}

export interface IdempotencyKeyTable {
  player_id: string;
  key: string;
  request_hash: string;
  status_code: number;
  response: JsonObject;
  expires_at: Timestamp;
  created_at: Generated<Date>;
}

export interface EconomyLedgerTable {
  id: string;
  player_id: string;
  resource_key: string;
  delta: string;
  reason: string;
  ref_type: string;
  ref_id: string;
  correlation_id: string;
  created_at: Generated<Date>;
}

export interface OutboxEventTable {
  sequence: Generated<string>;
  event_id: string;
  aggregate_type: string;
  aggregate_id: string;
  event_type: string;
  payload: JsonObject;
  occurred_at: Generated<Date>;
  published_at: Timestamp | null;
}

export interface Database {
  kingdoms: KingdomTable;
  accounts: AccountTable;
  sessions: SessionTable;
  players: PlayerTable;
  cities: CityTable;
  buildings: BuildingTable;
  troop_stacks: TroopStackTable;
  queue_jobs: QueueJobTable;
  map_objects: MapObjectTable;
  marches: MarchTable;
  battle_reports: BattleReportTable;
  idempotency_keys: IdempotencyKeyTable;
  economy_ledger: EconomyLedgerTable;
  outbox_events: OutboxEventTable;
}
