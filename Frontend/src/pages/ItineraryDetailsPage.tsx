import { ArrowLeft, ArrowRight, CalendarDays, Edit3, Plus, Share2, Sparkles, Users } from 'lucide-react';
import {
  Alert,
  alpha,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  FormControl,
  Grid,
  IconButton,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { styled } from '@mui/material/styles';
import type FullCalendar from '@fullcalendar/react';
import { Suspense, lazy, useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Navigate, useParams } from 'react-router-dom';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore, type EventInput, type ItineraryInput } from '../app/store/useTravelStore';
import type { CalendarView } from '../components/calendar/ItineraryCalendar';
import { CostSummaryCard } from '../components/common/CostSummaryCard';
import { RouteLoadingScreen } from '../components/common/RouteLoadingScreen';
import { EventListPanel } from '../components/event/EventListPanel';
import { UserAvatarGroup } from '../components/user/UserAvatarGroup';
import { ApiError } from '../lib/api';
import { getCostTotalsByCurrency, getCurrencySummaryDisplay } from '../lib/currency';
import { dayjs, formatDateRange } from '../lib/date';
import { getItineraryDuration, getUpcomingEvents, getUserMap } from '../lib/travel';

const ItineraryCalendar = lazy(() =>
  import('../components/calendar/ItineraryCalendar').then((module) => ({ default: module.ItineraryCalendar })),
);
const EventDrawer = lazy(() =>
  import('../components/event/EventDrawer').then((module) => ({ default: module.EventDrawer })),
);
const ItineraryFormDialog = lazy(() =>
  import('../components/itinerary/ItineraryFormDialog').then((module) => ({ default: module.ItineraryFormDialog })),
);
const loadShareItineraryDialog = () => import('../components/itinerary/ShareItineraryDialog');
const ShareItineraryDialog = lazy(() =>
  loadShareItineraryDialog().then((module) => ({ default: module.ShareItineraryDialog })),
);

const viewOptions: Array<{ value: CalendarView; label: string }> = [
  { value: 'timeGridDay', label: 'Day' },
  { value: 'timeGridThreeDay', label: '3 Days' },
  { value: 'timeGridWeek', label: 'Week' },
  { value: 'dayGridMonth', label: 'Month' },
];

