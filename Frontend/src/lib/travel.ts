import { dayjs } from './date';
import type { ItineraryEvent } from '../types/event';
import type { User } from '../types/user';

export const getUserMap = (users: User[]) =>
  users.reduce<Record<string, User>>((accumulator, user) => {
    accumulator[user.id] = user;
    return accumulator;
  }, {});

export const getUpcomingEvents = (events: ItineraryEvent[]) =>
  [...events].sort((a, b) => dayjs(a.startDateTime).valueOf() - dayjs(b.startDateTime).valueOf());

export const getItineraryDuration = (startDate: string, endDate: string) =>
  dayjs(endDate).diff(dayjs(startDate), 'day') + 1;
