import { create } from 'zustand';
import { dayjs } from '../../lib/date';
import { ApiError, authSessionStorage, travelApi, type AuthSession, type EventInputDto, type ItineraryInputDto } from '../../lib/api';
import { itineraryRealtimeClient } from '../../lib/realtime';
import type { EventAuditLog, ItineraryEvent } from '../../types/event';
import type { Itinerary, ItineraryMember } from '../../types/itinerary';
import type { User } from '../../types/user';

export interface ItineraryInput extends ItineraryInputDto {}

export interface EventInput extends EventInputDto {}

export interface RegisterInput {
  name: string;
  email: string;
  password: string;
  avatar?: string;
}

export interface ChangePasswordInput {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

interface TravelState {
  users: User[];
  itineraries: Itinerary[];
  events: ItineraryEvent[];
  itineraryMembers: Record<string, ItineraryMember[]>;
  eventHistory: Record<string, EventAuditLog[]>;
  currentUserId: string;
  accessToken: string | null;
  refreshToken: string | null;
  isBootstrapping: boolean;
  isReady: boolean;
  error: string | null;
  clearError: () => void;
  bootstrap: () => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  changePassword: (input: ChangePasswordInput) => Promise<void>;
  logout: () => Promise<void>;
  refreshAll: () => Promise<void>;
  refreshItineraryBundle: (itineraryId: string) => Promise<void>;
  searchUsers: (query: string) => Promise<User[]>;
  loadEventHistory: (eventId: string) => Promise<void>;
  createItinerary: (input: ItineraryInput) => Promise<string>;
  updateItinerary: (itineraryId: string, input: ItineraryInput) => Promise<void>;
  shareItinerary: (itineraryId: string, memberIds: string[]) => Promise<void>;
  createEvent: (itineraryId: string, input: EventInput) => Promise<string>;
  updateEvent: (eventId: string, input: EventInput) => Promise<void>;
  deleteEvent: (eventId: string) => Promise<void>;
  rescheduleEvent: (eventId: string, startDateTime: string, endDateTime: string) => Promise<void>;
}

const emptyDataState = {
  users: [] as User[],
  itineraries: [] as Itinerary[],
  events: [] as ItineraryEvent[],
  itineraryMembers: {} as Record<string, ItineraryMember[]>,
  eventHistory: {} as Record<string, EventAuditLog[]>,
  currentUserId: '',
  accessToken: null as string | null,
  refreshToken: null as string | null,
};

const mergeUsers = (...collections: User[][]) => {
  const lookup = new Map<string, User>();

  for (const collection of collections) {
    for (const user of collection) {
      lookup.set(user.id, {
        ...lookup.get(user.id),
        ...user,
      });
    }
  }

  return Array.from(lookup.values()).sort((left, right) => left.name.localeCompare(right.name));
};

const memberToUser = (member: ItineraryMember): User => ({
  id: member.userId,
  name: member.name,
  email: member.email,
  avatar: member.avatar,
});

const upsertItinerary = (itineraries: Itinerary[], nextItinerary: Itinerary) => {
  const existing = itineraries.filter((itinerary) => itinerary.id !== nextItinerary.id);
  return [...existing, nextItinerary].sort((left, right) => left.startDate.localeCompare(right.startDate));
};

const upsertEvent = (events: ItineraryEvent[], nextEvent: ItineraryEvent) => {
  const existing = events.filter((event) => event.id !== nextEvent.id);
  return [...existing, nextEvent];
};

const restoreSession = async (existingSession?: AuthSession | null): Promise<AuthSession | null> => {
  let session = existingSession ?? authSessionStorage.load();

  if (!session) {
    return null;
  }

  const refreshThreshold = dayjs().add(5, 'minute');

  try {
    if (dayjs(session.expiresAt).isBefore(refreshThreshold) && session.refreshToken) {
      session = await travelApi.refresh(session.refreshToken);
      return session;
    }

    const currentUser = await travelApi.getCurrentUser(session.accessToken);
    session = {
      ...session,
      user: currentUser,
    };
    authSessionStorage.save(session);
    return session;
  } catch {
    if (session.refreshToken) {
      try {
        return await travelApi.refresh(session.refreshToken);
      } catch {
        authSessionStorage.clear();
        return null;
      }
    }

    authSessionStorage.clear();
    return null;
  }
};

const requireSession = async () => {
  const session = await restoreSession();
  if (!session) {
    throw new ApiError('Please sign in to continue.', 401);
  }

  return session;
};

const fetchBundle = async (accessToken: string) => {
  const [currentUser, itineraryDtos] = await Promise.all([
    travelApi.getCurrentUser(accessToken),
    travelApi.listItineraries(accessToken),
  ]);

  const itineraryMembersEntries = await Promise.all(
    itineraryDtos.map(async (itineraryDto) => {
      const [members, events] = await Promise.all([
        travelApi.listItineraryMembers(accessToken, itineraryDto.id),
        travelApi.listEvents(accessToken, itineraryDto.id),
      ]);

      return {
        itinerary: travelApi.mapItinerary(itineraryDto, members),
        members,
        events,
      };
    }),
  );

  const itineraryMembers = itineraryMembersEntries.reduce<Record<string, ItineraryMember[]>>((accumulator, entry) => {
    accumulator[entry.itinerary.id] = entry.members;
    return accumulator;
  }, {});

  const itineraries = itineraryMembersEntries.map((entry) => entry.itinerary);
  const events = itineraryMembersEntries.flatMap((entry) => entry.events);
  const users = mergeUsers([currentUser], itineraryMembersEntries.flatMap((entry) => entry.members.map(memberToUser)));

  return {
    currentUser,
    users,
    itineraries,
    itineraryMembers,
    events,
  };
};

const buildAuthenticatedState = (session: AuthSession, bundle: Awaited<ReturnType<typeof fetchBundle>>) => ({
  users: bundle.users,
  itineraries: bundle.itineraries,
  events: bundle.events,
  itineraryMembers: bundle.itineraryMembers,
  eventHistory: {},
  currentUserId: bundle.currentUser.id,
  accessToken: session.accessToken,
  refreshToken: session.refreshToken,
  isBootstrapping: false,
  isReady: true,
  error: null,
});

export const useTravelStore = create<TravelState>((set, get) => ({
  ...emptyDataState,
  isBootstrapping: false,
  isReady: false,
  error: null,

  clearError: () => {
    set({ error: null });
  },

  bootstrap: async () => {
    if (get().isBootstrapping) {
      return;
    }

    set({ isBootstrapping: true, error: null });

    const storedSession = authSessionStorage.load();
    if (!storedSession) {
      set({
        ...emptyDataState,
        isBootstrapping: false,
        isReady: true,
        error: null,
      });
      return;
    }

    try {
      const session = await restoreSession(storedSession);
      if (!session) {
        set({
          ...emptyDataState,
          isBootstrapping: false,
          isReady: true,
          error: null,
        });
        return;
      }

      const bundle = await fetchBundle(session.accessToken);

      set(buildAuthenticatedState(session, bundle));
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to connect to the TravelPlannerApp backend.';
      set({
        ...emptyDataState,
        isBootstrapping: false,
        isReady: true,
        error: message,
      });
    }
  },

  login: async (email, password) => {
    set({ isBootstrapping: true, error: null });

    try {
      const session = await travelApi.login({ email, password });
      const bundle = await fetchBundle(session.accessToken);

      set(buildAuthenticatedState(session, bundle));
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to sign in.';
      set({
        ...emptyDataState,
        isBootstrapping: false,
        isReady: true,
        error: message,
      });
      throw error;
    }
  },

  register: async (input) => {
    set({ isBootstrapping: true, error: null });

    try {
      await travelApi.register({
        name: input.name,
        email: input.email,
        password: input.password,
        avatar: input.avatar,
      });

      const session = await travelApi.login({
        email: input.email,
        password: input.password,
      });

      const bundle = await fetchBundle(session.accessToken);
      set(buildAuthenticatedState(session, bundle));
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to create your account.';
      set({
        ...emptyDataState,
        isBootstrapping: false,
        isReady: true,
        error: message,
      });
      throw error;
    }
  },

  changePassword: async (input) => {
    const session = await requireSession();

    try {
      await travelApi.changePassword(session.accessToken, input);

      set({
        accessToken: session.accessToken,
        refreshToken: session.refreshToken,
        error: null,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to change password.';
      set({ error: message });
      throw error;
    }
  },

  logout: async () => {
    const refreshToken = get().refreshToken ?? authSessionStorage.load()?.refreshToken ?? null;

    try {
      if (refreshToken) {
        await travelApi.logout(refreshToken);
      }
    } catch {
    }

    authSessionStorage.clear();

    await itineraryRealtimeClient.disconnect();

    set({
      ...emptyDataState,
      isBootstrapping: false,
      isReady: true,
      error: null,
    });
  },

  refreshAll: async () => {
    const session = await requireSession();
    const bundle = await fetchBundle(session.accessToken);

    set((state) => ({
      users: mergeUsers(state.users, bundle.users),
      itineraries: bundle.itineraries,
      itineraryMembers: bundle.itineraryMembers,
      events: bundle.events,
      currentUserId: bundle.currentUser.id,
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      error: null,
    }));
  },

  refreshItineraryBundle: async (itineraryId) => {
    const session = await requireSession();
    const [itineraryDto, members, events] = await Promise.all([
      travelApi.getItinerary(session.accessToken, itineraryId),
      travelApi.listItineraryMembers(session.accessToken, itineraryId),
      travelApi.listEvents(session.accessToken, itineraryId),
    ]);

    const itinerary = travelApi.mapItinerary(itineraryDto, members);

    set((state) => ({
      users: mergeUsers(state.users, members.map(memberToUser)),
      itineraries: upsertItinerary(state.itineraries, itinerary),
      itineraryMembers: {
        ...state.itineraryMembers,
        [itineraryId]: members,
      },
      events: [...state.events.filter((event) => event.itineraryId !== itineraryId), ...events],
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      error: null,
    }));
  },

  searchUsers: async (query) => {
    const trimmedQuery = query.trim();
    if (trimmedQuery.length < 2) {
      return [];
    }

    const session = await requireSession();
    const users = await travelApi.searchUsers(session.accessToken, trimmedQuery, 25);

    set((state) => ({
      users: mergeUsers(state.users, users),
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
    }));

    return users;
  },

  loadEventHistory: async (eventId) => {
    const session = await requireSession();
    const auditHistory = await travelApi.getEventHistory(session.accessToken, eventId);

    set((state) => ({
      eventHistory: {
        ...state.eventHistory,
        [eventId]: auditHistory,
      },
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
    }));
  },

  createItinerary: async (input) => {
    const session = await requireSession();
    const itineraryDto = await travelApi.createItinerary(session.accessToken, input);
    const currentUser = session.user;

    const members: ItineraryMember[] = [
      {
        itineraryId: itineraryDto.id,
        userId: currentUser.id,
        name: currentUser.name,
        email: currentUser.email,
        avatar: currentUser.avatar,
        addedByUserId: currentUser.id,
        addedAt: itineraryDto.createdAtUtc,
      },
    ];

    const itinerary = travelApi.mapItinerary(itineraryDto, members);

    set((state) => ({
      users: mergeUsers(state.users, [currentUser]),
      itineraries: upsertItinerary(state.itineraries, itinerary),
      itineraryMembers: {
        ...state.itineraryMembers,
        [itinerary.id]: members,
      },
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      currentUserId: currentUser.id,
      error: null,
    }));

    return itinerary.id;
  },

  updateItinerary: async (itineraryId, input) => {
    const state = get();
    const itinerary = state.itineraries.find((entry) => entry.id === itineraryId);
    if (!itinerary) {
      return;
    }

    const session = await requireSession();
    const updatedDto = await travelApi.updateItinerary(session.accessToken, itineraryId, input, itinerary.version);
    const members = state.itineraryMembers[itineraryId] ?? [];

    set((currentState) => ({
      itineraries: upsertItinerary(currentState.itineraries, travelApi.mapItinerary(updatedDto, members)),
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      error: null,
    }));
  },

  shareItinerary: async (itineraryId, memberIds) => {
    const state = get();
    const itinerary = state.itineraries.find((entry) => entry.id === itineraryId);
    if (!itinerary) {
      return;
    }

    const session = await requireSession();
    const replacement = await travelApi.replaceItineraryMembers(session.accessToken, itineraryId, memberIds, itinerary.version);
    const refreshedDto = await travelApi.getItinerary(session.accessToken, itineraryId);
    const refreshedItinerary = travelApi.mapItinerary(
      {
        ...refreshedDto,
        version: replacement.version,
      },
      replacement.members,
    );

    set((currentState) => ({
      users: mergeUsers(currentState.users, replacement.members.map(memberToUser)),
      itineraries: upsertItinerary(currentState.itineraries, refreshedItinerary),
      itineraryMembers: {
        ...currentState.itineraryMembers,
        [itineraryId]: replacement.members,
      },
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      error: null,
    }));
  },

  createEvent: async (itineraryId, input) => {
    const session = await requireSession();
    const event = await travelApi.createEvent(session.accessToken, itineraryId, input);

    set((state) => ({
      events: upsertEvent(state.events, event),
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      error: null,
    }));

    return event.id;
  },

  updateEvent: async (eventId, input) => {
    const state = get();
    const currentEvent = state.events.find((event) => event.id === eventId);
    if (!currentEvent) {
      return;
    }

    const session = await requireSession();
    const event = await travelApi.updateEvent(session.accessToken, eventId, input, currentEvent.version);

    set((currentState) => ({
      events: upsertEvent(currentState.events, event),
      eventHistory: {
        ...currentState.eventHistory,
        [eventId]: [],
      },
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
      error: null,
    }));
  },

  deleteEvent: async (eventId) => {
    const state = get();
    const currentEvent = state.events.find((event) => event.id === eventId);
    if (!currentEvent) {
      return;
    }

    const session = await requireSession();
    await travelApi.deleteEvent(session.accessToken, eventId, currentEvent.version);

    set((currentState) => {
      const nextHistory = { ...currentState.eventHistory };
      delete nextHistory[eventId];

      return {
        events: currentState.events.filter((event) => event.id !== eventId),
        eventHistory: nextHistory,
        accessToken: session.accessToken,
        refreshToken: session.refreshToken,
        error: null,
      };
    });
  },

  rescheduleEvent: async (eventId, startDateTime, endDateTime) => {
    const currentEvent = get().events.find((event) => event.id === eventId);
    if (!currentEvent) {
      return;
    }

    await get().updateEvent(eventId, {
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
}));
