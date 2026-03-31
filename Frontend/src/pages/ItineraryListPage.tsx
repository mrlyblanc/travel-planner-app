import { Coins, Hash, RefreshCw, Route, Users2 } from 'lucide-react';
import { alpha, Box, Button, Grid, Stack, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore, type ItineraryInput } from '../app/store/useTravelStore';
import { StatCard } from '../components/common/StatCard';
import { ItineraryCard } from '../components/itinerary/ItineraryCard';
import { JoinItineraryDialog } from '../components/itinerary/JoinItineraryDialog';
import { formatCurrencySummary, getCostTotalsByCurrency } from '../lib/currency';
import { ItineraryFormDialog } from '../components/itinerary/ItineraryFormDialog';
import { formatDateRange } from '../lib/date';
import { getUserMap } from '../lib/travel';

export const ItineraryListPage = () => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const rawItineraries = useTravelStore((state) => state.itineraries);
  const users = useTravelStore((state) => state.users);
  const events = useTravelStore((state) => state.events);
  const currentUserId = useTravelStore((state) => state.currentUserId);
  const createItinerary = useTravelStore((state) => state.createItinerary);
  const joinItineraryByCode = useTravelStore((state) => state.joinItineraryByCode);
  const refreshAll = useTravelStore((state) => state.refreshAll);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [joinDialogOpen, setJoinDialogOpen] = useState(false);
  const itineraries = useMemo(
    () => [...rawItineraries].sort((left, right) => left.startDate.localeCompare(right.startDate)),
    [rawItineraries],
  );
  const usersMap = useMemo(() => getUserMap(users), [users]);

  const totalCostLabel = useMemo(
    () =>
      formatCurrencySummary(getCostTotalsByCurrency(events), {
        emptyLabel: 'No costs yet',
        maxVisible: 3,
      }),
    [events],
  );
  const nextTrip = itineraries[0];

  const handleCreateItinerary = async (values: ItineraryInput) => {
    const itineraryId = await createItinerary(values);
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
              Keep flights, hotels, meals, and day plans in one shared calendar so everyone can see what is happening,
              where to be, and what the trip is expected to cost.
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
              onClick={() => void refreshAll().then(() => showToast('Backend data synced'))}
              startIcon={<RefreshCw size={16} />}
            >
              Sync backend data
            </HeroGhostButton>
            <HeroGhostButton onClick={() => setJoinDialogOpen(true)} startIcon={<Hash size={16} />}>
              Join with code
            </HeroGhostButton>
            <HeroPrimaryButton onClick={() => setDialogOpen(true)} variant="contained">
              Create itinerary
            </HeroPrimaryButton>
          </Stack>
        </HeroBannerContent>
      </HeroBanner>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 4 }}>
          <StatCard helper="Active itineraries across your travel workspace" icon={<Route size={20} />} label="Trips" value={String(itineraries.length)} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <StatCard helper="People available to join and collaborate on trips" icon={<Users2 size={20} />} label="Travelers" value={String(users.length)} />
        </Grid>
        <Grid size={{ xs: 12, md: 4 }}>
          <StatCard helper="Combined planned spend across all itineraries" icon={<Coins size={20} />} label="Total planned cost" value={totalCostLabel} />
        </Grid>
      </Grid>

      <Box>
        <Typography variant="h5">Your itineraries</Typography>
        <Typography color="text.secondary" mt={0.8}>
          Each card shows collaborators, date range, total cost, and event density so you can jump straight into planning.
        </Typography>
      </Box>

      <Grid container spacing={2.5}>
        {itineraries.map((itinerary) => {
          const itineraryEvents = events.filter((event) => event.itineraryId === itinerary.id);
          const collaborators = itinerary.memberIds
            .filter((memberId) => memberId !== currentUserId)
            .map((memberId) => usersMap[memberId])
            .filter(Boolean);

          return (
            <Grid key={itinerary.id} size={{ xs: 12, md: 6, xl: 4 }}>
              <ItineraryCard
                collaborators={collaborators}
                eventCount={itineraryEvents.length}
                itinerary={itinerary}
                totalCostLabel={formatCurrencySummary(getCostTotalsByCurrency(itineraryEvents), {
                  emptyLabel: 'No costs yet',
                  maxVisible: 1,
                })}
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

      <JoinItineraryDialog
        onClose={() => setJoinDialogOpen(false)}
        onSubmit={async (code) => {
          try {
            const itineraryId = await joinItineraryByCode(code);
            setJoinDialogOpen(false);
            showToast('Joined itinerary');
            navigate(`/itineraries/${itineraryId}`);
          } catch (error) {
            showToast(error instanceof Error ? error.message : 'Unable to join itinerary.', 'error');
            throw error;
          }
        }}
        open={joinDialogOpen}
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
