import { ArrowRight, CalendarRange, Coins, MapPinned } from 'lucide-react';
import { Button, Card, CardContent, Chip, Divider, Stack, Typography } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { formatDateRange } from '../../lib/date';
import type { Itinerary } from '../../types/itinerary';
import type { User } from '../../types/user';
import { UserAvatarGroup } from '../user/UserAvatarGroup';

interface ItineraryCardProps {
  itinerary: Itinerary;
  collaborators: User[];
  collaboratorCount: number;
  eventCountLabel: string;
  totalCostLabel: string;
}

export const ItineraryCard = ({ itinerary, collaborators, collaboratorCount, eventCountLabel, totalCostLabel }: ItineraryCardProps) => (
  <Card sx={{ height: '100%' }}>
    <CardContent sx={{ display: 'flex', flexDirection: 'column', gap: 2.2, height: '100%' }}>
      <Stack direction="row" justifyContent="space-between" spacing={2}>
        <Chip
          icon={<MapPinned size={14} />}
          label={itinerary.destination}
          sx={{ bgcolor: 'rgba(43, 110, 220, 0.08)', color: 'primary.dark' }}
        />
        {collaborators.length > 0 ? <UserAvatarGroup max={4} size={32} users={collaborators} /> : null}
      </Stack>

      <Stack spacing={1}>
        <Typography variant="h6">{itinerary.title}</Typography>
        <Typography color="text.secondary" variant="body2">
          {itinerary.description}
        </Typography>
      </Stack>

      <Divider />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <Chip
          icon={<CalendarRange size={14} />}
          label={formatDateRange(itinerary.startDate, itinerary.endDate)}
          variant="outlined"
        />
        <Chip icon={<Coins size={14} />} label={totalCostLabel} variant="outlined" />
        <Chip label={eventCountLabel} variant="outlined" />
      </Stack>

      <Stack direction="row" justifyContent="space-between" mt="auto" pt={1}>
        <Typography color="text.secondary" variant="body2">
          {collaboratorCount === 0
            ? 'Just you for now'
            : `Shared with ${collaboratorCount} traveler${collaboratorCount === 1 ? '' : 's'}`}
        </Typography>
        <Button component={RouterLink} endIcon={<ArrowRight size={16} />} to={`/itineraries/${itinerary.id}`}>
          Open trip
        </Button>
      </Stack>
    </CardContent>
  </Card>
);
