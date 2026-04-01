import { CalendarClock, Edit3, ExternalLink, Link2, MapPinned, ReceiptText, Trash2 } from 'lucide-react';
import {
  alpha,
  Box,
  Button,
  ClickAwayListener,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Fade,
  IconButton,
  Link,
  Paper,
  Popper,
  Stack,
  Tooltip,
  Typography,
  useTheme,
} from '@mui/material';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { formatCurrencyAmount } from '../../lib/currency';
import { dayjs, formatDateTime, formatEventSchedule } from '../../lib/date';
import type { ItineraryEvent } from '../../types/event';
import type { User } from '../../types/user';
import { EventCategoryChip } from './EventCategoryChip';

interface EventPreviewDialogProps {
  open: boolean;
  anchorEl: HTMLElement | null;
  event: ItineraryEvent | null;
  usersMap: Record<string, User>;
  canManage: boolean;
  canDelete: boolean;
  onClose: () => void;
  onEdit: () => void;
  onDelete: (eventId: string) => Promise<void> | void;
}

export const EventPreviewDialog = ({
  open,
  anchorEl,
  event,
  usersMap,
  canManage,
  canDelete,
  onClose,
  onEdit,
  onDelete,
}: EventPreviewDialogProps) => {
  const theme = useTheme();
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showAllLinks, setShowAllLinks] = useState(false);

  useEffect(() => {
    if (!open) {
      setConfirmDeleteOpen(false);
      setIsDeleting(false);
      setShowAllLinks(false);
    }
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const handleKeyDown = (keyboardEvent: KeyboardEvent) => {
      if (keyboardEvent.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [onClose, open]);

  const lifecycleLabel = useMemo(() => {
    if (!event) {
      return '';
    }

    const hasBeenUpdated = event.updatedAt !== event.createdAt || event.updatedBy !== event.createdBy;
    const actorId = hasBeenUpdated ? event.updatedBy : event.createdBy;
    const actorName = usersMap[actorId]?.name ?? 'Unknown';
    const timestamp = hasBeenUpdated ? event.updatedAt : event.createdAt;

    return `${hasBeenUpdated ? 'Updated' : 'Created'} by ${actorName} • ${formatDateTime(timestamp)}`;
  }, [event, usersMap]);

  const previewScheduleLabel = useMemo(() => {
    if (!event) {
      return '';
    }

    return event.isAllDay ? formatPreviewAllDaySchedule(event) : formatEventSchedule(event);
  }, [event]);

  const visibleLinks = useMemo(
    () => (showAllLinks ? event?.links ?? [] : event?.links.slice(0, 2) ?? []),
    [event, showAllLinks],
  );

  if (!event) {
    return null;
  }

  return (
    <>
      <Popper
        anchorEl={anchorEl}
        modifiers={[
          {
            name: 'offset',
            options: {
              offset: [0, 10],
            },
          },
          {
            name: 'preventOverflow',
            options: {
              padding: 12,
            },
          },
          {
            name: 'flip',
            options: {
              padding: 12,
            },
          },
        ]}
        open={open && Boolean(anchorEl)}
        placement="auto-start"
        sx={{ zIndex: theme.zIndex.modal + 1 }}
        transition
      >
        {({ TransitionProps }) => (
          <Fade {...TransitionProps} timeout={140}>
            <Box>
              <ClickAwayListener mouseEvent="onMouseDown" onClickAway={onClose}>
                <Paper
                  elevation={0}
                  sx={{
                    width: { xs: 'min(360px, calc(100vw - 24px))', sm: 360 },
                    maxWidth: 'calc(100vw - 24px)',
                    p: 2,
                    borderRadius: theme.app.radius.md,
                    border: `1px solid ${alpha(theme.palette.divider, 0.92)}`,
                    backgroundColor: alpha(theme.palette.background.paper, theme.palette.mode === 'light' ? 0.98 : 0.99),
                    backdropFilter: 'blur(18px)',
                    boxShadow:
                      theme.palette.mode === 'light'
                        ? '0 18px 42px rgba(24, 50, 77, 0.16)'
                        : '0 24px 54px rgba(0, 0, 0, 0.42)',
                  }}
                >
                  <Stack spacing={1.45}>
                    <Stack alignItems="flex-start" direction="row" justifyContent="space-between" spacing={1.2}>
                      <Box minWidth={0}>
                        <Typography sx={{ wordBreak: 'break-word' }} variant="h6">
                          {event.title}
                        </Typography>
                      </Box>
                      <EventCategoryChip category={event.category} color={event.color} />
                    </Stack>

                    <Stack spacing={0.95}>
                      <PreviewDetailRow icon={<CalendarClock size={15} />} text={previewScheduleLabel} />
                      {event.location || event.locationAddress ? (
                        <PreviewDetailRow
                          icon={<MapPinned size={15} />}
                          text={event.locationAddress || event.location}
                        />
                      ) : null}
                      {event.cost > 0 ? (
                        <PreviewDetailRow
                          icon={<ReceiptText size={15} />}
                          text={formatCurrencyAmount(event.cost, event.currencyCode)}
                        />
                      ) : null}
                    </Stack>

                    {event.description ? (
                      <Box
                        sx={{
                          px: 1.4,
                          py: 1.15,
                          borderRadius: theme.app.radius.md,
                          backgroundColor: alpha(theme.palette.common.white, theme.palette.mode === 'light' ? 0.44 : 0.05),
                          border: `1px solid ${alpha(theme.palette.divider, 0.55)}`,
                        }}
                      >
                        <Typography
                          sx={{
                            display: '-webkit-box',
                            overflow: 'hidden',
                            WebkitBoxOrient: 'vertical',
                            WebkitLineClamp: event.remarks ? 2 : 3,
                            wordBreak: 'break-word',
                          }}
                          variant="body2"
                        >
                          {event.description}
                        </Typography>
                        {event.remarks ? (
                          <Typography
                            color="text.secondary"
                            sx={{
                              mt: 0.8,
                              display: '-webkit-box',
                              overflow: 'hidden',
                              WebkitBoxOrient: 'vertical',
                              WebkitLineClamp: 3,
                              wordBreak: 'break-word',
                            }}
                            variant="caption"
                          >
                            Remarks: {event.remarks}
                          </Typography>
                        ) : null}
                      </Box>
                    ) : null}

                    {event.links.length > 0 ? (
                      <Stack spacing={0.7}>
                        <Stack alignItems="center" direction="row" spacing={0.8}>
                          <Link2 size={15} />
                          <Typography fontWeight={600} variant="body2">
                            Links
                          </Typography>
                        </Stack>
                        {visibleLinks.map((link) => (
                          <Stack key={link.id} spacing={0.2}>
                            <Typography fontWeight={600} variant="caption">
                              {link.description}
                            </Typography>
                            <Link
                              color="primary.main"
                              href={link.url}
                              rel="noreferrer"
                              sx={{
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: 0.5,
                                minWidth: 0,
                                width: 'fit-content',
                                textDecoration: 'none',
                              }}
                              target="_blank"
                              underline="hover"
                              variant="caption"
                            >
                              <Box
                                component="span"
                                sx={{
                                  display: 'inline-block',
                                  maxWidth: '100%',
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                  whiteSpace: 'nowrap',
                                  verticalAlign: 'bottom',
                                }}
                              >
                                {link.url}
                              </Box>
                              <ExternalLink size={12} />
                            </Link>
                          </Stack>
                        ))}
                        {event.links.length > 2 ? (
                          <Button
                            onClick={() => setShowAllLinks((current) => !current)}
                            size="small"
                            sx={{
                              alignSelf: 'flex-start',
                              minWidth: 0,
                              px: 0.15,
                              py: 0.1,
                              fontSize: '0.74rem',
                              lineHeight: 1.2,
                            }}
                            variant="text"
                          >
                            {showAllLinks
                              ? 'See less'
                              : `+${event.links.length - 2} more link${event.links.length - 2 === 1 ? '' : 's'}`}
                          </Button>
                        ) : null}
                      </Stack>
                    ) : null}

                    <Typography color="text.secondary" variant="caption">
                      {lifecycleLabel}
                    </Typography>

                    {canManage || canDelete ? (
                      <Stack direction="row" justifyContent="flex-end" spacing={1}>
                        {canDelete ? (
                          <Tooltip title="Delete event">
                            <IconButton color="error" onClick={() => setConfirmDeleteOpen(true)}>
                              <Trash2 size={17} />
                            </IconButton>
                          </Tooltip>
                        ) : null}
                        {canManage ? (
                          <Tooltip title="Edit event">
                            <IconButton color="primary" onClick={onEdit}>
                              <Edit3 size={17} />
                            </IconButton>
                          </Tooltip>
                        ) : null}
                      </Stack>
                    ) : null}
                  </Stack>
                </Paper>
              </ClickAwayListener>
            </Box>
          </Fade>
        )}
      </Popper>

      <Dialog
        fullWidth
        maxWidth="xs"
        onClose={() => {
          if (!isDeleting) {
            setConfirmDeleteOpen(false);
          }
        }}
        open={confirmDeleteOpen}
      >
        <DialogTitle>Delete event?</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" variant="body2">
            This will permanently remove "{event.title}" from the itinerary.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button disabled={isDeleting} onClick={() => setConfirmDeleteOpen(false)}>
            Cancel
          </Button>
          <Button
            color="error"
            loading={isDeleting}
            onClick={async () => {
              setIsDeleting(true);

              try {
                await onDelete(event.id);
                onClose();
              } finally {
                setIsDeleting(false);
                setConfirmDeleteOpen(false);
              }
            }}
            variant="contained"
          >
            Delete event
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

const PreviewDetailRow = ({
  icon,
  text,
}: {
  icon: ReactNode;
  text: string;
}) => (
  <Stack alignItems="flex-start" direction="row" spacing={0.9}>
    <Box sx={{ mt: 0.15, color: 'text.secondary', flexShrink: 0 }}>{icon}</Box>
    <Typography
      sx={{
        wordBreak: 'break-word',
      }}
      variant="body2"
    >
      {text}
    </Typography>
  </Stack>
);

const formatPreviewAllDaySchedule = (event: ItineraryEvent) => {
  const start = dayjs(event.startDateTime);
  const end = dayjs(event.endDateTime);

  if (!start.isValid() || !end.isValid()) {
    return '';
  }

  if (start.isSame(end, 'day')) {
    return start.format('ddd, MMM D, YYYY');
  }

  if (start.isSame(end, 'month')) {
    return `${start.format('ddd, MMM D')} - ${end.format('ddd, MMM D, YYYY')}`;
  }

  return `${start.format('ddd, MMM D, YYYY')} - ${end.format('ddd, MMM D, YYYY')}`;
};
