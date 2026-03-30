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
import FullCalendar from '@fullcalendar/react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Navigate, useParams } from 'react-router-dom';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore, type EventInput, type ItineraryInput } from '../app/store/useTravelStore';
import { ItineraryCalendar, type CalendarView } from '../components/calendar/ItineraryCalendar';
import { CostSummaryCard } from '../components/common/CostSummaryCard';
import { EventDrawer } from '../components/event/EventDrawer';
import { EventListPanel } from '../components/event/EventListPanel';
import { ItineraryFormDialog } from '../components/itinerary/ItineraryFormDialog';
import { ShareItineraryDialog } from '../components/itinerary/ShareItineraryDialog';
import { UserAvatarGroup } from '../components/user/UserAvatarGroup';
import { formatCurrencySummary, getCostTotalsByCurrency } from '../lib/currency';
import { dayjs, formatDateRange } from '../lib/date';
import { getItineraryDuration, getUpcomingEvents, getUserMap } from '../lib/travel';

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
  const updateItinerary = useTravelStore((state) => state.updateItinerary);
  const shareItinerary = useTravelStore((state) => state.shareItinerary);
  const removeItineraryMember = useTravelStore((state) => state.removeItineraryMember);
  const createEvent = useTravelStore((state) => state.createEvent);
  const updateEvent = useTravelStore((state) => state.updateEvent);
  const deleteEvent = useTravelStore((state) => state.deleteEvent);
  const rescheduleEvent = useTravelStore((state) => state.rescheduleEvent);
  const loadEventHistory = useTravelStore((state) => state.loadEventHistory);
  const eventHistory = useTravelStore((state) => state.eventHistory);
  const searchUsers = useTravelStore((state) => state.searchUsers);
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
  const [eventDrawerOpen, setEventDrawerOpen] = useState(false);
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const [draftRange, setDraftRange] = useState<{ start: string; end: string } | null>(null);

  if (!itinerary) {
    return <Navigate replace to="/itineraries" />;
  }

  const sortedEvents = getUpcomingEvents(events);
  const totalCostLabel = formatCurrencySummary(getCostTotalsByCurrency(events), {
    emptyLabel: 'No costs yet',
    maxVisible: 2,
  });
  const canManage = itinerary.memberIds.includes(currentUserId);
  const isOwner = itinerary.createdBy === currentUserId;
  const selectedEvent = selectedEventId ? events.find((event) => event.id === selectedEventId) ?? null : null;
  const canDeleteSelectedEvent = Boolean(
    selectedEvent && currentUserId && (isOwner || selectedEvent.createdBy === currentUserId),
  );
  const members = itinerary.memberIds.map((memberId) => usersMap[memberId]).filter(Boolean);
  const collaborators = members.filter((member) => member.id !== currentUserId);
  const selectedEventHistory = selectedEvent ? eventHistory[selectedEvent.id] ?? [] : [];
  const latestEventActivityLabel = useMemo(() => {
    const activityTimestamps = [
      itinerary.createdAt,
      itinerary.updatedAt,
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
  }, [events, itinerary.createdAt, itinerary.updatedAt]);

  useEffect(() => {
    setActiveView('dayGridMonth');

    const calendarApi = calendarRef.current?.getApi();
    if (!calendarApi) {
      return;
    }

    calendarApi.changeView('dayGridMonth');
    calendarApi.gotoDate(itinerary.startDate);
  }, [itinerary.id, itinerary.startDate]);

  const openCreateEvent = (selection?: { start: string; end: string }) => {
    setSelectedEventId(null);
    setDraftRange(selection ?? null);
    setEventDrawerOpen(true);
  };

  const handleEventSave = async (values: EventInput, eventId?: string) => {
    if (eventId) {
      await updateEvent(eventId, values);
      showToast('Event updated');
    } else {
      await createEvent(itinerary.id, values);
      showToast('Event created');
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
                <HeaderActionButton onClick={() => setShareDialogOpen(true)} startIcon={<Share2 size={16} />} variant="outlined">
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
                <Metric label="Total cost" value={totalCostLabel} />
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

      <ShareItineraryDialog
        canManageMembers={isOwner}
        currentUserId={currentUserId}
        itinerary={itinerary}
        onClose={() => setShareDialogOpen(false)}
        onRemoveMember={async (userId) => {
          await removeItineraryMember(itinerary.id, userId);
          showToast('Contributor removed', 'warning');
        }}
        onSearchUsers={searchUsers}
        onSubmit={async (memberIds) => {
          await shareItinerary(itinerary.id, memberIds);
          setShareDialogOpen(false);
          showToast('Itinerary members updated');
        }}
        open={shareDialogOpen}
        users={users}
      />

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
    </Stack>
  );
};

const Metric = ({
  label,
  value,
}: {
  label: string;
  value: string;
}) => {
  return (
    <MetricPanel>
      <Typography color="text.secondary" variant="caption">
        {label}
      </Typography>
      <Typography fontWeight={700} mt={0.5} variant="body1">
        {value}
      </Typography>
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