export const ItineraryDetailsPage = () => {
  const { itineraryId } = useParams();
  const { showToast } = useToast();
  const calendarRef = useRef<FullCalendar | null>(null);
  const users = useTravelStore((state) => state.users);
  const currentUserId = useTravelStore((state) => state.currentUserId);
  const itineraries = useTravelStore((state) => state.itineraries);
  const allEvents = useTravelStore((state) => state.events);
  const itineraryBundleStatus = useTravelStore((state) =>
    itineraryId ? state.itineraryBundleStatus[itineraryId] ?? 'idle' : 'idle',
  );
  const itineraryShareCodes = useTravelStore((state) => state.itineraryShareCodes);
  const ensureItineraryBundle = useTravelStore((state) => state.ensureItineraryBundle);
  const updateItinerary = useTravelStore((state) => state.updateItinerary);
  const loadItineraryShareCode = useTravelStore((state) => state.loadItineraryShareCode);
  const rotateItineraryShareCode = useTravelStore((state) => state.rotateItineraryShareCode);
  const removeItineraryMember = useTravelStore((state) => state.removeItineraryMember);
  const createEvent = useTravelStore((state) => state.createEvent);
  const updateEvent = useTravelStore((state) => state.updateEvent);
  const deleteEvent = useTravelStore((state) => state.deleteEvent);
  const rescheduleEvent = useTravelStore((state) => state.rescheduleEvent);
  const loadEventHistory = useTravelStore((state) => state.loadEventHistory);
  const eventHistory = useTravelStore((state) => state.eventHistory);
  const usersMap = useMemo(() => getUserMap(users), [users]);
  const itinerary = useMemo(
    () => itineraries.find((entry) => entry.id === itineraryId),
    [itineraries, itineraryId],
  );
  const events = useMemo(
    () => allEvents.filter((event) => event.itineraryId === itineraryId),
    [allEvents, itineraryId],
  );
  const [activeView, setActiveView] = useState<CalendarView>('dayGridMonth');
  const [rangeLabel, setRangeLabel] = useState('');
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [isPreparingShareDialog, setIsPreparingShareDialog] = useState(false);
  const [eventDrawerOpen, setEventDrawerOpen] = useState(false);
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const [draftRange, setDraftRange] = useState<{ start: string; end: string } | null>(null);
  const [isShareCodeLoading, setIsShareCodeLoading] = useState(false);
  const [bundleLoadError, setBundleLoadError] = useState<string | null>(null);
  const itineraryCreatedAt = itinerary?.createdAt ?? '';
  const itineraryUpdatedAt = itinerary?.updatedAt ?? '';

  const sortedEvents = getUpcomingEvents(events);
  const totalCostSummary = useMemo(
    () => getCurrencySummaryDisplay(getCostTotalsByCurrency(events), { maxSecondary: 0 }),
    [events],
  );
  const canManage = itinerary?.memberIds.includes(currentUserId) ?? false;
  const isOwner = itinerary?.createdBy === currentUserId;
  const selectedEvent = selectedEventId ? events.find((event) => event.id === selectedEventId) ?? null : null;
  const canDeleteSelectedEvent = Boolean(
    selectedEvent && currentUserId && (isOwner || selectedEvent.createdBy === currentUserId),
  );
  const shareCode = itinerary ? itineraryShareCodes[itinerary.id] ?? null : null;
  const members = itinerary ? itinerary.memberIds.map((memberId) => usersMap[memberId]).filter(Boolean) : [];
  const collaborators = members.filter((member) => member.id !== currentUserId);
  const selectedEventHistory = selectedEvent ? eventHistory[selectedEvent.id] ?? [] : [];
  const currentItineraryId = itinerary?.id ?? null;
  const latestEventActivityLabel = useMemo(() => {
    const activityTimestamps = [
      itineraryCreatedAt,
      itineraryUpdatedAt,
      ...events.map((event) => {
        const eventLatestTimestamp = dayjs(event.updatedAt).isAfter(dayjs(event.createdAt)) ? event.updatedAt : event.createdAt;
        return eventLatestTimestamp;
      }),
    ].filter(Boolean);

    const latestActivity = activityTimestamps.reduce((latest, timestamp) => {
      if (!latest || dayjs(timestamp).isAfter(dayjs(latest))) {
        return timestamp;
      }

      return latest;
    }, '' as string);

    return latestActivity ? dayjs(latestActivity).fromNow() : 'No activity yet';
  }, [events, itineraryCreatedAt, itineraryUpdatedAt]);

  useEffect(() => {
    if (!itineraryId || !itinerary || itineraryBundleStatus === 'loaded') {
      setBundleLoadError(null);
      return;
    }

    if (itineraryBundleStatus === 'loading' || itineraryBundleStatus === 'error') {
      return;
    }

    let active = true;
    setBundleLoadError(null);

    void ensureItineraryBundle(itineraryId).catch((error) => {
      if (!active) {
        return;
      }

      setBundleLoadError(error instanceof Error ? error.message : 'Unable to load this itinerary right now.');
    });

    return () => {
      active = false;
    };
  }, [ensureItineraryBundle, itinerary, itineraryBundleStatus, itineraryId]);

  useEffect(() => {
    if (!itinerary) {
      return;
    }

    setActiveView('dayGridMonth');

    const calendarApi = calendarRef.current?.getApi();
    if (!calendarApi) {
      return;
    }

    calendarApi.changeView('dayGridMonth');
    calendarApi.gotoDate(itinerary.startDate);
  }, [itinerary]);

  const handleLoadShareCode = useCallback(async () => {
    if (!isOwner || !currentItineraryId) {
      return;
    }

    setIsShareCodeLoading(true);
    try {
      await loadItineraryShareCode(currentItineraryId);
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Unable to load join code.', 'error');
      throw error;
    } finally {
      setIsShareCodeLoading(false);
    }
  }, [currentItineraryId, isOwner, loadItineraryShareCode, showToast]);

  const handleRemoveMember = useCallback(async (userId: string) => {
    if (!itinerary) {
      return;
    }

    try {
      await removeItineraryMember(itinerary.id, userId);
      showToast('Contributor removed', 'warning');
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Unable to remove contributor.', 'error');
      throw error;
    }
  }, [itinerary, removeItineraryMember, showToast]);

  const handleRotateShareCode = useCallback(async () => {
    if (!currentItineraryId) {
      return;
    }

    try {
      await rotateItineraryShareCode(currentItineraryId);
      showToast('Join code regenerated');
    } catch (error) {
      if (error instanceof ApiError && error.status === 429) {
        throw error;
      }

      showToast(error instanceof Error ? error.message : 'Unable to regenerate join code.', 'error');
      throw error;
    }
  }, [currentItineraryId, rotateItineraryShareCode, showToast]);

  const handleOpenShareDialog = useCallback(async () => {
    setIsPreparingShareDialog(true);

    try {
      await loadShareItineraryDialog();

      if (isOwner && !shareCode) {
        await handleLoadShareCode().catch(() => undefined);
      }

      setShareDialogOpen(true);
    } finally {
      setIsPreparingShareDialog(false);
    }
  }, [handleLoadShareCode, isOwner, shareCode]);

  const openCreateEvent = (selection?: { start: string; end: string }) => {
    setSelectedEventId(null);
    setDraftRange(selection ?? null);
    setEventDrawerOpen(true);
  };

  const handleEventSave = async (values: EventInput, eventId?: string) => {
    if (eventId) {
      await updateEvent(eventId, values);
      showToast('Event updated');
    } else if (itinerary) {
      await createEvent(itinerary.id, values);
      showToast('Event created');
    } else {
      return;
    }

    setEventDrawerOpen(false);
    setSelectedEventId(null);
    setDraftRange(null);
  };

  const handleDeleteEvent = async (eventId: string) => {
    await deleteEvent(eventId);
    setEventDrawerOpen(false);
    setSelectedEventId(null);
    setDraftRange(null);
    showToast('Event deleted', 'warning');
  };

  if (!itinerary) {
    return <Navigate replace to="/itineraries" />;
  }

  if (itineraryBundleStatus !== 'loaded') {
    if (itineraryBundleStatus === 'error') {
      return (
        <Stack spacing={2.5} sx={{ minHeight: '70vh', justifyContent: 'center', maxWidth: 520 }}>
          <Alert severity="error">
            {bundleLoadError ?? 'We couldn’t load the latest collaborators and events for this itinerary.'}
          </Alert>
          <Box>
            <Button
              onClick={() => {
                setBundleLoadError(null);
                void ensureItineraryBundle(itinerary.id).catch((error) => {
                  setBundleLoadError(
                    error instanceof Error ? error.message : 'Unable to load this itinerary right now.',
                  );
                });
              }}
              variant="contained"
            >
              Retry loading itinerary
            </Button>
          </Box>
        </Stack>
      );
    }

    return (
      <RouteLoadingScreen
        description="Pulling the collaborators, events, and trip timeline for this itinerary."
        minHeight="70vh"
        title="Loading itinerary"
      />
    );
  }

  return (
    <Stack spacing={3}>
      <HeaderHeroCard>
        <HeaderHeroCardContent>
          <Stack spacing={3}>
            <Stack
              direction={{ xs: 'column', lg: 'row' }}
              justifyContent="space-between"
              spacing={2.5}
              sx={{ alignItems: { lg: 'flex-start' } }}
            >
              <Box maxWidth={760}>
                <Stack alignItems="center" direction="row" flexWrap="wrap" gap={1}>
                  <DestinationChip icon={<Sparkles size={14} />} label={itinerary.destination} />
                  <Chip icon={<CalendarDays size={14} />} label={formatDateRange(itinerary.startDate, itinerary.endDate)} variant="outlined" />
                  <Chip label={`${getItineraryDuration(itinerary.startDate, itinerary.endDate)} days`} variant="outlined" />
                </Stack>

                <Typography mt={2} variant="h4">
                  {itinerary.title}
                </Typography>
                <Typography color="text.secondary" mt={1.2}>
                  {itinerary.description}
                </Typography>
              </Box>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.2}>
                <HeaderActionButton onClick={() => setEditDialogOpen(true)} startIcon={<Edit3 size={16} />} variant="outlined">
                  Edit trip
                </HeaderActionButton>
                <HeaderActionButton
                  loading={isPreparingShareDialog}
                  onClick={() => void handleOpenShareDialog()}
                  startIcon={<Share2 size={16} />}
                  variant="outlined"
                >
                  Share
                </HeaderActionButton>
                <Button disabled={!canManage} onClick={() => openCreateEvent()} startIcon={<Plus size={16} />} variant="contained">
                  Add event
                </Button>
              </Stack>
            </Stack>

            <Stack
              direction={{ xs: 'column', md: 'row' }}
              justifyContent="space-between"
              spacing={2}
              sx={{ alignItems: { md: 'center' } }}
            >
              <Stack spacing={0.6}>
                <Stack alignItems="center" direction="row" spacing={1}>
                  <Users size={16} />
                  <Typography fontWeight={600} variant="body2">
                    Collaborators
                  </Typography>
                </Stack>
                {collaborators.length > 0 ? (
                  <UserAvatarGroup max={5} users={collaborators} />
                ) : (
                  <Typography color="text.secondary" variant="body2">
                    Just you on this itinerary for now
                  </Typography>
                )}
              </Stack>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                <Metric label="Events" value={String(events.length)} />
                <Metric
                  detail={
                    totalCostSummary && totalCostSummary.totalCount > 1
                      ? `+${totalCostSummary.totalCount - 1} more currenc${totalCostSummary.totalCount - 1 === 1 ? 'y' : 'ies'}`
                      : undefined
                  }
                  label="Total cost"
                  value={totalCostSummary?.primaryLabel ?? 'No costs yet'}
                />
                <Metric label="Last updated" value={latestEventActivityLabel} />
              </Stack>
            </Stack>

            {!canManage ? (
              <Alert severity="info">
                This itinerary is view-only for your account until the trip owner adds you as a collaborator.
              </Alert>
            ) : null}
          </Stack>
        </HeaderHeroCardContent>
      </HeaderHeroCard>

      <Card>
        <ToolbarCardContent>
          <Stack
            direction={{ xs: 'column', lg: 'row' }}
            justifyContent="space-between"
            spacing={2}
            sx={{ alignItems: { lg: 'center' } }}
          >
            <Stack direction="row" spacing={1}>
              <Tooltip title="Previous range">
                <CalendarNavButton aria-label="Previous range" onClick={() => calendarRef.current?.getApi().prev()}>
                  <ArrowLeft size={18} />
                </CalendarNavButton>
              </Tooltip>
              <CalendarTodayButton onClick={() => calendarRef.current?.getApi().today()} variant="outlined">
                Today
              </CalendarTodayButton>
              <Tooltip title="Next range">
                <CalendarNavButton aria-label="Next range" onClick={() => calendarRef.current?.getApi().next()}>
                  <ArrowRight size={18} />
                </CalendarNavButton>
              </Tooltip>
            </Stack>

            <Typography textAlign={{ xs: 'left', lg: 'center' }} variant="h6">
              {rangeLabel}
            </Typography>

            <ViewSelectControl size="small">
              <Select
                onChange={(event) => {
                  const value = event.target.value as CalendarView;
                  setActiveView(value);
                  calendarRef.current?.getApi().changeView(value);
                }}
                sx={{ borderRadius: 'inherit' }}
                value={activeView}
              >
                {viewOptions.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    {option.label}
                  </MenuItem>
                ))}
              </Select>
            </ViewSelectControl>
          </Stack>
        </ToolbarCardContent>
      </Card>

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, xl: 8 }}>
          <Card>
            <CalendarCardContent>
              <Suspense fallback={<CalendarSectionFallback />}>
                <ItineraryCalendar
                  activeView={activeView}
                  calendarRef={calendarRef}
                  canManage={canManage}
                  events={events}
                  initialDate={itinerary.startDate}
                  onRangeChange={setRangeLabel}
                  onReschedule={(eventId, start, end) => {
                    void rescheduleEvent(eventId, start, end).then(() => showToast('Event rescheduled'));
                  }}
                  onSelectEvent={(event) => {
                    setSelectedEventId(event.id);
                    setDraftRange(null);
                    setEventDrawerOpen(true);
                    void loadEventHistory(event.id);
                  }}
                  onSelectSlot={(selection) => openCreateEvent(selection)}
                  onViewChange={setActiveView}
                />
              </Suspense>
            </CalendarCardContent>
          </Card>
        </Grid>

        <Grid size={{ xs: 12, xl: 4 }}>
          <Stack spacing={2.5}>
            <CostSummaryCard events={events} />
            <EventListPanel
              events={sortedEvents}
              selectedEventId={selectedEventId}
              onSelectEvent={(event) => {
                setSelectedEventId(event.id);
                setDraftRange(null);
                setEventDrawerOpen(true);
                void loadEventHistory(event.id);
              }}
              usersMap={usersMap}
            />
          </Stack>
        </Grid>
      </Grid>

      <Suspense fallback={null}>
        {editDialogOpen ? (
          <ItineraryFormDialog
            initialValues={{
              title: itinerary.title,
              description: itinerary.description,
              destination: itinerary.destination,
              startDate: itinerary.startDate,
              endDate: itinerary.endDate,
            }}
            onClose={() => setEditDialogOpen(false)}
            onSubmit={async (values: ItineraryInput) => {
              await updateItinerary(itinerary.id, values);
              setEditDialogOpen(false);
              showToast('Itinerary updated');
            }}
            open={editDialogOpen}
            title="Edit itinerary"
          />
        ) : null}

        {shareDialogOpen ? (
          <ShareItineraryDialog
            canManageMembers={isOwner}
            currentUserId={currentUserId}
            isShareCodeLoading={isShareCodeLoading}
            itinerary={itinerary}
            onClose={() => setShareDialogOpen(false)}
            onLoadShareCode={handleLoadShareCode}
            onRemoveMember={handleRemoveMember}
            onRotateShareCode={handleRotateShareCode}
            open={shareDialogOpen}
            shareCode={shareCode}
            users={users}
          />
        ) : null}

        {eventDrawerOpen ? (
          <EventDrawer
            canManage={canManage}
            canDelete={canDeleteSelectedEvent}
            auditHistory={selectedEventHistory}
            draftRange={draftRange}
            event={selectedEvent}
            existingEvents={events}
            itinerary={itinerary}
            onLoadHistory={loadEventHistory}
            onClose={() => {
              setEventDrawerOpen(false);
              setSelectedEventId(null);
              setDraftRange(null);
            }}
            onDelete={handleDeleteEvent}
            onSave={handleEventSave}
            open={eventDrawerOpen}
            usersMap={usersMap}
          />
        ) : null}
      </Suspense>
    </Stack>
  );
};

