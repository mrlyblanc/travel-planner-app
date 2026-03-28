export type EventCategory =
  | 'Hotel'
  | 'Restaurant'
  | 'Landmark'
  | 'Travel'
  | 'Activity'
  | 'Shopping'
  | 'Transport'
  | 'Other';

export interface ItineraryEvent {
  id: string;
  itineraryId: string;
  title: string;
  description: string;
  category: EventCategory;
  color: string;
  startDateTime: string;
  endDateTime: string;
  timezone: string;
  location: string;
  locationAddress: string;
  locationLat: number | null;
  locationLng: number | null;
  cost: number;
  createdBy: string;
  updatedBy: string;
  createdAt: string;
  updatedAt: string;
}

export interface LocationSuggestion {
  id: string;
  name: string;
  address: string;
  lat: number;
  lng: number;
}
