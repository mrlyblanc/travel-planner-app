import { CalendarDays, Compass, Globe2, MapPinned } from 'lucide-react';
import { Avatar, Box, Button, Divider, List, ListItemButton, ListItemText, Stack, Typography } from '@mui/material';
import { Link as RouterLink, useLocation } from 'react-router-dom';
import { useTravelStore } from '../../app/store/useTravelStore';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { useMemo } from 'react';
import { formatDateRange } from '../../lib/date';
import { useTheme } from '@mui/material/styles';

interface SidebarContentProps {
  onNavigate?: () => void;
}

export const SidebarContent = ({ onNavigate }: SidebarContentProps) => {
  const theme = useTheme();
  const location = useLocation();
  const currentUser = useCurrentUser();
  const rawItineraries = useTravelStore((state) => state.itineraries);
  const itineraries = useMemo(
    () => [...rawItineraries].sort((left, right) => left.startDate.localeCompare(right.startDate)),
    [rawItineraries],
  );

  return (
    <Stack sx={{ height: '100%' }}>
      <Stack spacing={1.5} sx={{ p: 2.5 }}>
        <Stack direction="row" spacing={1.5}>
          <Box
            sx={{
              display: 'grid',
              placeItems: 'center',
              width: 44,
              height: 44,
              borderRadius: 3,
              bgcolor: 'primary.main',
              color: 'white',
            }}
          >
            <Compass size={22} />
          </Box>
          <Box>
            <Typography variant="h6">Trip Board</Typography>
            <Typography color="text.secondary" variant="body2">
              Collaborative itineraries
            </Typography>
          </Box>
        </Stack>

        <Button component={RouterLink} onClick={onNavigate} to="/itineraries" variant="contained">
          All itineraries
        </Button>
      </Stack>

      <Divider />

      <Box sx={{ px: 2.5, pt: 2, pb: 1 }}>
        <Typography color="text.secondary" variant="overline">
          Upcoming trips
        </Typography>
      </Box>

      <List sx={{ px: 1.5, py: 0, flexGrow: 1, overflowY: 'auto' }}>
        {itineraries.map((itinerary) => {
          const selected = location.pathname === `/itineraries/${itinerary.id}`;

          return (
            <ListItemButton
              key={itinerary.id}
              component={RouterLink}
              onClick={onNavigate}
              selected={selected}
              sx={{
                mb: 0.8,
                borderRadius: theme.app.radius.md,
                alignItems: 'flex-start',
                py: 1.5,
                border: `1px solid ${selected ? theme.app.selection.border : 'transparent'}`,
                bgcolor: selected ? theme.app.selection.bg : 'transparent',
                boxShadow: selected ? `inset 3px 0 0 ${theme.app.selection.accent}` : 'none',
                '&.Mui-selected': {
                  bgcolor: theme.app.selection.bg,
                },
                '&.Mui-selected:hover': {
                  bgcolor: theme.app.selection.hoverBg,
                },
              }}
              to={`/itineraries/${itinerary.id}`}
            >
              <ListItemText
                primary={itinerary.title}
                primaryTypographyProps={{ fontWeight: 600, mb: 0.5 }}
                secondary={
                  <Stack spacing={0.4}>
                    <Stack alignItems="center" direction="row" spacing={0.8}>
                      <MapPinned size={14} />
                      <Typography color="text.secondary" variant="caption">
                        {itinerary.destination}
                      </Typography>
                    </Stack>
                    <Stack alignItems="center" direction="row" spacing={0.8}>
                      <CalendarDays size={14} />
                      <Typography color="text.secondary" variant="caption">
                        {formatDateRange(itinerary.startDate, itinerary.endDate)}
                      </Typography>
                    </Stack>
                  </Stack>
                }
              />
            </ListItemButton>
          );
        })}
      </List>

      <Divider />

      {currentUser ? (
        <Stack direction="row" spacing={1.5} sx={{ p: 2.5 }}>
          <Avatar sx={{ bgcolor: 'rgba(43, 110, 220, 0.14)', color: 'primary.main' }}>{currentUser.avatar}</Avatar>
          <Box>
            <Typography fontWeight={600}>{currentUser.name}</Typography>
            <Stack alignItems="center" direction="row" spacing={0.8}>
              <Globe2 size={14} />
              <Typography color="text.secondary" variant="body2">
                Backend-authenticated traveler
              </Typography>
            </Stack>
          </Box>
        </Stack>
      ) : null}
    </Stack>
  );
};
