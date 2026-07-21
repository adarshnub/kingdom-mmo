export interface CityBuildingRule {
  slotKey: string;
  buildingType: string;
  displayName: string;
  requiredPalaceLevel: number;
  initialLevel: number;
}

// This is intentionally data-driven so new accounts, existing accounts, and
// upgrade validation all use the same city layout rules.
export const CITY_BUILDING_RULES: readonly CityBuildingRule[] = [
  { slotKey: 'palace', buildingType: 'palace', displayName: 'Palace', requiredPalaceLevel: 1, initialLevel: 1 },
  { slotKey: 'wall', buildingType: 'wall', displayName: 'Wall', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'farm_1', buildingType: 'farm', displayName: 'Farm', requiredPalaceLevel: 1, initialLevel: 1 },
  { slotKey: 'farm_2', buildingType: 'farm', displayName: 'Farm', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'lumberyard_1', buildingType: 'lumberyard', displayName: 'Lumberyard', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'lumberyard_2', buildingType: 'lumberyard', displayName: 'Lumberyard', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'barracks', buildingType: 'barracks', displayName: 'Barracks', requiredPalaceLevel: 1, initialLevel: 1 },
  { slotKey: 'stable', buildingType: 'stable', displayName: 'Stable', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'range', buildingType: 'range', displayName: 'Range', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'workshop', buildingType: 'workshop', displayName: 'Workshop', requiredPalaceLevel: 1, initialLevel: 0 },
  { slotKey: 'drill_grounds', buildingType: 'drill_grounds', displayName: 'Drill Grounds', requiredPalaceLevel: 3, initialLevel: 0 },
  { slotKey: 'academy', buildingType: 'academy', displayName: 'Academy', requiredPalaceLevel: 4, initialLevel: 0 },
  { slotKey: 'hospital', buildingType: 'hospital', displayName: 'Hospital', requiredPalaceLevel: 4, initialLevel: 0 },
  { slotKey: 'tavern', buildingType: 'tavern', displayName: 'Tavern', requiredPalaceLevel: 5, initialLevel: 0 },
  { slotKey: 'embassy', buildingType: 'embassy', displayName: 'Embassy', requiredPalaceLevel: 6, initialLevel: 0 },
  { slotKey: 'watchtower', buildingType: 'watchtower', displayName: 'Watchtower', requiredPalaceLevel: 7, initialLevel: 0 },
  { slotKey: 'forge', buildingType: 'forge', displayName: 'Forge', requiredPalaceLevel: 8, initialLevel: 0 },
  { slotKey: 'iron_mine', buildingType: 'iron_mine', displayName: 'Iron Mine', requiredPalaceLevel: 9, initialLevel: 0 },
  { slotKey: 'market', buildingType: 'market', displayName: 'Market', requiredPalaceLevel: 10, initialLevel: 0 },
  { slotKey: 'hall_of_war', buildingType: 'hall_of_war', displayName: 'Hall of War', requiredPalaceLevel: 12, initialLevel: 0 },
  { slotKey: 'silver_mine', buildingType: 'silver_mine', displayName: 'Silver Mine', requiredPalaceLevel: 15, initialLevel: 0 },
  { slotKey: 'warehouse', buildingType: 'warehouse', displayName: 'Warehouse', requiredPalaceLevel: 1, initialLevel: 0 },
];

export function getCityBuildingRule(slotKey: string): CityBuildingRule | undefined {
  return CITY_BUILDING_RULES.find((rule) => rule.slotKey === slotKey);
}
