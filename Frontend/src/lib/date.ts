import dayjs, { type ConfigType, type Dayjs } from 'dayjs';
import localizedFormat from 'dayjs/plugin/localizedFormat';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(localizedFormat);
dayjs.extend(relativeTime);

export { dayjs };

export const DEFAULT_TIMED_SLOT_MINUTES = 30;

export const combineDateAndTime = (dateValue: Dayjs | null, timeValue: Dayjs | null) => {
  if (!dateValue || !timeValue) {
    return null;
  }

  return dateValue.hour(timeValue.hour()).minute(timeValue.minute()).second(0).millisecond(0);
};

export const buildDefaultTime = (dateValue: Dayjs, hour: number, minute = 0) =>
  dateValue.hour(hour).minute(minute).second(0).millisecond(0);

export const buildCurrentFallbackTimeRange = (startDate: Dayjs, endDate?: Dayjs | null) => {
  const roundedNow = dayjs().startOf('hour');
  const roundedEnd = roundedNow.add(1, 'hour');
  const normalizedStartDate = startDate.startOf('day');
  let normalizedEndDate = (endDate ?? startDate).startOf('day');

  if (!roundedEnd.isSame(roundedNow, 'day') && normalizedEndDate.isSame(normalizedStartDate, 'day')) {
    normalizedEndDate = normalizedEndDate.add(1, 'day');
  }

  return {
    startTime: buildDefaultTime(normalizedStartDate, roundedNow.hour(), roundedNow.minute()),
    endTime: buildDefaultTime(normalizedEndDate, roundedEnd.hour(), roundedEnd.minute()),
    endDate: normalizedEndDate,
  };
};

export const buildAllDayDateTimeRange = (startDate: ConfigType, endDate: ConfigType) => ({
  startDateTime: dayjs(startDate).startOf('day').format(),
  endDateTime: dayjs(endDate).hour(23).minute(59).second(0).millisecond(0).format(),
});

export const formatLocalDateTimePayload = (value: ConfigType) => {
  const parsedValue = dayjs(value);
  return parsedValue.isValid() ? parsedValue.format('YYYY-MM-DDTHH:mm:ss') : String(value);
};

export const toCalendarAllDayEndDate = (endDateTime: ConfigType) =>
  dayjs(endDateTime).startOf('day').add(1, 'day').format('YYYY-MM-DD');

export const formatInclusiveAllDayEndDate = (exclusiveEnd: ConfigType | null, start: ConfigType) =>
  (exclusiveEnd ? dayjs(exclusiveEnd).subtract(1, 'day') : dayjs(start)).format('YYYY-MM-DD');

export const formatCompactTime = (value: ConfigType) => {
  const parsedValue = dayjs(value);
  return parsedValue.minute() === 0 ? parsedValue.format('ha').toLowerCase() : parsedValue.format('h:mma').toLowerCase();
};

export const isLongTimedEvent = ({
  startDateTime,
  endDateTime,
  isAllDay,
}: {
  startDateTime: ConfigType;
  endDateTime: ConfigType;
  isAllDay: boolean;
}) => {
  if (isAllDay) {
    return false;
  }

  const start = dayjs(startDateTime);
  const end = dayjs(endDateTime);

  if (!start.isValid() || !end.isValid()) {
    return false;
  }

  return end.diff(start, 'minute') > 24 * 60;
};

export const formatDateRange = (start: ConfigType, end: ConfigType) => {
  const startDate = dayjs(start);
  const endDate = dayjs(end);

  if (startDate.isSame(endDate, 'month')) {
    return `${startDate.format('MMM D')} - ${endDate.format('D, YYYY')}`;
  }

  return `${startDate.format('MMM D')} - ${endDate.format('MMM D, YYYY')}`;
};

export const formatDateTime = (value: ConfigType) => dayjs(value).format('ddd, MMM D, YYYY h:mm A');

export const formatEventSchedule = ({
  startDateTime,
  endDateTime,
  isAllDay,
}: {
  startDateTime: ConfigType;
  endDateTime: ConfigType;
  isAllDay: boolean;
}) => {
  const start = dayjs(startDateTime);
  const end = dayjs(endDateTime);

  if (!start.isValid() || !end.isValid()) {
    return '';
  }

  if (isAllDay) {
    if (start.isSame(end, 'day')) {
      return `${start.format('ddd, MMM D, YYYY')} • All day`;
    }

    if (start.isSame(end, 'month')) {
      return `${start.format('ddd, MMM D')} - ${end.format('ddd, MMM D, YYYY')} • All day`;
    }

    return `${start.format('ddd, MMM D, YYYY')} - ${end.format('ddd, MMM D, YYYY')} • All day`;
  }

  if (start.isSame(end, 'day')) {
    return `${start.format('ddd, MMM D, YYYY h:mm A')} - ${end.format('h:mm A')}`;
  }

  return `${start.format('ddd, MMM D, YYYY h:mm A')} - ${end.format('ddd, MMM D, YYYY h:mm A')}`;
};

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
