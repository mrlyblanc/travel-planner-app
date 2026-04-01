import { dayjs } from './date';
import type { EventCategory, ItineraryEvent } from '../types/event';

export const eventCategoryOptions: EventCategory[] = [
  'Hotel',
  'Restaurant',
  'Landmark',
  'Travel',
  'Activity',
  'Shopping',
  'Transport',
  'Other',
];

export const eventCategoryMeta: Record<
  EventCategory,
  {
    label: string;
    color: string;
    softColor: string;
  }
> = {
  Hotel: { label: 'Hotel', color: '#1976d2', softColor: '#e7f1ff' },
  Restaurant: { label: 'Restaurant', color: '#ef6c00', softColor: '#fff1e5' },
  Landmark: { label: 'Landmark', color: '#7b1fa2', softColor: '#f6ebff' },
  Travel: { label: 'Travel', color: '#00897b', softColor: '#e4f8f5' },
  Activity: { label: 'Activity', color: '#2e7d32', softColor: '#e7f5ea' },
  Shopping: { label: 'Shopping', color: '#c2185b', softColor: '#ffe7f1' },
  Transport: { label: 'Transport', color: '#455a64', softColor: '#ecf1f3' },
  Other: { label: 'Other', color: '#5c6bc0', softColor: '#eef0ff' },
};

const fallbackTimezoneOptions = [
  'Asia/Manila',
  'Asia/Tokyo',
  'Asia/Seoul',
  'Asia/Singapore',
  'Asia/Makassar',
  'UTC',
];

export const timezoneOptions = Array.from(
  new Set([
    ...fallbackTimezoneOptions,
    ...(typeof Intl.supportedValuesOf === 'function'
      ? (Intl.supportedValuesOf('timeZone') as string[])
      : []),
  ]),
).sort((left, right) => left.localeCompare(right));

export const formatTimezoneLabel = (timezone: string) =>
  timezone
    .split('/')
    .map((segment) => segment.replaceAll('_', ' '))
    .join('/');

export const formatTimezoneCityLabel = (timezone: string) => {
  const segments = timezone
    .split('/')
    .map((segment) => segment.trim())
    .filter(Boolean);

  if (segments.length === 0) {
    return '';
  }

  return segments[segments.length - 1].replaceAll('_', ' ');
};

const timezoneDisplayLabelCache = new Map<string, string>();

const readTimezoneNamePart = (timezone: string, timeZoneName: 'long' | 'longOffset') =>
  new Intl.DateTimeFormat('en-US', {
    timeZone: timezone,
    timeZoneName,
  })
    .formatToParts(new Date())
    .find((part) => part.type === 'timeZoneName')
    ?.value;

export const formatTimezoneOffsetLabel = (timezone: string) => {
  const normalizedTimezone = timezone.trim();
  if (!normalizedTimezone) {
    return '';
  }

  const offsetLabel = readTimezoneNamePart(normalizedTimezone, 'longOffset') ?? 'GMT';
  return offsetLabel.replace(/:00$/, '');
};

export const formatTimezoneDisplayLabel = (timezone: string) => {
  const normalizedTimezone = timezone.trim();
  if (!normalizedTimezone) {
    return '';
  }

  const cachedLabel = timezoneDisplayLabelCache.get(normalizedTimezone);
  if (cachedLabel) {
    return cachedLabel;
  }

  const offsetLabel = formatTimezoneOffsetLabel(normalizedTimezone) || 'GMT';
  const longName = readTimezoneNamePart(normalizedTimezone, 'long');
  const fallbackName = formatTimezoneLabel(normalizedTimezone);
  const displayName = longName && longName !== offsetLabel ? longName : fallbackName;
  const cityLabel = formatTimezoneCityLabel(normalizedTimezone);
  const shouldAppendCity =
    cityLabel &&
    !displayName.toLowerCase().includes(cityLabel.toLowerCase()) &&
    displayName.toLowerCase() !== cityLabel.toLowerCase();
  const label = `(${offsetLabel}) ${displayName}${shouldAppendCity ? ` • ${cityLabel}` : ''}`;

  timezoneDisplayLabelCache.set(normalizedTimezone, label);
  return label;
};

export const normalizeEventColor = (value: string | null | undefined) => value?.trim().toUpperCase() ?? '';

const extraEventColors = ['#2B6EDC', '#D84315', '#00838F', '#F97316', '#0F766E', '#16A34A'];

export const eventColorPalette = Array.from(
  new Map(
    [...Object.values(eventCategoryMeta).map((meta) => meta.color), ...extraEventColors].map((color) => [
      normalizeEventColor(color),
      normalizeEventColor(color),
    ]),
  ).values(),
);

export const getEventColorOptions = (selectedColor?: string | null) => {
  const normalizedSelectedColor = normalizeEventColor(selectedColor);
  if (!normalizedSelectedColor || eventColorPalette.includes(normalizedSelectedColor)) {
    return eventColorPalette;
  }

  return [...eventColorPalette, normalizedSelectedColor];
};

export const getDefaultEventColor = (category: EventCategory) => normalizeEventColor(eventCategoryMeta[category].color);

export const getEventTextColor = (backgroundColor: string) => {
  const normalized = normalizeEventColor(backgroundColor).replace('#', '');
  const fullHex =
    normalized.length === 3
      ? normalized
          .split('')
          .map((char) => `${char}${char}`)
          .join('')
      : normalized;

  const red = Number.parseInt(fullHex.slice(0, 2), 16);
  const green = Number.parseInt(fullHex.slice(2, 4), 16);
  const blue = Number.parseInt(fullHex.slice(4, 6), 16);

  const luminance = (0.299 * red + 0.587 * green + 0.114 * blue) / 255;

  return luminance > 0.6 ? '#16304b' : '#ffffff';
};

export const eventsOverlap = (
  candidateStartDateTime: string,
  candidateEndDateTime: string,
  existingStartDateTime: string,
  existingEndDateTime: string,
) =>
  dayjs(candidateStartDateTime).isBefore(dayjs(existingEndDateTime)) &&
  dayjs(candidateEndDateTime).isAfter(dayjs(existingStartDateTime));

export const findEventConflicts = ({
  events,
  startDateTime,
  endDateTime,
  excludeEventId,
}: {
  events: ItineraryEvent[];
  startDateTime: string;
  endDateTime: string;
  excludeEventId?: string | null;
}) =>
  events
    .filter((event) => event.id !== excludeEventId)
    .filter((event) => eventsOverlap(startDateTime, endDateTime, event.startDateTime, event.endDateTime))
    .sort((left, right) => left.startDateTime.localeCompare(right.startDateTime));
