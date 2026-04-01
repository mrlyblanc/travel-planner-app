import interactionPlugin, { type DateClickArg, type EventResizeDoneArg } from '@fullcalendar/interaction';
import type { DatesSetArg, EventClickArg, EventDropArg, EventInput } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import FullCalendar from '@fullcalendar/react';
import timeGridPlugin from '@fullcalendar/timegrid';
import { Box, Typography } from '@mui/material';
import type { RefObject } from 'react';
import {
  DEFAULT_TIMED_SLOT_MINUTES,
  dayjs,
  formatCompactTime,
  formatInclusiveAllDayEndDate,
  formatMonthLabel,
  isLongTimedEvent,
  toCalendarAllDayEndDate,
} from '../../lib/date';
import { getDefaultEventColor, getEventTextColor } from '../../lib/events';
import type { ItineraryEvent } from '../../types/event';

export type CalendarView = 'timeGridDay' | 'timeGridThreeDay' | 'timeGridWeek' | 'dayGridMonth';

interface ItineraryCalendarProps {
  calendarRef: RefObject<FullCalendar | null>;
  events: ItineraryEvent[];
  initialDate: string;
  activeView: CalendarView;
  canManage: boolean;
  onViewChange: (view: CalendarView) => void;
  onRangeChange: (label: string) => void;
  onSelectEvent: (event: ItineraryEvent) => void;
  onSelectSlot: (selection: { start: string; end: string; allDay: boolean }) => void;
  onReschedule: (eventId: string, start: string, end: string) => void;
}

export const ItineraryCalendar = ({
  calendarRef,
  events,
  initialDate,
  activeView,
  canManage,
  onViewChange,
  onRangeChange,
  onSelectEvent,
  onSelectSlot,
  onReschedule,
}: ItineraryCalendarProps) => {
  const calendarEvents: EventInput[] = events.map((event) => {
    const fillColor = event.color || getDefaultEventColor(event.category);
    const renderInAllDayRow = shouldRenderInAllDayRow(event);

    return {
      id: event.id,
      title: event.title,
      start: renderInAllDayRow ? dayjs(event.startDateTime).format('YYYY-MM-DD') : event.startDateTime,
      end: renderInAllDayRow ? toCalendarAllDayEndDate(event.endDateTime) : event.endDateTime,
      allDay: renderInAllDayRow,
      display: 'block',
      backgroundColor: fillColor,
      borderColor: fillColor,
      textColor: getEventTextColor(fillColor),
      extendedProps: {
        sourceEvent: event,
        renderInAllDayRow,
      },
    };
  });

  return (
    <Box
      sx={{
        '& .fc-toolbar': { display: 'none' },
        '& .fc-daygrid-day-number, & .fc-col-header-cell-cushion': {
          color: 'text.primary',
          fontWeight: 600,
        },
        '& .fc-timegrid-slot-label-cushion, & .fc-timegrid-axis-cushion': {
          color: 'text.secondary',
        },
      }}
    >
      <FullCalendar
        ref={calendarRef}
        allDaySlot
        allDayText="AD"
        dateClick={(arg: DateClickArg) => {
          if (!canManage || arg.dayEl.closest('.fc-event')) {
            return;
          }

          const start = dayjs(arg.date);
          const end = arg.allDay ? start.add(1, 'day') : start.add(DEFAULT_TIMED_SLOT_MINUTES, 'minute');

          onSelectSlot({
            start: arg.allDay ? start.format('YYYY-MM-DD') : start.format(),
            end: arg.allDay ? end.format('YYYY-MM-DD') : end.format(),
            allDay: arg.allDay,
          });
        }}
        editable={canManage}
        eventClick={(arg: EventClickArg) => onSelectEvent(arg.event.extendedProps.sourceEvent as ItineraryEvent)}
        eventDisplay="block"
        eventContent={(arg) => {
          const sourceEvent = arg.event.extendedProps.sourceEvent as ItineraryEvent;
          const fillColor = sourceEvent.color || getDefaultEventColor(sourceEvent.category);
          const textColor = getEventTextColor(fillColor);
          const display = buildCalendarEventDisplay(sourceEvent);

          return (
            <Box
              sx={{
                color: textColor,
                borderRadius: 2.5,
                px: 1.1,
                py: 0.6,
                minHeight: '100%',
              }}
            >
              <Typography
                sx={{
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  whiteSpace: 'nowrap',
                  color: 'inherit',
                  fontWeight: 600,
                }}
                variant="body2"
              >
                {display.primary}
              </Typography>
              {display.secondary ? (
                <Typography
                  sx={{
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    whiteSpace: 'nowrap',
                    color: 'inherit',
                    opacity: 0.92,
                    lineHeight: 1.18,
                    mt: 0.2,
                  }}
                  variant="caption"
                >
                  {display.secondary}
                </Typography>
              ) : null}
            </Box>
          );
        }}
        events={calendarEvents}
        headerToolbar={false}
        height="auto"
        initialDate={initialDate}
        initialView={activeView}
        nowIndicator
        plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin]}
        selectable={canManage}
        select={(selection) =>
          onSelectSlot({
            start: selection.startStr,
            end: selection.endStr,
            allDay: selection.allDay,
          })
        }
        slotDuration="00:30:00"
        timeZone="local"
        viewDidMount={(arg) => onViewChange(arg.view.type as CalendarView)}
        views={{
          timeGridThreeDay: {
            type: 'timeGrid',
            duration: { days: 3 },
            buttonText: '3 days',
          },
        }}
        datesSet={(arg: DatesSetArg) => {
          onViewChange(arg.view.type as CalendarView);
          onRangeChange(buildRangeLabel(arg));
        }}
        eventDrop={(arg: EventDropArg) => {
          if (arg.event.start && arg.event.end) {
            const sourceEvent = arg.event.extendedProps.sourceEvent as ItineraryEvent;
            const nextRange = shouldRenderInAllDayRow(sourceEvent)
              ? buildAllDayRowRescheduleRange(sourceEvent, arg.event.start, arg.event.end)
              : {
                  start: arg.event.start.toISOString(),
                  end: arg.event.end.toISOString(),
                };

            onReschedule(
              arg.event.id,
              nextRange.start,
              nextRange.end,
            );
          }
        }}
        eventResize={(arg: EventResizeDoneArg) => {
          if (arg.event.start && arg.event.end) {
            const sourceEvent = arg.event.extendedProps.sourceEvent as ItineraryEvent;
            const nextRange = shouldRenderInAllDayRow(sourceEvent)
              ? buildAllDayRowRescheduleRange(sourceEvent, arg.event.start, arg.event.end)
              : {
                  start: arg.event.start.toISOString(),
                  end: arg.event.end.toISOString(),
                };

            onReschedule(
              arg.event.id,
              nextRange.start,
              nextRange.end,
            );
          }
        }}
      />
    </Box>
  );
};

