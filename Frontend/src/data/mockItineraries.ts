import type { Itinerary } from '../types/itinerary';

const createdAt = '2026-02-10T09:00:00+08:00';
const updatedAt = '2026-03-20T12:15:00+08:00';

export const mockItineraries: Itinerary[] = [
  {
    id: 'itinerary-tokyo',
    version: 'mock-itinerary-tokyo-v1',
    title: 'Tokyo Sakura Sprint',
    description:
      'A fast-paced cherry blossom week with food stops, design shopping, and a Mt. Fuji day trip.',
    destination: 'Tokyo, Japan',
    startDate: '2026-04-10',
    endDate: '2026-04-16',
    createdBy: 'user-ava',
    memberIds: ['user-ava', 'user-luca', 'user-mina'],
    memberCount: 3,
    createdAt,
    updatedAt,
  },
  {
    id: 'itinerary-seoul',
    version: 'mock-itinerary-seoul-v1',
    title: 'Seoul Food & Culture Loop',
    description:
      'Neighborhood hopping across palaces, cafes, and a full day in Hongdae with collaborative planning.',
    destination: 'Seoul, South Korea',
    startDate: '2026-05-04',
    endDate: '2026-05-09',
    createdBy: 'user-mina',
    memberIds: ['user-ava', 'user-mina', 'user-sofia', 'user-noah'],
    memberCount: 4,
    createdAt,
    updatedAt,
  },
  {
    id: 'itinerary-singapore',
    version: 'mock-itinerary-singapore-v1',
    title: 'Singapore Family Stopover',
    description:
      'A tidy four-day city break focused on comfort, attractions, and easy transport connections.',
    destination: 'Singapore',
    startDate: '2026-06-18',
    endDate: '2026-06-22',
    createdBy: 'user-luca',
    memberIds: ['user-ava', 'user-luca', 'user-ethan'],
    memberCount: 3,
    createdAt,
    updatedAt,
  },
  {
    id: 'itinerary-bali',
    version: 'mock-itinerary-bali-v1',
    title: 'Bali Recharge Week',
    description:
      'Ubud mornings, beach sunsets, and enough unstructured time to keep the trip restorative.',
    destination: 'Bali, Indonesia',
    startDate: '2026-07-08',
    endDate: '2026-07-14',
    createdBy: 'user-sofia',
    memberIds: ['user-ava', 'user-sofia', 'user-ethan', 'user-noah'],
    memberCount: 4,
    createdAt,
    updatedAt,
  },
  {
    id: 'itinerary-manila',
    version: 'mock-itinerary-manila-v1',
    title: 'Manila Long Weekend',
    description:
      'An urban local itinerary mixing heritage stops, hotel downtime, and a flexible business meetup.',
    destination: 'Manila, Philippines',
    startDate: '2026-08-20',
    endDate: '2026-08-23',
    createdBy: 'user-ava',
    memberIds: ['user-ava', 'user-luca', 'user-ethan', 'user-noah'],
    memberCount: 4,
    createdAt,
    updatedAt,
  },
];
