import { alpha } from '@mui/material/styles';
import { Box, ButtonBase, Stack, Typography, useTheme } from '@mui/material';
import { dayjs, formatCompactTime } from '../../lib/date';
import { getDefaultEventColor, getEventTextColor } from '../../lib/events';
import type { ItineraryEvent } from '../../types/event';

interface ItineraryScheduleViewProps {
  events: ItineraryEvent[];
  activeEventId?: string | null;
  onSelectEvent: (event: ItineraryEvent, anchorEl: HTMLElement) => void;
}

interface ScheduleEventOccurrence {
  id: string;
  event: ItineraryEvent;
  day: string;
}

const scheduleDateColumnWidth = { xs: '92px', md: '160px' } as const;

export const ItineraryScheduleView = ({
  events,
  activeEventId,
  onSelectEvent,
}: ItineraryScheduleViewProps) => {
  const theme = useTheme();
  const today = dayjs().startOf('day');
  const groups = buildScheduleGroups(events, today);

  return (
    <Stack divider={<Box sx={{ borderBottom: `1px solid ${alpha(theme.palette.divider, 0.72)}` }} />} spacing={0}>
      {groups.map((group) => {
        const isToday = group.date.isSame(dayjs(), 'day');

        return (
          <Box
            key={group.key}
            sx={{
              px: { xs: 1.2, sm: 2.2 },
              py: { xs: 1.25, sm: 1.7 },
            }}
          >
            <Stack spacing={0.9}>
              {group.events.length === 0 ? (
                <Box
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: {
                      xs: `${scheduleDateColumnWidth.xs} minmax(0, 1fr)`,
                      md: `${scheduleDateColumnWidth.md} minmax(0, 1fr)`,
                    },
                    columnGap: { xs: 1, md: 2.4 },
                    alignItems: 'start',
                  }}
                >
                  <DateCell date={group.date} isToday={isToday} />
                  <Box
                    sx={{
                      px: 1.25,
                      py: 1.1,
                    }}
                  >
                    <Typography color="text.secondary" variant="body2">
                      No events scheduled today.
                    </Typography>
                  </Box>
                </Box>
              ) : null}

              {group.events.map((occurrence, occurrenceIndex) => {
                const showDateCell = occurrenceIndex === 0;
                const selected = occurrence.event.id === activeEventId;
                const color = occurrence.event.color || getDefaultEventColor(occurrence.event.category);
                const textColor = getEventTextColor(color);

                return (
                  <Box
                    key={`${occurrence.id}-${occurrence.day}`}
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: {
                        xs: `${scheduleDateColumnWidth.xs} minmax(0, 1fr)`,
                        md: `${scheduleDateColumnWidth.md} minmax(0, 1fr)`,
                      },
                      columnGap: { xs: 1, md: 2.4 },
                      alignItems: 'start',
                    }}
                  >
                    {showDateCell ? (
                      <DateCell date={group.date} isToday={isToday} />
                    ) : (
                      <Box sx={{ minHeight: 40, alignSelf: 'center' }} />
                    )}

                    <ButtonBase
                      onClick={(clickEvent) => onSelectEvent(occurrence.event, clickEvent.currentTarget)}
                      sx={{
                        width: '100%',
                        textAlign: 'left',
                        borderRadius: theme.app.radius.md,
                        px: { xs: 0.9, sm: 1.25 },
                        py: { xs: 0.85, sm: 1 },
                        justifyContent: 'flex-start',
                        border: `1px solid ${selected ? theme.app.selection.border : 'transparent'}`,
                        backgroundColor: selected ? theme.app.selection.bg : 'transparent',
                        boxShadow: selected ? `inset 3px 0 0 ${theme.app.selection.accent}` : 'none',
                        transition: 'background-color 160ms ease, box-shadow 160ms ease, transform 160ms ease',
                        '&:hover': {
                          backgroundColor: selected ? theme.app.selection.hoverBg : theme.palette.action.hover,
                          transform: 'translateY(-1px)',
                        },
                      }}
                    >
                      <Stack
                        direction={{ xs: 'column', sm: 'row' }}
                        spacing={{ xs: 0.7, sm: 1.5 }}
                        sx={{ width: '100%', alignItems: { sm: 'center' } }}
                      >
                        <Stack
                          direction="row"
                          justifyContent={{ xs: 'space-between', sm: 'flex-start' }}
                          spacing={{ xs: 1.2, sm: 1 }}
                          sx={{ alignItems: 'center', minWidth: { sm: 128 }, width: { xs: '100%', sm: 'auto' } }}
                        >
                          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0, flex: 1 }}>
                            <Box
                              sx={{
                                width: 11,
                                height: 11,
                                borderRadius: '50%',
                                backgroundColor: color,
                                boxShadow: `0 0 0 3px ${alpha(color, 0.16)}`,
                                flexShrink: 0,
                              }}
                            />
                            <Typography color="text.secondary" fontWeight={600} noWrap variant="caption">
                              {buildOccurrenceLabel(occurrence.event, group.date)}
                            </Typography>
                          </Stack>

                          <Box
                            sx={{
                              display: { xs: 'inline-flex', sm: 'none' },
                              alignSelf: 'center',
                              px: 1,
                              py: 0.45,
                              borderRadius: theme.app.radius.md,
                              backgroundColor: alpha(color, theme.palette.mode === 'light' ? 0.16 : 0.24),
                              color: textColor,
                              maxWidth: '48%',
                            }}
                          >
                            <Typography fontWeight={700} noWrap variant="caption">
                              {occurrence.event.category}
                            </Typography>
                          </Box>
                        </Stack>

                        <Box sx={{ minWidth: 0, flex: 1 }}>
                          <Typography fontWeight={600} variant="body2">
                            {occurrence.event.title}
                          </Typography>
                          {occurrence.event.location ? (
                            <Typography color="text.secondary" sx={{ mt: 0.2 }} variant="caption">
                              {occurrence.event.location}
                            </Typography>
                          ) : null}
                        </Box>

                        <Box
                          sx={{
                            alignSelf: { xs: 'flex-start', sm: 'center' },
                            display: { xs: 'none', sm: 'inline-flex' },
                            px: 1,
                            py: 0.45,
                            borderRadius: theme.app.radius.md,
                            backgroundColor: alpha(color, theme.palette.mode === 'light' ? 0.16 : 0.24),
                            color: textColor,
                          }}
                        >
                          <Typography fontWeight={700} variant="caption">
                            {occurrence.event.category}
                          </Typography>
                        </Box>
                      </Stack>
                    </ButtonBase>
                  </Box>
                );
              })}
            </Stack>
          </Box>
        );
      })}
    </Stack>
  );
};