const buildRangeLabel = ({ start, end, view }: DatesSetArg) => {
  const startDate = dayjs(start);
  const inclusiveEnd = dayjs(end).subtract(1, 'day');

  if (view.type === 'dayGridMonth') {
    return formatMonthLabel(view.currentStart);
  }

  if (startDate.isSame(inclusiveEnd, 'day')) {
    return startDate.format('MMMM D, YYYY');
  }

  return `${startDate.format('MMM D')} - ${inclusiveEnd.format('MMM D, YYYY')}`;
};

const shouldRenderInAllDayRow = (event: ItineraryEvent) => event.isAllDay || isLongTimedEvent(event);

const buildCalendarEventDisplay = (event: ItineraryEvent) => {
  const start = dayjs(event.startDateTime);
  const end = dayjs(event.endDateTime);

  if (!start.isValid() || !end.isValid()) {
    return {
      primary: event.title,
      secondary: null as string | null,
    };
  }

  if (event.isAllDay) {
    return {
      primary: event.title,
      secondary: null,
    };
  }

  if (isLongTimedEvent(event)) {
    return {
      primary: `${formatCompactTime(start)}, ${event.title}`,
      secondary: null,
    };
  }

  return {
    primary: event.title,
    secondary: `${formatCompactTime(start)} - ${formatCompactTime(end)}`,
  };
};

const buildAllDayRowRescheduleRange = (event: ItineraryEvent, start: Date, end: Date | null) => {
  if (event.isAllDay) {
    return {
      start: dayjs(start).format('YYYY-MM-DD'),
      end: formatInclusiveAllDayEndDate(end, start),
    };
  }

  const originalStart = dayjs(event.startDateTime);
  const originalEnd = dayjs(event.endDateTime);
  const nextStartDate = dayjs(start);
  const nextEndDate = end ? dayjs(end).subtract(1, 'day') : nextStartDate;

  return {
    start: nextStartDate
      .hour(originalStart.hour())
      .minute(originalStart.minute())
      .second(originalStart.second())
      .millisecond(0)
      .format(),
    end: nextEndDate
      .hour(originalEnd.hour())
      .minute(originalEnd.minute())
      .second(originalEnd.second())
      .millisecond(0)
      .format(),
  };
};