const Metric = ({
  label,
  value,
  detail,
}: {
  label: string;
  value: ReactNode;
  detail?: string;
}) => {
  return (
    <MetricPanel>
      <Typography color="text.secondary" variant="caption">
        {label}
      </Typography>
      <Typography fontWeight={700} mt={0.5} sx={{ wordBreak: 'break-word' }} variant="body1">
        {value}
      </Typography>
      {detail ? (
        <Typography color="text.secondary" mt={0.45} variant="caption">
          {detail}
        </Typography>
      ) : null}
    </MetricPanel>
  );
};

const HeaderHeroCard = styled(Card)(({ theme }) => ({
  background: theme.app.surfaces.headerHero,
}));

const HeaderHeroCardContent = styled(CardContent)(({ theme }) => ({
  padding: theme.spacing(3),
  [theme.breakpoints.up('md')]: {
    padding: theme.spacing(4),
  },
}));

const DestinationChip = styled(Chip)(({ theme }) => ({
  backgroundColor: theme.app.selection.bg,
  color: theme.palette.text.primary,
}));

const HeaderActionButton = styled(Button)(({ theme }) => ({
  borderColor: theme.app.selection.border,
  backgroundColor: alpha(theme.palette.background.paper, theme.palette.mode === 'light' ? 0.28 : 0.32),
  borderRadius: theme.app.radius.sm,
}));

