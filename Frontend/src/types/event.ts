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
  version: string;
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

export interface EventAuditSnapshot {
  id: string;
  itineraryId: string;
  title: string;
  description: string | null;
  category: EventCategory;
  color: string | null;
  startDateTime: string;
  endDateTime: string;
  timezone: string;
  location: string | null;
  locationAddress: string | null;
  locationLat: number | null;
  locationLng: number | null;
  cost: number | null;
  updatedBy: string;
  updatedAt: string;
}

export interface EventAuditLog {
  id: string;
  eventId: string;
  itineraryId: string;
  action: 'Created' | 'Updated' | 'Deleted';
  summary: string;
  snapshot: EventAuditSnapshot;
  changedBy: string;
  changedAt: string;
}

export interface LocationSuggestion {
  id: string;
  name: string;
  address: string;
  lat: number;
  lng: number;
}
