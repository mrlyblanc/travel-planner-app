import { create } from 'zustand';
import { buildAllDayDateTimeRange, dayjs } from '../../lib/date';
import {
  ApiError,
  authSessionCache,
  travelApi,
  type AuthSession,
  type EventInputDto,
  type ItineraryInputDto,
  type UserRealtimeNotification,
  type ForgotPasswordResponse,
} from '../../lib/api';
import { itineraryRealtimeClient } from '../../lib/realtime';
import type { EventAuditLog, ItineraryEvent } from '../../types/event';
import type { UserNotification } from '../../types/notification';
import type { Itinerary, ItineraryMember, ItineraryShareCode } from '../../types/itinerary';
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

export interface ForgotPasswordInput {
  email: string;
}

export interface ResetPasswordInput {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

type ItineraryBundleStatus = 'idle' | 'loading' | 'loaded' | 'error';

interface TravelState {
  users: User[];
  itineraries: Itinerary[];
  events: ItineraryEvent[];
  itineraryMembers: Record<string, ItineraryMember[]>;
  itineraryBundleStatus: Record<string, ItineraryBundleStatus>;
  itineraryShareCodes: Record<string, ItineraryShareCode>;
  eventHistory: Record<string, EventAuditLog[]>;
  notifications: UserNotification[];
  currentUserId: string;
  accessToken: string | null;
  pendingMutationCount: number;
  isBootstrapping: boolean;
  isReady: boolean;
  error: string | null;
  clearError: () => void;
  bootstrap: () => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  register: (input: RegisterInput) => Promise<void>;
  forgotPassword: (input: ForgotPasswordInput) => Promise<ForgotPasswordResponse>;
  resetPassword: (input: ResetPasswordInput) => Promise<void>;
  changePassword: (input: ChangePasswordInput) => Promise<void>;
  logout: () => Promise<void>;
  refreshAll: () => Promise<void>;
  ensureItineraryBundle: (itineraryId: string) => Promise<void>;
  refreshItineraryBundle: (itineraryId: string) => Promise<void>;
  searchUsers: (query: string) => Promise<User[]>;
  loadEventHistory: (eventId: string) => Promise<void>;
  loadNotifications: () => Promise<void>;
  markNotificationRead: (notificationId: string) => Promise<void>;
  markAllNotificationsRead: () => Promise<void>;
  deleteNotification: (notificationId: string) => Promise<void>;
  addRealtimeNotification: (notification: UserRealtimeNotification) => void;
  loadItineraryShareCode: (itineraryId: string) => Promise<ItineraryShareCode>;
  rotateItineraryShareCode: (itineraryId: string) => Promise<ItineraryShareCode>;
  createItinerary: (input: ItineraryInput) => Promise<string>;
  updateItinerary: (itineraryId: string, input: ItineraryInput) => Promise<void>;
  joinItineraryByCode: (code: string) => Promise<string>;
  removeItineraryMember: (itineraryId: string, userId: string) => Promise<void>;
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
  itineraryBundleStatus: {} as Record<string, ItineraryBundleStatus>,
  itineraryShareCodes: {} as Record<string, ItineraryShareCode>,
  eventHistory: {} as Record<string, EventAuditLog[]>,
  notifications: [] as UserNotification[],
  currentUserId: '',
  accessToken: null as string | null,
  pendingMutationCount: 0,
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

const mergeNotifications = (...collections: UserNotification[][]) => {
  const lookup = new Map<string, UserNotification>();

  for (const collection of collections) {
    for (const notification of collection) {
      lookup.set(notification.id, {
        ...lookup.get(notification.id),
        ...notification,
      });
    }
  }

  return Array.from(lookup.values()).sort((left, right) => dayjs(right.createdAt).valueOf() - dayjs(left.createdAt).valueOf());
};

const sortItinerariesByStartDate = (itineraries: Itinerary[]) =>
  [...itineraries].sort((left, right) => left.startDate.localeCompare(right.startDate));

const memberToUser = (member: ItineraryMember): User => ({
  id: member.userId,
  name: member.name,
  email: member.email,
  avatar: member.avatar,
});

const upsertItinerary = (itineraries: Itinerary[], nextItinerary: Itinerary) => {
  const existing = itineraries.filter((itinerary) => itinerary.id !== nextItinerary.id);
  return sortItinerariesByStartDate([...existing, nextItinerary]);
};

const upsertEvent = (events: ItineraryEvent[], nextEvent: ItineraryEvent) => {
  const existing = events.filter((event) => event.id !== nextEvent.id);
  return [...existing, nextEvent];
};

const updateItineraryVersion = (itineraries: Itinerary[], itineraryId: string, version: string) =>
  itineraries.map((itinerary) => (itinerary.id === itineraryId ? { ...itinerary, version } : itinerary));

const buildShellItinerary = (
  itineraryDto: Awaited<ReturnType<typeof travelApi.listItineraries>>[number],
  currentUserId: string,
  members: ItineraryMember[] = [],
) => {
  const itinerary = travelApi.mapItinerary(itineraryDto, members);

  return members.length > 0 || !currentUserId
    ? itinerary
    : {
        ...itinerary,
        memberIds: [currentUserId],
      };
};

const filterRecordByKeys = <T>(record: Record<string, T>, validKeys: Set<string>) =>
  Object.fromEntries(Object.entries(record).filter(([key]) => validKeys.has(key))) as Record<string, T>;

const syncShareCodeVersions = (shareCodes: Record<string, ItineraryShareCode>, itineraries: Itinerary[]) => {
  const itineraryVersionMap = new Map(itineraries.map((itinerary) => [itinerary.id, itinerary.version]));
  const nextShareCodes: Record<string, ItineraryShareCode> = {};

  Object.entries(shareCodes).forEach(([itineraryId, shareCode]) => {
    const version = itineraryVersionMap.get(itineraryId);
    if (!version) {
      return;
    }

    nextShareCodes[itineraryId] = {
      ...shareCode,
      version,
    };
  });

  return nextShareCodes;
};

const isUnauthorizedApiError = (error: unknown) => error instanceof ApiError && error.status === 401;

const restoreSession = async (existingSession?: AuthSession | null): Promise<AuthSession | null> => {
  let session = existingSession ?? authSessionCache.load();
  const legacySession = !session ? authSessionCache.loadLegacy() : null;

  if (!session && legacySession?.refreshToken) {
    try {
      session = await travelApi.refresh(legacySession.refreshToken);
      authSessionCache.clearLegacy();
      return session;
    } catch (error) {
      authSessionCache.clearLegacy();
      if (!isUnauthorizedApiError(error)) {
        throw error;
      }
    }
  }

  if (!session) {
    try {
      return await travelApi.refresh();
    } catch (error) {
      if (isUnauthorizedApiError(error)) {
        return null;
      }

      throw error;
    }
  }

  const refreshSessionOrFail = async () => {
    try {
      return await travelApi.refresh();
    } catch (error) {
      authSessionCache.clear();
      if (isUnauthorizedApiError(error)) {
        return null;
      }

      throw error;
    }
  };

  if (dayjs(session.expiresAt).isBefore(dayjs().add(5, 'minute'))) {
    return refreshSessionOrFail();
  }

  try {
    const currentUser = await travelApi.getCurrentUser(session.accessToken);
    session = {
      ...session,
      user: currentUser,
    };
    authSessionCache.save(session);
    return session;
  } catch {
    return refreshSessionOrFail();
  }
};

const requireSession = async () => {
  const session = await restoreSession();
  if (!session) {
    throw new ApiError('Please sign in to continue.', 401);
  }

  return session;
};

const fetchWorkspaceShell = async (accessToken: string) => {
  const [currentUser, itineraryDtos, notifications] = await Promise.all([
    travelApi.getCurrentUser(accessToken),
    travelApi.listItineraries(accessToken),
    travelApi.listNotifications(accessToken),
  ]);

  return {
    currentUser,
    itineraryDtos,
    notifications,
  };
};

const buildAuthenticatedState = (session: AuthSession, shell: Awaited<ReturnType<typeof fetchWorkspaceShell>>) => ({
  users: [shell.currentUser],
  itineraries: sortItinerariesByStartDate(
    shell.itineraryDtos.map((itineraryDto) => buildShellItinerary(itineraryDto, shell.currentUser.id)),
  ),
  events: [],
  itineraryMembers: {},
  itineraryBundleStatus: Object.fromEntries(
    shell.itineraryDtos.map((itineraryDto) => [itineraryDto.id, 'idle' as ItineraryBundleStatus]),
  ),
  itineraryShareCodes: {},
  eventHistory: {},
  notifications: shell.notifications,
  currentUserId: shell.currentUser.id,
  accessToken: session.accessToken,
  pendingMutationCount: 0,
  isBootstrapping: false,
  isReady: true,
  error: null,
});

const mergeWorkspaceShellIntoState = (
  state: Pick<
    TravelState,
    | 'users'
    | 'itineraries'
    | 'events'
    | 'itineraryMembers'
    | 'itineraryBundleStatus'
    | 'itineraryShareCodes'
    | 'eventHistory'
    | 'notifications'
  >,
  shell: Awaited<ReturnType<typeof fetchWorkspaceShell>>,
  accessToken: string,
) => {
  const validItineraryIds = new Set(shell.itineraryDtos.map((itineraryDto) => itineraryDto.id));
  const nextItineraryMembers = filterRecordByKeys(state.itineraryMembers, validItineraryIds);
  const nextItineraryBundleStatus = filterRecordByKeys(state.itineraryBundleStatus, validItineraryIds);
  const nextEvents = state.events.filter((event) => validItineraryIds.has(event.itineraryId));
  const validEventIds = new Set(nextEvents.map((event) => event.id));
  const nextEventHistory = filterRecordByKeys(state.eventHistory, validEventIds);
  const nextItineraryShareCodes = filterRecordByKeys(state.itineraryShareCodes, validItineraryIds);
  const itineraries = sortItinerariesByStartDate(
    shell.itineraryDtos.map((itineraryDto) => {
      const members =
        nextItineraryBundleStatus[itineraryDto.id] === 'loaded' ? nextItineraryMembers[itineraryDto.id] ?? [] : [];

      return buildShellItinerary(itineraryDto, shell.currentUser.id, members);
    }),
  );

  shell.itineraryDtos.forEach((itineraryDto) => {
    if (!nextItineraryBundleStatus[itineraryDto.id]) {
      nextItineraryBundleStatus[itineraryDto.id] = 'idle';
    }
  });

  return {
    users: mergeUsers(state.users, [shell.currentUser], Object.values(nextItineraryMembers).flat().map(memberToUser)),
    itineraries,
    events: nextEvents,
    itineraryMembers: nextItineraryMembers,
    itineraryBundleStatus: nextItineraryBundleStatus,
    itineraryShareCodes: syncShareCodeVersions(nextItineraryShareCodes, itineraries),
    eventHistory: nextEventHistory,
    notifications: mergeNotifications(state.notifications, shell.notifications),
    currentUserId: shell.currentUser.id,
    accessToken,
    error: null,
  };
};

const itineraryBundleRequests = new Map<string, Promise<void>>();

export const useTravelStore = create<TravelState>((set, get) => {
  const beginMutation = () => {
    set((state) => ({
      pendingMutationCount: state.pendingMutationCount + 1,
    }));
  };

  const endMutation = () => {
    set((state) => ({
      pendingMutationCount: Math.max(0, state.pendingMutationCount - 1),
    }));
  };

  const runMutation = async <T>(operation: () => Promise<T>) => {
    beginMutation();

    try {
      return await operation();
    } finally {
      endMutation();
    }
  };

  const loadItineraryBundle = async (itineraryId: string, force = false) => {
    const state = get();
    const currentStatus = state.itineraryBundleStatus[itineraryId];

    if (!force && currentStatus === 'loaded') {
      return;
    }

    const existingRequest = itineraryBundleRequests.get(itineraryId);
    if (existingRequest) {
      return existingRequest;
    }

    set((currentState) => ({
      itineraryBundleStatus: {
        ...currentState.itineraryBundleStatus,
        [itineraryId]: 'loading',
      },
    }));

    const request = (async () => {
      try {
        const session = await requireSession();
        const [itineraryDto, members, events] = await Promise.all([
          travelApi.getItinerary(session.accessToken, itineraryId),
          travelApi.listItineraryMembers(session.accessToken, itineraryId),
          travelApi.listEvents(session.accessToken, itineraryId),
        ]);

        const itinerary = travelApi.mapItinerary(itineraryDto, members);

        set((currentState) => ({
          users: mergeUsers(currentState.users, members.map(memberToUser)),
          itineraries: upsertItinerary(currentState.itineraries, itinerary),
          itineraryMembers: {
            ...currentState.itineraryMembers,
            [itineraryId]: members,
          },
          itineraryBundleStatus: {
            ...currentState.itineraryBundleStatus,
            [itineraryId]: 'loaded',
          },
          itineraryShareCodes: currentState.itineraryShareCodes[itineraryId]
            ? {
                ...currentState.itineraryShareCodes,
                [itineraryId]: {
                  ...currentState.itineraryShareCodes[itineraryId],
                  version: itinerary.version,
                },
              }
            : currentState.itineraryShareCodes,
          events: [...currentState.events.filter((event) => event.itineraryId !== itineraryId), ...events],
          accessToken: session.accessToken,
          error: null,
        }));
      } catch (error) {
        set((currentState) => ({
          itineraryBundleStatus: {
            ...currentState.itineraryBundleStatus,
            [itineraryId]: 'error',
          },
        }));
        throw error;
      } finally {
        itineraryBundleRequests.delete(itineraryId);
      }
    })();

    itineraryBundleRequests.set(itineraryId, request);
    return request;
  };

  return ({
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

    try {
      const session = await restoreSession();
      if (!session) {
        set({
          ...emptyDataState,
          isBootstrapping: false,
          isReady: true,
          error: null,
        });
        return;
      }

      const shell = await fetchWorkspaceShell(session.accessToken);
      set(buildAuthenticatedState(session, shell));
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
      const shell = await fetchWorkspaceShell(session.accessToken);
      set(buildAuthenticatedState(session, shell));
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

      const shell = await fetchWorkspaceShell(session.accessToken);
      set(buildAuthenticatedState(session, shell));
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

  forgotPassword: async (input) => {
    set({ isBootstrapping: true, error: null });

    try {
      const response = await travelApi.forgotPassword({
        email: input.email,
      });

      set({
        isBootstrapping: false,
        isReady: true,
        error: null,
      });

      return response;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to start password reset.';
      set({
        isBootstrapping: false,
        isReady: true,
        error: message,
      });
      throw error;
    }
  },

  resetPassword: async (input) => {
    set({ isBootstrapping: true, error: null });

    try {
      await travelApi.resetPassword(input);
      set({
        isBootstrapping: false,
        isReady: true,
        error: null,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to reset password.';
      set({
        isBootstrapping: false,
        isReady: true,
        error: message,
      });
      throw error;
    }
  },

  changePassword: async (input) => {
    return runMutation(async () => {
      const session = await requireSession();

      try {
        await travelApi.changePassword(session.accessToken, input);
        set({
          accessToken: session.accessToken,
          error: null,
        });
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Unable to change password.';
        set({ error: message });
        throw error;
      }
    });
  },

  logout: async () => {
    try {
      await travelApi.logout();
    } catch {
      // no-op
    }

    authSessionCache.clear();
    itineraryBundleRequests.clear();

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
    const shell = await fetchWorkspaceShell(session.accessToken);

    set((state) => mergeWorkspaceShellIntoState(state, shell, session.accessToken));
  },

  ensureItineraryBundle: async (itineraryId) => {
    await loadItineraryBundle(itineraryId);
  },

  refreshItineraryBundle: async (itineraryId) => {
    await loadItineraryBundle(itineraryId, true);
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
    }));
  },

  loadNotifications: async () => {
    const session = await requireSession();
    const notifications = await travelApi.listNotifications(session.accessToken);

    set((state) => ({
      notifications: mergeNotifications(state.notifications, notifications),
      accessToken: session.accessToken,
    }));
  },

  markNotificationRead: async (notificationId) => {
    const existingNotification = get().notifications.find((notification) => notification.id === notificationId);
    if (!existingNotification || existingNotification.readAt) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      const notification = await travelApi.markNotificationRead(session.accessToken, notificationId);

      set((state) => ({
        notifications: mergeNotifications(
          state.notifications.filter((entry) => entry.id !== notificationId),
          [notification],
        ),
        accessToken: session.accessToken,
      }));
    });
  },

  markAllNotificationsRead: async () => {
    const unreadNotifications = get().notifications.filter((notification) => !notification.readAt);
    if (unreadNotifications.length === 0) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      await travelApi.markAllNotificationsRead(session.accessToken);
      const readAt = new Date().toISOString();

      set((state) => ({
        notifications: state.notifications.map((notification) =>
          notification.readAt ? notification : { ...notification, readAt },
        ),
        accessToken: session.accessToken,
      }));
    });
  },

