import { dayjs } from './date';
import { roundCurrency } from './utils';
import type { ItineraryEvent } from '../types/event';
import type { User } from '../types/user';

export const getUserMap = (users: User[]) =>
  users.reduce<Record<string, User>>((accumulator, user) => {
    accumulator[user.id] = user;
    return accumulator;
  }, {});

export const getTotalCost = (events: ItineraryEvent[]) =>
  roundCurrency(events.reduce((sum, event) => sum + event.cost, 0));

export const getCostByCategory = (events: ItineraryEvent[]) =>
  events.reduce<Record<string, number>>((accumulator, event) => {
    accumulator[event.category] = roundCurrency((accumulator[event.category] ?? 0) + event.cost);
    return accumulator;
  }, {});

export const getUpcomingEvents = (events: ItineraryEvent[]) =>
  [...events].sort((a, b) => dayjs(a.startDateTime).valueOf() - dayjs(b.startDateTime).valueOf());

export const getItineraryDuration = (startDate: string, endDate: string) =>
  dayjs(endDate).diff(dayjs(startDate), 'day') + 1;
