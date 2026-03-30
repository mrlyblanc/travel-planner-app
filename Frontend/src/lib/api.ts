import { getDefaultEventColor, normalizeEventColor } from './events';
import type { EventAuditLog, EventAuditSnapshot, EventCategory, ItineraryEvent } from '../types/event';
import type { Itinerary, ItineraryMember } from '../types/itinerary';
import type { User } from '../types/user';

const API_VERSION = import.meta.env.VITE_API_VERSION?.trim() || '1.0';
const ENV_API_BASE_URL = import.meta.env.VITE_API_BASE_URL?.trim();
const LOCAL_API_BASE_URLS = [
  'http://localhost:5290',
  'https://localhost:7051',
  'http://localhost:5070',
  'https://localhost:7291',
];
const DEFAULT_LOGIN_EMAIL = import.meta.env.VITE_DEV_LOGIN_EMAIL?.trim() ?? '';
const DEFAULT_LOGIN_PASSWORD = import.meta.env.VITE_DEV_LOGIN_PASSWORD ?? '';
const LEGACY_AUTH_STORAGE_KEY = 'travel-planner-auth-session';

const normalizeBaseUrl = (value: string) => value.replace(/\/$/, '');
const isLocalUrl = (value: string) => {
  try {
    const parsed = new URL(value);
    return ['localhost', '127.0.0.1'].includes(parsed.hostname);
  } catch {
    return false;
  }
};

const isLocalBrowserHost = () => {
  if (typeof window === 'undefined') {
    return false;
  }

  return ['localhost', '127.0.0.1'].includes(window.location.hostname);
};

const CONFIGURED_API_BASE_URL = ENV_API_BASE_URL ? normalizeBaseUrl(ENV_API_BASE_URL) : null;
const SHOULD_INCLUDE_LOCAL_FALLBACKS = isLocalBrowserHost() && (!CONFIGURED_API_BASE_URL || isLocalUrl(CONFIGURED_API_BASE_URL));

const API_BASE_URL_CANDIDATES = [
  ...(CONFIGURED_API_BASE_URL ? [CONFIGURED_API_BASE_URL] : []),
  ...(SHOULD_INCLUDE_LOCAL_FALLBACKS ? LOCAL_API_BASE_URLS.map(normalizeBaseUrl) : []),
].filter((value, index, values) => values.indexOf(value) === index);

let resolvedApiBaseUrl = API_BASE_URL_CANDIDATES[0] ?? '';

interface ApiProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

interface ApiResponseEnvelope<T> {
  data: T;
  etag?: string;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  avatar?: string;
}

interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

interface RefreshTokenRequest {
  refreshToken: string;
}

interface AuthResponseDto {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
  user: UserResponseDto;
}

interface UserLookupResponseDto {
  id: string;
  name: string;
  email: string;
  avatar: string;
}

interface UserResponseDto extends UserLookupResponseDto {
  version: string;
  createdAtUtc: string;
}