const ToolbarCardContent = styled(CardContent)(() => ({}));

const CalendarNavButton = styled(IconButton)(({ theme }) => ({
  border: `1px solid ${alpha(theme.palette.primary.main, 0.5)}`,
  borderRadius: theme.app.radius.sm,
  color: theme.palette.primary.main,
  '&:hover': {
    backgroundColor: alpha(theme.palette.primary.main, 0.08),
    borderColor: theme.palette.primary.main,
  },
}));

const CalendarTodayButton = styled(Button)(({ theme }) => ({
  borderRadius: theme.app.radius.sm,
}));

const ViewSelectControl = styled(FormControl)(({ theme }) => ({
  minWidth: 152,
  borderRadius: theme.app.radius.sm,
}));

const CalendarCardContent = styled(CardContent)(({ theme }) => ({
  padding: theme.spacing(1.5),
}));

const MetricPanel = styled(Box)(({ theme }) => ({
  minWidth: 136,
  paddingInline: theme.spacing(2),
  paddingBlock: theme.spacing(1.4),
  borderRadius: theme.app.radius.md,
  backgroundColor: theme.app.surfaces.metric,
  border: `1px solid ${theme.app.surfaces.metricBorder}`,
  backdropFilter: 'blur(12px)',
}));

const CalendarSectionFallback = () => (
  <Box sx={{ minHeight: 520 }}>
    <RouteLoadingScreen
      description="Loading the shared trip calendar and timeline."
      minHeight="100%"
      title="Loading calendar"
    />
  </Box>
);
