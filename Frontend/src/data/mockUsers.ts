import type { User } from '../types/user';

export const mockUsers: User[] = [
  {
    id: 'user-ava',
    name: 'Ava Santos',
    email: 'ava.santos@globejet.com',
    avatar: 'AS',
  },
  {
    id: 'user-luca',
    name: 'Luca Reyes',
    email: 'luca.reyes@globejet.com',
    avatar: 'LR',
  },
  {
    id: 'user-mina',
    name: 'Mina Park',
    email: 'mina.park@globejet.com',
    avatar: 'MP',
  },
  {
    id: 'user-ethan',
    name: 'Ethan Lim',
    email: 'ethan.lim@globejet.com',
    avatar: 'EL',
  },
  {
    id: 'user-sofia',
    name: 'Sofia Tan',
    email: 'sofia.tan@globejet.com',
    avatar: 'ST',
  },
  {
    id: 'user-noah',
    name: 'Noah Kim',
    email: 'noah.kim@globejet.com',
    avatar: 'NK',
  },
];

export const mockCurrentUserId = 'user-ava';