interface ItineraryResponseDto {
  id: string;
  version: string;
  title: string;
  description?: string | null;
  destination: string;
  startDate: string;
  endDate: string;
  createdById: string;
  memberCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

interface ItineraryMemberResponseDto {
  itineraryId: string;
  userId: string;
  name: string;
  email: string;
  avatar: string;
  addedByUserId: string;
  addedAtUtc: string;
}

interface EventResponseDto {
  id: string;
  version: string;
  itineraryId: string;
  title: string;
  description?: string | null;
  category: EventCategory;
  color?: string | null;
  startDateTime: string;
  endDateTime: string;
  timezone: string;
  location?: string | null;
  locationAddress?: string | null;
  locationLat?: number | null;
  locationLng?: number | null;
  cost?: number | null;
  createdById: string;
  updatedById: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

interface EventAuditSnapshotResponseDto {
  id: string;
  itineraryId: string;
  title: string;
  description?: string | null;
  category: EventCategory;
  color?: string | null;
  startDateTime: string;
  endDateTime: string;
  timezone: string;
  location?: string | null;
  locationAddress?: string | null;
  locationLat?: number | null;
  locationLng?: number | null;
  cost?: number | null;
  updatedById: string;
  updatedAtUtc: string;
}

interface EventAuditLogResponseDto {
  id: string;
  eventId: string;
  itineraryId: string;
  action: 'Created' | 'Updated' | 'Deleted';
  summary: string;
  snapshot: EventAuditSnapshotResponseDto;
  changedByUserId: string;
  changedAtUtc: string;
}

export interface AuthSession {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  refreshTokenExpiresAt: string;
  user: User;
}

interface LegacyAuthSession extends AuthSession {
  refreshToken?: string | null;
}

export interface ItineraryInputDto {
  title: string;
  description: string;
  destination: string;
  startDate: string;
  endDate: string;
}

export interface EventInputDto {
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
}

export interface ItineraryRealtimeNotification {
  type: string;
  itineraryId: string;
  entityId: string;
  occurredAtUtc: string;
  payload?: unknown;
}

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

const apiUrl = (baseUrl: string, path: string) => `${baseUrl}/api${path}`;

const normalizeEtag = (etag: string | null | undefined) => etag?.trim().replace(/^W\//i, '').replace(/^"|"$/g, '') ?? null;
const normalizeUtcTimestamp = (value: string | null | undefined) => {
  if (!value) {
    return '';
  }

  const trimmedValue = value.trim();
  if (!trimmedValue) {
    return '';
  }

  if (!trimmedValue.includes('T')) {
    return trimmedValue;
  }

  return /(?:Z|[+\-]\d{2}:\d{2})$/i.test(trimmedValue) ? trimmedValue : `${trimmedValue}Z`;
};

export const toIfMatchHeader = (version: string) => `"${version}"`;

let inMemoryAuthSession: AuthSession | null = null;

const normalizeAuthSession = (session: AuthSession): AuthSession => ({
  ...session,
  expiresAt: normalizeUtcTimestamp(session.expiresAt),
  refreshTokenExpiresAt: normalizeUtcTimestamp(session.refreshTokenExpiresAt),
  user: {
    ...session.user,
    createdAt: normalizeUtcTimestamp(session.user.createdAt),
  },
});

const readAuthSession = (): AuthSession | null => {
  return inMemoryAuthSession ? normalizeAuthSession(inMemoryAuthSession) : null;
};

const clearLegacyAuthSession = () => {
  if (typeof window === 'undefined') {
    return;
  }

  window.localStorage.removeItem(LEGACY_AUTH_STORAGE_KEY);
};

const readLegacyAuthSession = (): LegacyAuthSession | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const raw = window.localStorage.getItem(LEGACY_AUTH_STORAGE_KEY);
  if (!raw) {
    return null;
  }

  try {
    const session = JSON.parse(raw) as LegacyAuthSession;
    return {
      ...normalizeAuthSession(session),
      refreshToken: session.refreshToken?.trim() || null,
    };
  } catch {
    clearLegacyAuthSession();
    return null;
  }
};

const writeAuthSession = (session: AuthSession | null) => {
  inMemoryAuthSession = session ? normalizeAuthSession(session) : null;
  clearLegacyAuthSession();
};

async function apiRequest<T>(
  path: string,
  options: {
    method?: string;
    token?: string | null;
    body?: unknown;
    ifMatch?: string | null;
  } = {},
): Promise<ApiResponseEnvelope<T>> {
  let response: Response | null = null;

  for (const baseUrl of [resolvedApiBaseUrl, ...API_BASE_URL_CANDIDATES.filter((candidate) => candidate !== resolvedApiBaseUrl)]) {
    try {
      response = await fetch(apiUrl(baseUrl, path), {
        method: options.method ?? 'GET',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'X-Api-Version': API_VERSION,
          ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
          ...(options.ifMatch ? { 'If-Match': options.ifMatch } : {}),
        },
        body: options.body === undefined ? undefined : JSON.stringify(options.body),
      });

      resolvedApiBaseUrl = baseUrl;
      break;
    } catch {
      // Try the next known local backend address.
    }
  }