const DateCell = ({
  date,
  isToday,
}: {
  date: ReturnType<typeof dayjs>;
  isToday: boolean;
}) => (
  <Box
    sx={{
      minWidth: 0,
      display: 'flex',
      alignItems: 'center',
      gap: 1.25,
      minHeight: 40,
      width: '100%',
      alignSelf: 'center',
    }}
  >
    <Typography
      color={isToday ? '#ffffff' : 'text.primary'}
      component="span"
      fontSize="1.1rem"
      fontWeight={700}
      lineHeight={1}
      sx={
        isToday
          ? {
              width: 40,
              height: 40,
              borderRadius: '50%',
              display: 'grid',
              placeItems: 'center',
              backgroundColor: 'primary.main',
              boxShadow: '0 8px 20px rgba(75, 141, 255, 0.28)',
              flexShrink: 0,
            }
          : {
              width: 40,
              height: 40,
              display: 'grid',
              placeItems: 'center',
              flexShrink: 0,
            }
      }
    >
      {date.format('D')}
    </Typography>
    <Typography
      color="primary.main"
      component="span"
      fontWeight={700}
      sx={{ letterSpacing: '0.12em', lineHeight: 1.15 }}
      variant="caption"
    >
      {date.format('MMM, ddd').toUpperCase()}
    </Typography>
  </Box>
);

const buildScheduleGroups = (events: ItineraryEvent[], today: ReturnType<typeof dayjs>) => {
  const groups = new Map<string, ScheduleEventOccurrence[]>();

  events.forEach((event) => {
    const start = dayjs(event.startDateTime);
    const end = dayjs(event.endDateTime);

    if (!start.isValid() || !end.isValid()) {
      return;
    }

    const effectiveStart = start.startOf('day');
    const effectiveEnd = end.startOf('day');

    for (let cursor = effectiveStart.startOf('day'); cursor.isSame(effectiveEnd, 'day') || cursor.isBefore(effectiveEnd, 'day'); cursor = cursor.add(1, 'day')) {
      const key = cursor.format('YYYY-MM-DD');
      const existing = groups.get(key) ?? [];

      existing.push({
        id: event.id,
        event,
        day: key,
      });

      groups.set(key, existing);
    }
  });

  const sortedGroups = Array.from(groups.entries())
    .sort(([left], [right]) => left.localeCompare(right))
    .map(([key, occurrences]) => ({
      key,
      date: dayjs(key),
      events: occurrences.sort((left, right) => compareOccurrences(left.event, right.event)),
    }));

  const todayKey = today.format('YYYY-MM-DD');
  const todayGroupIndex = sortedGroups.findIndex((group) => group.key === todayKey);

  if (todayGroupIndex === -1) {
    return [
      {
        key: todayKey,
        date: today,
        events: [],
      },
      ...sortedGroups,
    ];
  }

  const [todayGroup] = sortedGroups.splice(todayGroupIndex, 1);
  return [todayGroup, ...sortedGroups];
};

const compareOccurrences = (left: ItineraryEvent, right: ItineraryEvent) => {
  if (left.isAllDay !== right.isAllDay) {
    return left.isAllDay ? -1 : 1;
  }

  const startDifference = dayjs(left.startDateTime).valueOf() - dayjs(right.startDateTime).valueOf();
  if (startDifference !== 0) {
    return startDifference;
  }

  return left.title.localeCompare(right.title);
};

const buildOccurrenceLabel = (event: ItineraryEvent, occurrenceDate: ReturnType<typeof dayjs>) => {
  if (event.isAllDay) {
    return 'All day';
  }

  const start = dayjs(event.startDateTime);
  const end = dayjs(event.endDateTime);

  if (!start.isValid() || !end.isValid()) {
    return 'Scheduled';
  }

  if (start.isSame(occurrenceDate, 'day') && end.isSame(occurrenceDate, 'day')) {
    return `${formatCompactTime(start)} - ${formatCompactTime(end)}`;
  }

  if (occurrenceDate.isSame(start, 'day')) {
    return `Starts ${formatCompactTime(start)}`;
  }

  if (occurrenceDate.isSame(end, 'day')) {
    return `Until ${formatCompactTime(end)}`;
  }

  return 'Continues';
};
