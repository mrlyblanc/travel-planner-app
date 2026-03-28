import { Coins, RefreshCw, Route, Users2 } from 'lucide-react';
import { alpha, Box, Button, Grid, Stack, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore, type ItineraryInput } from '../app/store/useTravelStore';
import { StatCard } from '../components/common/StatCard';
import { ItineraryCard } from '../components/itinerary/ItineraryCard';
import { ItineraryFormDialog } from '../components/itinerary/ItineraryFormDialog';
import { formatDateRange } from '../lib/date';
import { getTotalCost, getUserMap } from '../lib/travel';
import { currencyFormatter } from '../lib/utils';

export const ItineraryListPage = () => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const rawItineraries = useTravelStore((state) => state.itineraries);
  const users = useTravelStore((state) => state.users);
  const events = useTravelStore((state) => state.events);
  const createItinerary = useTravelStore((state) => state.createItinerary);
  const seedDemoData = useTravelStore((state) => state.seedDemoData);
  const [dialogOpen, setDialogOpen] = useState(false);
  const itineraries = useMemo(
    () => [...rawItineraries].sort((left, right) => left.startDate.localeCompare(right.startDate)),
    [rawItineraries],
  );
  const usersMap = useMemo(() => getUserMap(users), [users]);

  const totalCost = useMemo(() => getTotalCost(events), [events]);
  const nextTrip = itineraries[0];

  const handleCreateItinerary = (values: ItineraryInput) => {
    const itineraryId = createItinerary(values);
    setDialogOpen(false);
    showToast('Itinerary created');
    navigate(`/itineraries/${itineraryId}`);
  };

  return (
    <Stack spacing={3}>
      <HeroBanner>
        <HeroBannerContent direction={{ xs: 'column', lg: 'row' }} justifyContent="space-between" spacing={3}>
          <Box maxWidth={720}>
            <Typography variant="h3">Plan travel like a shared calendar.</Typography>
            <HeroSupportingCopy mt={1.5}>
              Seeded demo data, persistent localStorage, and a Google Calendar-inspired workflow for collaborative trip
              planning. Everything stays frontend-only and runnable right away.
            </HeroSupportingCopy>
            {nextTrip ? (
              <HeroNextTrip mt={2.5} variant="body2">
                Next trip: {nextTrip.title} • {formatDateRange(nextTrip.startDate, nextTrip.endDate)}
              </HeroNextTrip>
            ) : null}
          </Box>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <HeroGhostButton
              color="inherit"
              onClick={() => {
                seedDemoData();
                showToast('Demo data reset');
              }}
              startIcon={<RefreshCw size={16} />}
            >
              Reset demo data
            </HeroGhostButton>
            <HeroPrimaryButton onClick={() => setDialogOpen(true)} variant="contained">
              Create itinerary
            </HeroPrimaryButton>
          </Stack>
        </HeroBannerContent>
      </HeroBanner>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 4 }}>
          <StatCard helper="Seeded across five destinations" icon={<Route size={20} />} label="Trips" value={String(itineraries.length)} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <StatCard helper="Simulated collaborators available" icon={<Users2 size={20} />} label="Travelers" value={String(users.length)} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <StatCard helper="Combined sample itinerary spend" icon={<Coins size={20} />} label="Total planned cost" value={currencyFormatter.format(totalCost)} />
        </Grid>
      </Grid>

      <Box>
        <Typography variant="h5">Your itineraries</Typography>
        <Typography color="text.secondary" mt={0.8}>
          Each card shows members, date range, total cost, and event density so you can jump straight into planning.
        </Typography>
      </Box>

      <Grid container spacing={2.5}>
        {itineraries.map((itinerary) => {
          const itineraryEvents = events.filter((event) => event.itineraryId === itinerary.id);
          const members = itinerary.memberIds.map((memberId) => usersMap[memberId]).filter(Boolean);

          return (
            <Grid key={itinerary.id} size={{ xs: 12, md: 6, xl: 4 }}>
              <ItineraryCard
                eventCount={itineraryEvents.length}
                itinerary={itinerary}
                members={members}
                totalCost={getTotalCost(itineraryEvents)}
              />
            </Grid>
          );
        })}
      </Grid>

      <ItineraryFormDialog
        onClose={() => setDialogOpen(false)}
        onSubmit={handleCreateItinerary}
        open={dialogOpen}
        title="Create itinerary"
      />
    </Stack>
  );
};

const HeroBanner = styled(Box)(({ theme }) => ({
  padding: theme.spacing(3),
  borderRadius: theme.app.radius.md,
  border: `1px solid ${theme.app.surfaces.heroBorder}`,
  background: theme.app.surfaces.hero,
  color: '#ffffff',
  boxShadow: theme.app.surfaces.heroShadow,
  [theme.breakpoints.up('md')]: {
    padding: theme.spacing(4),
  },
}));

const HeroBannerContent = styled(Stack)(({ theme }) => ({
  [theme.breakpoints.up('lg')]: {
    alignItems: 'flex-end',
  },
}));

const HeroSupportingCopy = styled(Typography)({
  color: 'rgba(255,255,255,0.82)',
});

const HeroNextTrip = styled(Typography)({
  color: 'rgba(255,255,255,0.92)',
});

const HeroGhostButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.app.surfaces.overlayButton,
  color: '#ffffff',
  border: `1px solid ${theme.app.surfaces.overlayButtonBorder}`,
  borderRadius: theme.app.radius.sm,
}));

const HeroPrimaryButton = styled(Button)(({ theme }) => ({
  backgroundColor: '#ffffff',
  color: theme.palette.primary.main,
  borderRadius: theme.app.radius.sm,
  '&:hover': {
    backgroundColor: alpha('#ffffff', 0.92),
  },
}));