  if (!response) {
    const configuredBase = CONFIGURED_API_BASE_URL;
    const triedBases = configuredBase ? [configuredBase] : API_BASE_URL_CANDIDATES;
    const triedBasesMessage = triedBases.length > 0
      ? `Tried ${triedBases.join(', ')}.`
      : 'No API base URL is configured for this build.';
    const deploymentHint = !configuredBase && !isLocalBrowserHost()
      ? ' Set VITE_API_BASE_URL to your deployed backend URL before building the frontend.'
      : '';

    throw new ApiError(
      `Unable to reach the backend API. ${triedBasesMessage} Make sure the backend is running and that VITE_API_BASE_URL points to the correct host and port.${deploymentHint}`,
      0,
    );
  }

  if (!response.ok) {
    let problem: ApiProblemDetails | null = null;

    try {
      problem = (await response.json()) as ApiProblemDetails;
    } catch {
      problem = null;
    }

    throw new ApiError(problem?.detail ?? problem?.title ?? `Request failed with status ${response.status}.`, response.status);
  }

  if (response.status === 204) {
    return {
      data: undefined as T,
      etag: normalizeEtag(response.headers.get('ETag')) ?? undefined,
    };
  }

  const data = (await response.json()) as T;

  return {
    data,
    etag: normalizeEtag(response.headers.get('ETag')) ?? undefined,
  };
}

const mapUserLookup = (user: UserLookupResponseDto): User => ({
  id: user.id,
  name: user.name,
  email: user.email,
  avatar: user.avatar,
});

const mapUserResponse = (user: UserResponseDto): User => ({
  id: user.id,
  name: user.name,
  email: user.email,
  avatar: user.avatar,
  version: user.version,
  createdAt: normalizeUtcTimestamp(user.createdAtUtc),
});

const mapMemberResponse = (member: ItineraryMemberResponseDto): ItineraryMember => ({
  itineraryId: member.itineraryId,
  userId: member.userId,
  name: member.name,
  email: member.email,
  avatar: member.avatar,
  addedByUserId: member.addedByUserId,
  addedAt: member.addedAtUtc,
});

const mapItineraryResponse = (itinerary: ItineraryResponseDto, members: ItineraryMember[]): Itinerary => ({
  id: itinerary.id,
  version: itinerary.version,
  title: itinerary.title,
  description: itinerary.description ?? '',
  destination: itinerary.destination,
  startDate: itinerary.startDate,
  endDate: itinerary.endDate,
  createdBy: itinerary.createdById,
  memberIds: members.map((member) => member.userId),
  memberCount: itinerary.memberCount,
  createdAt: normalizeUtcTimestamp(itinerary.createdAtUtc),
  updatedAt: normalizeUtcTimestamp(itinerary.updatedAtUtc),
});

const mapEventResponse = (event: EventResponseDto): ItineraryEvent => ({
  id: event.id,
  version: event.version,
  itineraryId: event.itineraryId,
  title: event.title,
  description: event.description ?? '',
  category: event.category,
  color: normalizeEventColor(event.color ?? getDefaultEventColor(event.category)),
  startDateTime: event.startDateTime,
  endDateTime: event.endDateTime,
  timezone: event.timezone,
  location: event.location ?? '',
  locationAddress: event.locationAddress ?? '',
  locationLat: event.locationLat ?? null,
  locationLng: event.locationLng ?? null,
  cost: event.cost ?? 0,
  createdBy: event.createdById,
  updatedBy: event.updatedById,
  createdAt: normalizeUtcTimestamp(event.createdAtUtc),
  updatedAt: normalizeUtcTimestamp(event.updatedAtUtc),
});

const mapAuditSnapshot = (snapshot: EventAuditSnapshotResponseDto): EventAuditSnapshot => ({
  id: snapshot.id,
  itineraryId: snapshot.itineraryId,
  title: snapshot.title,
  description: snapshot.description ?? null,
  category: snapshot.category,
  color: snapshot.color ?? null,
  startDateTime: snapshot.startDateTime,
  endDateTime: snapshot.endDateTime,
  timezone: snapshot.timezone,
  location: snapshot.location ?? null,
  locationAddress: snapshot.locationAddress ?? null,
  locationLat: snapshot.locationLat ?? null,
  locationLng: snapshot.locationLng ?? null,
  cost: snapshot.cost ?? null,
  updatedBy: snapshot.updatedById,
  updatedAt: normalizeUtcTimestamp(snapshot.updatedAtUtc),
});

const mapAuditLog = (auditLog: EventAuditLogResponseDto): EventAuditLog => ({
  id: auditLog.id,
  eventId: auditLog.eventId,
  itineraryId: auditLog.itineraryId,
  action: auditLog.action,
  summary: auditLog.summary,
  snapshot: mapAuditSnapshot(auditLog.snapshot),
  changedBy: auditLog.changedByUserId,
  changedAt: normalizeUtcTimestamp(auditLog.changedAtUtc),
});

const toItineraryPayload = (input: ItineraryInputDto) => ({
  title: input.title,
  description: input.description,
  destination: input.destination,
  startDate: input.startDate,
  endDate: input.endDate,
});

const toEventPayload = (input: EventInputDto) => ({
  title: input.title,
  description: input.description,
  category: input.category,
  color: input.color,
  startDateTime: input.startDateTime,
  endDateTime: input.endDateTime,
  timezone: input.timezone,
  location: input.location,
  locationAddress: input.locationAddress,
  locationLat: input.locationLat,
  locationLng: input.locationLng,
  cost: input.cost,
});

export const authSessionCache = {
  load: readAuthSession,
  save: writeAuthSession,
  clear: () => writeAuthSession(null),
  loadLegacy: readLegacyAuthSession,
  clearLegacy: clearLegacyAuthSession,
};

export const backendConfig = {
  apiBaseUrl: resolvedApiBaseUrl,
  apiBaseUrlCandidates: API_BASE_URL_CANDIDATES,
  apiVersion: API_VERSION,
  defaultLoginEmail: DEFAULT_LOGIN_EMAIL,
  defaultLoginPassword: DEFAULT_LOGIN_PASSWORD,
  hasSeededDevLogin: Boolean(DEFAULT_LOGIN_EMAIL && DEFAULT_LOGIN_PASSWORD),
};

export const getApiBaseUrl = () => resolvedApiBaseUrl;

export const travelApi = {
  async login(request: LoginRequest) {
    const response = await apiRequest<AuthResponseDto>('/auth/login', {
      method: 'POST',
      body: request,
    });

    const session: AuthSession = {
      accessToken: response.data.accessToken,
      tokenType: response.data.tokenType,
      expiresAt: normalizeUtcTimestamp(response.data.expiresAtUtc),
      refreshTokenExpiresAt: normalizeUtcTimestamp(response.data.refreshTokenExpiresAtUtc),
      user: mapUserResponse(response.data.user),
    };

    authSessionCache.save(session);
    return session;
  },

  async register(request: RegisterRequest) {
    const response = await apiRequest<UserResponseDto>('/users', {
      method: 'POST',
      body: request,
    });

    return mapUserResponse(response.data);
  },

  async refresh(refreshToken?: string) {
    const response = await apiRequest<AuthResponseDto>('/auth/refresh', {
      method: 'POST',
      body: refreshToken ? ({ refreshToken } satisfies RefreshTokenRequest) : undefined,
    });

    const session: AuthSession = {
      accessToken: response.data.accessToken,
      tokenType: response.data.tokenType,
      expiresAt: normalizeUtcTimestamp(response.data.expiresAtUtc),
      refreshTokenExpiresAt: normalizeUtcTimestamp(response.data.refreshTokenExpiresAtUtc),
      user: mapUserResponse(response.data.user),
    };

    authSessionCache.save(session);
    return session;
  },

  async logout() {
    await apiRequest<void>('/auth/logout', {
      method: 'POST',
    });
    authSessionCache.clear();
  },

  async changePassword(token: string, request: ChangePasswordRequest) {
    await apiRequest<void>('/auth/change-password', {
      method: 'POST',
      token,
      body: request,
    });
  },

  async getCurrentUser(token: string) {
    const response = await apiRequest<UserResponseDto>('/auth/me', {
      token,
    });

    return mapUserResponse(response.data);
  },

  async searchUsers(token: string, query: string, limit = 25) {
    const response = await apiRequest<UserLookupResponseDto[]>(
      `/users?query=${encodeURIComponent(query)}&limit=${limit}`,
      { token },
    );

    return response.data.map(mapUserLookup);
  },

  async listItineraries(token: string) {
    const response = await apiRequest<ItineraryResponseDto[]>('/itineraries', { token });
    return response.data;
  },

  async getItinerary(token: string, itineraryId: string) {
    const response = await apiRequest<ItineraryResponseDto>(`/itineraries/${itineraryId}`, { token });
    return response.data;
  },

  async createItinerary(token: string, input: ItineraryInputDto) {
    const response = await apiRequest<ItineraryResponseDto>('/itineraries', {
      method: 'POST',
      token,
      body: toItineraryPayload(input),
    });

    return response.data;
  },

  async updateItinerary(token: string, itineraryId: string, input: ItineraryInputDto, version: string) {
    const response = await apiRequest<ItineraryResponseDto>(`/itineraries/${itineraryId}`, {
      method: 'PUT',
      token,
      body: toItineraryPayload(input),
      ifMatch: toIfMatchHeader(version),
    });

    return response.data;
  },

  async listItineraryMembers(token: string, itineraryId: string) {
    const response = await apiRequest<ItineraryMemberResponseDto[]>(`/itineraries/${itineraryId}/members`, { token });
    return response.data.map(mapMemberResponse);
  },

  async replaceItineraryMembers(token: string, itineraryId: string, userIds: string[], version: string) {
    const response = await apiRequest<ItineraryMemberResponseDto[]>(`/itineraries/${itineraryId}/members`, {
      method: 'PUT',
      token,
      body: { userIds },
      ifMatch: toIfMatchHeader(version),
    });

    return {
      members: response.data.map(mapMemberResponse),
      version: response.etag ?? version,
    };
  },

  async removeItineraryMember(token: string, itineraryId: string, userId: string, version: string) {
    const response = await apiRequest<ItineraryMemberResponseDto[]>(`/itineraries/${itineraryId}/members/${userId}`, {
      method: 'DELETE',
      token,
      ifMatch: toIfMatchHeader(version),
    });

    return {
      members: response.data.map(mapMemberResponse),
      version: response.etag ?? version,
    };
  },

  async listEvents(token: string, itineraryId: string) {
    const response = await apiRequest<EventResponseDto[]>(`/itineraries/${itineraryId}/events`, { token });
    return response.data.map(mapEventResponse);
  },

  async getEvent(token: string, eventId: string) {
    const response = await apiRequest<EventResponseDto>(`/events/${eventId}`, { token });
    return mapEventResponse(response.data);
  },

  async createEvent(token: string, itineraryId: string, input: EventInputDto) {
    const response = await apiRequest<EventResponseDto>(`/itineraries/${itineraryId}/events`, {
      method: 'POST',
      token,
      body: toEventPayload(input),
    });

    return mapEventResponse(response.data);
  },

  async updateEvent(token: string, eventId: string, input: EventInputDto, version: string) {
    const response = await apiRequest<EventResponseDto>(`/events/${eventId}`, {
      method: 'PUT',
      token,
      body: toEventPayload(input),
      ifMatch: toIfMatchHeader(version),
    });

    return mapEventResponse(response.data);
  },

  async deleteEvent(token: string, eventId: string, version: string) {
    await apiRequest<void>(`/events/${eventId}`, {
      method: 'DELETE',
      token,
      ifMatch: toIfMatchHeader(version),
    });
  },

  async getEventHistory(token: string, eventId: string) {
    const response = await apiRequest<EventAuditLogResponseDto[]>(`/events/${eventId}/history`, { token });
    return response.data.map(mapAuditLog);
  },

  mapItinerary(itinerary: ItineraryResponseDto, members: ItineraryMember[]) {
    return mapItineraryResponse(itinerary, members);
  },
};
