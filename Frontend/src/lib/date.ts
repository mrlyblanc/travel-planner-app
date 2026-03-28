import dayjs, { type ConfigType } from 'dayjs';
import localizedFormat from 'dayjs/plugin/localizedFormat';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(localizedFormat);
dayjs.extend(relativeTime);

export { dayjs };

export const formatDateRange = (start: ConfigType, end: ConfigType) => {
  const startDate = dayjs(start);
  const endDate = dayjs(end);

  if (startDate.isSame(endDate, 'month')) {
    return `${startDate.format('MMM D')} - ${endDate.format('D, YYYY')}`;
  }

  return `${startDate.format('MMM D')} - ${endDate.format('MMM D, YYYY')}`;
};

export const formatDateTime = (value: ConfigType) => dayjs(value).format('ddd, MMM D, YYYY h:mm A');

export const formatShortDate = (value: ConfigType) => dayjs(value).format('MMM D');

export const formatMonthLabel = (value: ConfigType) => dayjs(value).format('MMMM YYYY');

export const fromNow = (value: ConfigType) => dayjs(value).fromNow();

export const isWithinItinerary = (value: ConfigType, start: string, end: string) => {
  const date = dayjs(value);
  return (
    (date.isAfter(dayjs(start).startOf('day')) || date.isSame(dayjs(start).startOf('day'))) &&
    (date.isBefore(dayjs(end).endOf('day')) || date.isSame(dayjs(end).endOf('day')))
  );
};
