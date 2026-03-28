import { mockLocations } from '../../data/mockLocations';
import type { LocationSuggestion } from '../../types/event';

export const searchMockLocations = async (query: string): Promise<LocationSuggestion[]> => {
  await new Promise((resolve) => window.setTimeout(resolve, 180));

  if (!query.trim()) {
    return mockLocations.slice(0, 5);
  }

  const loweredQuery = query.toLowerCase();

  return mockLocations
    .filter(
      (location) =>
        location.name.toLowerCase().includes(loweredQuery) ||
        location.address.toLowerCase().includes(loweredQuery),
    )
    .slice(0, 5);
};
