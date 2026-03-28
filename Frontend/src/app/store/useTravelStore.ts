import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { mockEvents } from '../../data/mockEvents';
import { mockCurrentUserId, mockUsers } from '../../data/mockUsers';
import { mockItineraries } from '../../data/mockItineraries';
import { dayjs } from '../../lib/date';
import { getDefaultEventColor } from '../../lib/events';
import { APP_STORAGE_KEY } from '../../lib/storage';
import { uid } from '../../lib/utils';
import type { ItineraryEvent } from '../../types/event';
import type { Itinerary } from '../../types/itinerary';
import type { User } from '../../types/user';

export interface ItineraryInput {
  title: string;
  description: string;
  destination: string;
  startDate: string;
  endDate: string;
}

export interface EventInput {
  title: string;
  description: string;
  category: ItineraryEvent['category'];
  color: string;
  startDateTime: string;
  endDateTime: string;
  timezone: string;
  location: string;
  locationAddress: string;
  locationLat: number | null;
  locationLng: number | null;
  cost: number;
}

interface TravelState {
  users: User[];
  itineraries: Itinerary[];
  events: ItineraryEvent[];
  currentUserId: string;
  createItinerary: (input: ItineraryInput) => string;
  updateItinerary: (itineraryId: string, input: ItineraryInput) => void;
  shareItinerary: (itineraryId: string, memberIds: string[]) => void;
  createEvent: (itineraryId: string, input: EventInput) => string;
  updateEvent: (eventId: string, input: EventInput) => void;
  deleteEvent: (eventId: string) => void;
  rescheduleEvent: (eventId: string, startDateTime: string, endDateTime: string) => void;
  seedDemoData: () => void;
}

const cloneUsers = () => mockUsers.map((user) => ({ ...user }));
const cloneItineraries = () => mockItineraries.map((itinerary) => ({ ...itinerary, memberIds: [...itinerary.memberIds] }));
const cloneEvents = () => mockEvents.map((event) => ({ ...event }));

const createSeedState = () => ({
  users: cloneUsers(),
  itineraries: cloneItineraries(),
  events: cloneEvents(),
  currentUserId: mockCurrentUserId,
});

const withMigratedEventColors = (events: Array<Partial<ItineraryEvent>>) =>
  events.map((event) => ({
    ...event,
    color: event.color ?? getDefaultEventColor(event.category ?? 'Other'),
  })) as ItineraryEvent[];

export const useTravelStore = create<TravelState>()(
  persist(
    (set, get) => ({
      ...createSeedState(),
      createItinerary: (input) => {
        const now = dayjs().toISOString();
        const itineraryId = uid('itinerary');
        const currentUserId = get().currentUserId;
        const itinerary: Itinerary = {
          id: itineraryId,
          title: input.title,
          description: input.description,
          destination: input.destination,
          startDate: input.startDate,
          endDate: input.endDate,
          createdBy: currentUserId,
          memberIds: [currentUserId],
          createdAt: now,
          updatedAt: now,
        };

        set((state) => ({
          itineraries: [itinerary, ...state.itineraries],
        }));

        return itineraryId;
      },
      updateItinerary: (itineraryId, input) => {
        const now = dayjs().toISOString();
        set((state) => ({
          itineraries: state.itineraries.map((itinerary) =>
            itinerary.id === itineraryId ? { ...itinerary, ...input, updatedAt: now } : itinerary,
          ),
        }));
      },
      shareItinerary: (itineraryId, memberIds) => {
        const now = dayjs().toISOString();
        set((state) => ({
          itineraries: state.itineraries.map((itinerary) =>
            itinerary.id === itineraryId
              ? {
                  ...itinerary,
                  memberIds: Array.from(new Set([itinerary.createdBy, ...memberIds])),
                  updatedAt: now,
                }
              : itinerary,
          ),
        }));
      },
      createEvent: (itineraryId, input) => {
        const now = dayjs().toISOString();
        const currentUserId = get().currentUserId;
        const eventId = uid('event');
        const event: ItineraryEvent = {
          id: eventId,
          itineraryId,
          ...input,
          createdBy: currentUserId,
          updatedBy: currentUserId,
          createdAt: now,
          updatedAt: now,
        };

        set((state) => ({
          events: [event, ...state.events],
          itineraries: state.itineraries.map((itinerary) =>
            itinerary.id === itineraryId ? { ...itinerary, updatedAt: now } : itinerary,
          ),
        }));

        return eventId;
      },
      updateEvent: (eventId, input) => {
        const now = dayjs().toISOString();
        const currentUserId = get().currentUserId;
        set((state) => {
          const targetEvent = state.events.find((event) => event.id === eventId);

          return {
            events: state.events.map((event) =>
              event.id === eventId ? { ...event, ...input, updatedBy: currentUserId, updatedAt: now } : event,
            ),
            itineraries: state.itineraries.map((itinerary) =>
              itinerary.id === targetEvent?.itineraryId ? { ...itinerary, updatedAt: now } : itinerary,
            ),
          };
        });
      },
      deleteEvent: (eventId) => {
        const now = dayjs().toISOString();
        set((state) => {
          const targetEvent = state.events.find((event) => event.id === eventId);

          return {
            events: state.events.filter((event) => event.id !== eventId),
            itineraries: state.itineraries.map((itinerary) =>
              itinerary.id === targetEvent?.itineraryId ? { ...itinerary, updatedAt: now } : itinerary,
            ),
          };
        });
      },
      rescheduleEvent: (eventId, startDateTime, endDateTime) => {
        const currentEvent = get().events.find((event) => event.id === eventId);
        if (!currentEvent) {
          return;
        }

        get().updateEvent(eventId, {
          title: currentEvent.title,
          description: currentEvent.description,
          category: currentEvent.category,
          color: currentEvent.color,
          startDateTime,
          endDateTime,
          timezone: currentEvent.timezone,
          location: currentEvent.location,
          locationAddress: currentEvent.locationAddress,
          locationLat: currentEvent.locationLat,
          locationLng: currentEvent.locationLng,
          cost: currentEvent.cost,
        });
      },
      seedDemoData: () => {
        if (typeof window !== 'undefined') {
          window.localStorage.removeItem(APP_STORAGE_KEY);
        }

        set((state) => ({
          ...state,
          ...createSeedState(),
        }));
      },
    }),
    {
      name: APP_STORAGE_KEY,
      version: 2,
      migrate: (persistedState) => {
        const state = persistedState as Partial<TravelState> | undefined;

        if (!state) {
          return {
            ...createSeedState(),
          };
        }

        return {
          ...state,
          users: state.users ?? cloneUsers(),
          itineraries: state.itineraries ?? cloneItineraries(),
          events: withMigratedEventColors((state.events as Array<Partial<ItineraryEvent>> | undefined) ?? cloneEvents()),
          currentUserId: state.currentUserId ?? mockCurrentUserId,
        };
      },
    },
  ),
);
