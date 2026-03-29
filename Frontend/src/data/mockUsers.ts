import type { User } from '../types/user';

export const mockUsers: User[] = [
  {
    id: 'user-ava',
    name: 'Ava Santos',
    email: 'ava.santos@example.com',
    avatar: 'AS',
  },
  {
    id: 'user-luca',
    name: 'Luca Reyes',
    email: 'luca.reyes@example.com',
    avatar: 'LR',
  },
  {
    id: 'user-mina',
    name: 'Mina Park',
    email: 'mina.park@example.com',
    avatar: 'MP',
  },
  {
    id: 'user-ethan',
    name: 'Ethan Lim',
    email: 'ethan.lim@example.com',
    avatar: 'EL',
  },
  {
    id: 'user-sofia',
    name: 'Sofia Tan',
    email: 'sofia.tan@example.com',
    avatar: 'ST',
  },
  {
    id: 'user-noah',
    name: 'Noah Kim',
    email: 'noah.kim@example.com',
    avatar: 'NK',
  },
];

export const mockCurrentUserId = 'user-ava';
