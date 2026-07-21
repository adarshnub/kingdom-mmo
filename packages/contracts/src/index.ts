import { z } from 'zod';

export const uuidSchema = z.string().uuid();
export const rfc3339Schema = z.string().datetime({ offset: true });
export const int64StringSchema = z
  .string()
  .regex(/^(0|[1-9]\d*)$/, 'Expected an unsigned decimal string');
export const signedInt64StringSchema = z.string().regex(/^-?(0|[1-9]\d*)$/);

export const resourcesSchema = z.object({
  food: int64StringSchema,
  wood: int64StringSchema,
  iron: int64StringSchema,
  silver: int64StringSchema,
  gold: int64StringSchema,
});

export const guestAuthRequestSchema = z.object({
  installation_id: uuidSchema,
  display_name: z.string().trim().min(3).max(24).optional(),
});

export const guestAuthResponseSchema = z.object({
  account_id: uuidSchema,
  player_id: uuidSchema,
  access_token: z.string().min(20),
  refresh_token: z.string().min(20),
  access_token_expires_at: rfc3339Schema,
});

export const buildingSchema = z.object({
  id: uuidSchema,
  slot_key: z.string(),
  building_type: z.string(),
  level: z.number().int().nonnegative(),
});

export const queueJobSchema = z.object({
  id: uuidSchema,
  job_type: z.string(),
  ref_id: uuidSchema.nullable(),
  start_at: rfc3339Schema,
  finish_at: rfc3339Schema,
  status: z.string(),
});

export const bootstrapResponseSchema = z.object({
  server_time: rfc3339Schema,
  player: z.object({
    id: uuidSchema,
    kingdom_id: uuidSchema,
    display_name: z.string(),
    power: int64StringSchema,
  }),
  city: z.object({
    id: uuidSchema,
    x: z.number().int(),
    y: z.number().int(),
    resources: resourcesSchema,
    buildings: z.array(buildingSchema),
    active_queues: z.array(queueJobSchema),
  }),
  active_marches: z.array(
    z.object({
      id: uuidSchema,
      march_type: z.string(),
      status: z.string(),
      arrive_at: rfc3339Schema,
    }),
  ),
});

export const startBuildRequestSchema = z.object({
  building_id: uuidSchema,
});

export const startBuildResponseSchema = z.object({
  queue: queueJobSchema,
  resources: resourcesSchema,
});

export const speedUpBuildRequestSchema = z.object({
  building_id: uuidSchema,
});

export const speedUpBuildResponseSchema = z.object({
  queue: queueJobSchema,
  building: buildingSchema,
});

export const viewportQuerySchema = z
  .object({
    min_x: z.coerce.number().int().min(0).max(1199),
    max_x: z.coerce.number().int().min(0).max(1199),
    min_y: z.coerce.number().int().min(0).max(1199),
    max_y: z.coerce.number().int().min(0).max(1199),
  })
  .refine((value) => value.max_x >= value.min_x && value.max_y >= value.min_y, {
    message: 'Viewport maximums must be greater than or equal to minimums',
  })
  .refine((value) => (value.max_x - value.min_x + 1) * (value.max_y - value.min_y + 1) <= 10_000, {
    message: 'Viewport is too large',
  });

export const mapObjectSchema = z.object({
  id: uuidSchema,
  object_type: z.string(),
  x: z.number().int(),
  y: z.number().int(),
  level: z.number().int().positive(),
  state: z.record(z.unknown()),
});

export const dispatchMarchRequestSchema = z.object({
  target_id: uuidSchema,
  march_type: z.enum(['attack', 'scout', 'gather', 'reinforce']),
  troops: z
    .array(
      z.object({
        troop_type: z.enum(['infantry', 'cavalry', 'archer', 'siege']),
        tier: z.number().int().min(1).max(10),
        count: int64StringSchema.refine(
          (value) => BigInt(value) > 0n,
          'Troop count must be positive',
        ),
      }),
    )
    .min(1)
    .max(40),
});

export const marchSchema = z.object({
  id: uuidSchema,
  march_type: z.string(),
  status: z.string(),
  depart_at: rfc3339Schema,
  arrive_at: rfc3339Schema,
  target_id: uuidSchema,
});

export const problemDetailsSchema = z.object({
  type: z.string(),
  title: z.string(),
  status: z.number().int(),
  code: z.string(),
  detail: z.string(),
  instance: z.string(),
  correlation_id: z.string(),
});

export const realtimeEnvelopeSchema = z.object({
  event_id: uuidSchema,
  type: z.string().regex(/^[a-z][a-z0-9_.-]+\.v\d+$/),
  sequence: int64StringSchema,
  occurred_at: rfc3339Schema,
  payload: z.record(z.unknown()),
});

export type Resources = z.infer<typeof resourcesSchema>;
export type GuestAuthRequest = z.infer<typeof guestAuthRequestSchema>;
export type GuestAuthResponse = z.infer<typeof guestAuthResponseSchema>;
export type BootstrapResponse = z.infer<typeof bootstrapResponseSchema>;
export type StartBuildRequest = z.infer<typeof startBuildRequestSchema>;
export type StartBuildResponse = z.infer<typeof startBuildResponseSchema>;
export type SpeedUpBuildRequest = z.infer<typeof speedUpBuildRequestSchema>;
export type SpeedUpBuildResponse = z.infer<typeof speedUpBuildResponseSchema>;
export type ViewportQuery = z.infer<typeof viewportQuerySchema>;
export type DispatchMarchRequest = z.infer<typeof dispatchMarchRequestSchema>;
export type RealtimeEnvelope = z.infer<typeof realtimeEnvelopeSchema>;
