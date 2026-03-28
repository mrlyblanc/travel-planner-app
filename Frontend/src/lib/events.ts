import type { EventCategory } from '../types/event';

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

export const timezoneOptions = [
  'Asia/Manila',
  'Asia/Tokyo',
  'Asia/Seoul',
  'Asia/Singapore',
  'Asia/Makassar',
  'UTC',
];

export const eventColorPalette = [
  '#2b6edc',
  '#00897b',
  '#2e7d32',
  '#ef6c00',
  '#c2185b',
  '#7b1fa2',
  '#5c6bc0',
  '#455a64',
  '#d84315',
  '#00838f',
];

export const getDefaultEventColor = (category: EventCategory) => eventCategoryMeta[category].color;

export const getEventTextColor = (backgroundColor: string) => {
  const normalized = backgroundColor.replace('#', '');
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
