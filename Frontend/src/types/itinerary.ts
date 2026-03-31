export interface Itinerary {
  id: string;
  version: string;
  title: string;
  description: string;
  destination: string;
  startDate: string;
  endDate: string;
  createdBy: string;
  memberIds: string[];
  memberCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface ItineraryMember {
  itineraryId: string;
  userId: string;
  name: string;
  email: string;
  avatar: string;
  addedByUserId: string;
  addedAt: string;
}

export interface ItineraryShareCode {
  itineraryId: string;
  version: string;
  code: string;
  updatedAt: string;
}
