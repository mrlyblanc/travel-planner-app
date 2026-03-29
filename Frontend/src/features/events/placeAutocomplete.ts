import { mockLocations } from '../../data/mockLocations';
import type { LocationSuggestion } from '../../types/event';

const GEOAPIFY_API_KEY = import.meta.env.VITE_GEOAPIFY_API_KEY?.trim();
const GEOAPIFY_AUTOCOMPLETE_URL = 'https://api.geoapify.com/v1/geocode/autocomplete';
const AUTOCOMPLETE_RESULT_LIMIT = 5;

interface GeoapifyAutocompleteResult {
  place_id?: string;
  name?: string;
  formatted?: string;
  address_line1?: string;
  address_line2?: string;
  lat?: number;
  lon?: number;
}

interface GeoapifyAutocompleteResponse {
  results?: GeoapifyAutocompleteResult[];
}

export const isGeoapifyConfigured = Boolean(GEOAPIFY_API_KEY);

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
    .slice(0, AUTOCOMPLETE_RESULT_LIMIT);
};

const mapGeoapifyResult = (
  result: GeoapifyAutocompleteResult,
  index: number,
): LocationSuggestion | null => {
  if (typeof result.lat !== 'number' || typeof result.lon !== 'number') {
    return null;
  }

  const name = (result.address_line1 || result.name || result.formatted || '').trim();
  const address = (result.formatted || result.address_line2 || name).trim();

  if (!name || !address) {
    return null;
  }

  return {
    id: result.place_id ?? `geoapify-${index}-${name}-${address}`,
    name,
    address,
    lat: result.lat,
    lng: result.lon,
  };
};

const searchGeoapifyLocations = async (
  query: string,
  signal?: AbortSignal,
): Promise<LocationSuggestion[]> => {
  if (!GEOAPIFY_API_KEY) {
    return [];
  }

  const url = new URL(GEOAPIFY_AUTOCOMPLETE_URL);
  url.searchParams.set('text', query);
  url.searchParams.set('format', 'json');
  url.searchParams.set('lang', 'en');
  url.searchParams.set('limit', String(AUTOCOMPLETE_RESULT_LIMIT));
  url.searchParams.set('apiKey', GEOAPIFY_API_KEY);

  const response = await fetch(url, { signal });
  if (!response.ok) {
    throw new Error(`Geoapify autocomplete failed with status ${response.status}`);
  }

  const payload = (await response.json()) as GeoapifyAutocompleteResponse;

  return (payload.results ?? [])
    .map(mapGeoapifyResult)
    .filter((result): result is LocationSuggestion => result !== null)
    .slice(0, AUTOCOMPLETE_RESULT_LIMIT);
};

export const searchLocationSuggestions = async ({
  query,
  signal,
}: {
  query: string;
  signal?: AbortSignal;
}): Promise<LocationSuggestion[]> => {
  const trimmedQuery = query.trim();

  if (!trimmedQuery) {
    return isGeoapifyConfigured ? [] : searchMockLocations('');
  }

  if (!isGeoapifyConfigured) {
    return searchMockLocations(trimmedQuery);
  }

  try {
    return await searchGeoapifyLocations(trimmedQuery, signal);
  } catch (error) {
    if (signal?.aborted) {
      return [];
    }

    return searchMockLocations(trimmedQuery);
  }
};
