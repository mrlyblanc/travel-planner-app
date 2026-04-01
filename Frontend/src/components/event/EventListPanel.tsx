import { ListChecks, Search } from 'lucide-react';
import {
  alpha,
  Card,
  CardContent,
  InputAdornment,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  TextField,
  Typography,
  useTheme,
} from '@mui/material';
import { useMemo, useState } from 'react';
import { formatCurrencyAmount } from '../../lib/currency';
import { formatEventSchedule } from '../../lib/date';
import type { ItineraryEvent } from '../../types/event';
import type { User } from '../../types/user';
import { EmptyState } from '../common/EmptyState';
import { EventCategoryChip } from './EventCategoryChip';

interface EventListPanelProps {
  events: ItineraryEvent[];
  usersMap: Record<string, User>;
  selectedEventId?: string | null;
  onSelectEvent: (event: ItineraryEvent) => void;
}

export const EventListPanel = ({ events, usersMap, selectedEventId, onSelectEvent }: EventListPanelProps) => {
  const theme = useTheme();
  const [query, setQuery] = useState('');

  const filteredEvents = useMemo(() => {
    const loweredQuery = query.toLowerCase().trim();
    if (!loweredQuery) {
      return events;
    }

    return events.filter((event) =>
      [
        event.title,
        event.location,
        event.description,
        event.remarks,
        ...event.links.map((link) => `${link.description} ${link.url}`),
      ].some((value) => value.toLowerCase().includes(loweredQuery)),
    );
  }, [events, query]);

  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Stack spacing={2}>
          <Stack direction="row" justifyContent="space-between" spacing={2}>
            <BoxLabel subtitle={`${filteredEvents.length} matching events`} title="Event list" />
            <ListChecks size={18} />
          </Stack>

          <TextField
            fullWidth
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search by event title, hotel, restaurant, landmark, or neighborhood"
            size="small"
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <Search size={16} />
                  </InputAdornment>
                ),
              },
            }}
            value={query}
          />

          {filteredEvents.length === 0 ? (
            <EmptyState
              description="Search by stop name, venue, hotel, airport, or neighborhood, or add a new event from the itinerary toolbar."
              icon={<Search size={28} />}
              title="No matching events"
            />
          ) : (
            <List disablePadding sx={{ maxHeight: 520, overflowY: 'auto' }}>
              {filteredEvents.map((event) => {
                const selected = selectedEventId === event.id;

                return (
                  <ListItemButton
                    key={event.id}
                    onClick={() => onSelectEvent(event)}
                    selected={selected}
                    sx={{
                      mb: 1,
                      borderRadius: theme.app.radius.md,
                      alignItems: 'flex-start',
                      border: `1px solid ${selected ? theme.app.selection.border : alpha(theme.palette.divider, 0.7)}`,
                      bgcolor: selected ? theme.app.selection.bg : 'transparent',
                      boxShadow: selected ? `inset 3px 0 0 ${theme.app.selection.accent}` : 'none',
                      '&.Mui-selected': {
                        bgcolor: theme.app.selection.bg,
                      },
                      '&.Mui-selected:hover': {
                        bgcolor: theme.app.selection.hoverBg,
                      },
                    }}
                  >
                    <ListItemText
                      primary={
                        <Stack direction="row" justifyContent="space-between" spacing={1.5}>
                          <Typography fontWeight={600} variant="body2">
                            {event.title}
                          </Typography>
                          <EventCategoryChip category={event.category} color={event.color} />
                        </Stack>
                      }
                      secondary={
                        <Stack mt={1} spacing={0.6}>
                          <Typography color="text.secondary" variant="caption">
                            {formatEventSchedule(event)}
                          </Typography>
                          <Typography color="text.secondary" variant="caption">
                            {event.location}
                          </Typography>
                          {event.remarks ? (
                            <Typography color="text.secondary" variant="caption">
                              {event.remarks}
                            </Typography>
                          ) : null}
                          <Typography color="text.secondary" variant="caption">
                            {formatCurrencyAmount(event.cost, event.currencyCode)} • Updated by {usersMap[event.updatedBy]?.name ?? 'Unknown'}
                          </Typography>
                        </Stack>
                      }
                    />
                  </ListItemButton>
                );
              })}
            </List>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

const BoxLabel = ({ title, subtitle }: { title: string; subtitle: string }) => (
  <Stack spacing={0.5}>
    <Typography variant="h6">{title}</Typography>
    <Typography color="text.secondary" variant="body2">
      {subtitle}
    </Typography>
  </Stack>
);