  deleteNotification: async (notificationId) => {
    const existingNotification = get().notifications.find((notification) => notification.id === notificationId);
    if (!existingNotification) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      await travelApi.deleteNotification(session.accessToken, notificationId);

      set((state) => ({
        notifications: state.notifications.filter((notification) => notification.id !== notificationId),
        accessToken: session.accessToken,
        error: null,
      }));
    });
  },

  addRealtimeNotification: (notification) => {
    set((state) => ({
      notifications: mergeNotifications(state.notifications, [notification]),
    }));
  },

  loadItineraryShareCode: async (itineraryId) => {
    const session = await requireSession();
    const shareCode = await travelApi.getItineraryShareCode(session.accessToken, itineraryId);

    set((state) => ({
      itineraries: updateItineraryVersion(state.itineraries, itineraryId, shareCode.version),
      itineraryShareCodes: {
        ...state.itineraryShareCodes,
        [itineraryId]: shareCode,
      },
      accessToken: session.accessToken,
      error: null,
    }));

    return shareCode;
  },

  rotateItineraryShareCode: async (itineraryId) => {
    const state = get();
    const itinerary = state.itineraries.find((entry) => entry.id === itineraryId);
    if (!itinerary) {
      throw new ApiError('Itinerary not found.', 404);
    }

    return runMutation(async () => {
      const version = state.itineraryShareCodes[itineraryId]?.version ?? itinerary.version;
      const session = await requireSession();
      const shareCode = await travelApi.rotateItineraryShareCode(session.accessToken, itineraryId, version);

      set((currentState) => ({
        itineraryShareCodes: {
          ...currentState.itineraryShareCodes,
          [itineraryId]: shareCode,
        },
        itineraries: updateItineraryVersion(currentState.itineraries, itineraryId, shareCode.version),
        accessToken: session.accessToken,
        error: null,
      }));

      return shareCode;
    });
  },

  createItinerary: async (input) => {
    return runMutation(async () => {
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
        itineraryBundleStatus: {
          ...state.itineraryBundleStatus,
          [itinerary.id]: 'loaded',
        },
        accessToken: session.accessToken,
        currentUserId: currentUser.id,
        error: null,
      }));

      return itinerary.id;
    });
  },

  updateItinerary: async (itineraryId, input) => {
    const state = get();
    const itinerary = state.itineraries.find((entry) => entry.id === itineraryId);
    if (!itinerary) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      const updatedDto = await travelApi.updateItinerary(session.accessToken, itineraryId, input, itinerary.version);
      const members = state.itineraryMembers[itineraryId] ?? [];

      set((currentState) => ({
        itineraries: upsertItinerary(currentState.itineraries, travelApi.mapItinerary(updatedDto, members)),
        itineraryShareCodes: currentState.itineraryShareCodes[itineraryId]
          ? {
              ...currentState.itineraryShareCodes,
              [itineraryId]: {
                ...currentState.itineraryShareCodes[itineraryId],
                version: updatedDto.version,
              },
            }
          : currentState.itineraryShareCodes,
        accessToken: session.accessToken,
        error: null,
      }));
    });
  },

  joinItineraryByCode: async (code) => {
    return runMutation(async () => {
      const session = await requireSession();
      const itineraryDto = await travelApi.joinItineraryByCode(session.accessToken, code.trim());
      await get().refreshAll();
      return itineraryDto.id;
    });
  },

  removeItineraryMember: async (itineraryId, userId) => {
    const state = get();
    const itinerary = state.itineraries.find((entry) => entry.id === itineraryId);
    if (!itinerary) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      const replacement = await travelApi.removeItineraryMember(session.accessToken, itineraryId, userId, itinerary.version);
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
        itineraryBundleStatus: {
          ...currentState.itineraryBundleStatus,
          [itineraryId]: 'loaded',
        },
        itineraryShareCodes: currentState.itineraryShareCodes[itineraryId]
          ? {
              ...currentState.itineraryShareCodes,
              [itineraryId]: {
                ...currentState.itineraryShareCodes[itineraryId],
                version: refreshedItinerary.version,
              },
            }
          : currentState.itineraryShareCodes,
        accessToken: session.accessToken,
        error: null,
      }));
    });
  },

  createEvent: async (itineraryId, input) => {
    return runMutation(async () => {
      const session = await requireSession();
      const event = await travelApi.createEvent(session.accessToken, itineraryId, input);

      set((state) => ({
        events: upsertEvent(state.events, event),
        accessToken: session.accessToken,
        error: null,
      }));

      return event.id;
    });
  },

  updateEvent: async (eventId, input) => {
    const state = get();
    const currentEvent = state.events.find((event) => event.id === eventId);
    if (!currentEvent) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      const event = await travelApi.updateEvent(session.accessToken, eventId, input, currentEvent.version);

      set((currentState) => ({
        events: upsertEvent(currentState.events, event),
        eventHistory: {
          ...currentState.eventHistory,
          [eventId]: [],
        },
        accessToken: session.accessToken,
        error: null,
      }));
    });
  },

  deleteEvent: async (eventId) => {
    const state = get();
    const currentEvent = state.events.find((event) => event.id === eventId);
    if (!currentEvent) {
      return;
    }

    await runMutation(async () => {
      const session = await requireSession();
      await travelApi.deleteEvent(session.accessToken, eventId, currentEvent.version);

      set((currentState) => {
        const nextHistory = { ...currentState.eventHistory };
        delete nextHistory[eventId];

        return {
          events: currentState.events.filter((event) => event.id !== eventId),
          eventHistory: nextHistory,
          accessToken: session.accessToken,
          error: null,
        };
      });
    });
  },

  rescheduleEvent: async (eventId, startDateTime, endDateTime) => {
    const currentEvent = get().events.find((event) => event.id === eventId);
    if (!currentEvent) {
      return;
    }

    const nextRange = currentEvent.isAllDay
      ? buildAllDayDateTimeRange(startDateTime, endDateTime)
      : {
          startDateTime,
          endDateTime,
        };

    await get().updateEvent(eventId, {
      title: currentEvent.title,
      description: currentEvent.description,
      remarks: currentEvent.remarks,
      category: currentEvent.category,
      color: currentEvent.color,
      isAllDay: currentEvent.isAllDay,
      startDateTime: nextRange.startDateTime,
      endDateTime: nextRange.endDateTime,
      timezone: currentEvent.timezone,
      location: currentEvent.location,
      locationAddress: currentEvent.locationAddress,
      locationLat: currentEvent.locationLat,
      locationLng: currentEvent.locationLng,
      cost: currentEvent.cost,
      currencyCode: currentEvent.currencyCode,
      links: currentEvent.links.map((link) => ({
        description: link.description,
        url: link.url,
      })),
    });
  },
  });
});
