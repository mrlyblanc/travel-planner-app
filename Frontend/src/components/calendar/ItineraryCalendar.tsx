import interactionPlugin, { type DateClickArg, type EventResizeDoneArg } from '@fullcalendar/interaction';
import type { DatesSetArg, EventClickArg, EventDropArg, EventInput } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import FullCalendar from '@fullcalendar/react';
import timeGridPlugin from '@fullcalendar/timegrid';
import { Box, Typography } from '@mui/material';
import type { RefObject } from 'react';
import { dayjs, formatMonthLabel } from '../../lib/date';
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
  onSelectSlot: (selection: { start: string; end: string }) => void;
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

    return {
      id: event.id,
      title: event.title,
      start: event.startDateTime,
      end: event.endDateTime,
      display: 'block',
      backgroundColor: fillColor,
      borderColor: fillColor,
      textColor: getEventTextColor(fillColor),
      extendedProps: {
        sourceEvent: event,
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
        allDaySlot={false}
        dateClick={(arg: DateClickArg) => {
          if (!canManage || arg.dayEl.closest('.fc-event')) {
            return;
          }

          const start = dayjs(arg.date);
          const end = arg.allDay ? start.add(1, 'day') : start.add(1, 'hour');

          onSelectSlot({
            start: arg.allDay ? start.format('YYYY-MM-DD') : start.format(),
            end: arg.allDay ? end.format('YYYY-MM-DD') : end.format(),
          });
        }}
        editable={canManage}
        eventClick={(arg: EventClickArg) => onSelectEvent(arg.event.extendedProps.sourceEvent as ItineraryEvent)}
        eventDisplay="block"
        eventContent={(arg) => {
          const sourceEvent = arg.event.extendedProps.sourceEvent as ItineraryEvent;
          const fillColor = sourceEvent.color || getDefaultEventColor(sourceEvent.category);
          const textColor = getEventTextColor(fillColor);
          const timeRangeLabel = buildEventTimeRangeLabel(sourceEvent);

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
                sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', color: 'inherit', opacity: 0.92 }}
                variant="caption"
              >
                {timeRangeLabel}
              </Typography>
              <Typography
                sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', color: 'inherit', fontWeight: 600 }}
                variant="body2"
              >
                {arg.event.title}
              </Typography>
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
            onReschedule(arg.event.id, arg.event.start.toISOString(), arg.event.end.toISOString());
          }
        }}
        eventResize={(arg: EventResizeDoneArg) => {
          if (arg.event.start && arg.event.end) {
            onReschedule(arg.event.id, arg.event.start.toISOString(), arg.event.end.toISOString());
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

const buildEventTimeRangeLabel = (event: ItineraryEvent) => {
  const start = dayjs(event.startDateTime);
  const end = dayjs(event.endDateTime);

  if (!start.isValid() || !end.isValid()) {
    return '';
  }

  if (start.isSame(end, 'day')) {
    return `${start.format('h:mm A')} - ${end.format('h:mm A')}`;
  }

  if (start.isSame(end, 'year')) {
    return `${start.format('MMM D, h:mm A')} - ${end.format('MMM D, h:mm A')}`;
  }

  return `${start.format('MMM D, YYYY h:mm A')} - ${end.format('MMM D, YYYY h:mm A')}`;
};
