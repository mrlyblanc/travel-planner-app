import { Check, Copy, Crown, RefreshCw, UserMinus } from 'lucide-react';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { ApiError } from '../../lib/api';
import type { Itinerary, ItineraryShareCode } from '../../types/itinerary';
import type { User } from '../../types/user';

interface ShareItineraryDialogProps {
  canManageMembers: boolean;
  currentUserId: string;
  itinerary: Itinerary;
  users: User[];
  shareCode: ItineraryShareCode | null;
  isShareCodeLoading: boolean;
  open: boolean;
  onClose: () => void;
  onLoadShareCode: () => Promise<void>;
  onRotateShareCode: () => Promise<void>;
  onRemoveMember: (userId: string) => Promise<void>;
}

export const ShareItineraryDialog = ({
  canManageMembers,
  currentUserId,
  itinerary,
  users,
  shareCode,
  isShareCodeLoading,
  open,
  onClose,
  onLoadShareCode,
  onRotateShareCode,
  onRemoveMember,
}: ShareItineraryDialogProps) => {
  const [pendingRemovalUser, setPendingRemovalUser] = useState<User | null>(null);
  const [removingUserId, setRemovingUserId] = useState<string | null>(null);
  const [isRotatingCode, setIsRotatingCode] = useState(false);
  const [isCodeCopied, setIsCodeCopied] = useState(false);
  const [rotateError, setRotateError] = useState<string | null>(null);

  const existingMembers = useMemo(
    () => users.filter((user) => itinerary.memberIds.includes(user.id)),
    [itinerary.memberIds, users],
  );
  const sortedExistingMembers = useMemo(
    () =>
      [...existingMembers].sort((left, right) => {
        if (left.id === itinerary.createdBy) {
          return -1;
        }

        if (right.id === itinerary.createdBy) {
          return 1;
        }

        if (left.id === currentUserId) {
          return -1;
        }

        if (right.id === currentUserId) {
          return 1;
        }

        return left.name.localeCompare(right.name);
      }),
    [currentUserId, existingMembers, itinerary.createdBy],
  );

  useEffect(() => {
    if (!open) {
      setPendingRemovalUser(null);
      setRemovingUserId(null);
      setIsRotatingCode(false);
      setIsCodeCopied(false);
      setRotateError(null);
      return;
    }

    setPendingRemovalUser(null);
    setRemovingUserId(null);
    setIsRotatingCode(false);
    setIsCodeCopied(false);
    setRotateError(null);
  }, [itinerary.id, open]);

  useEffect(() => {
    if (!open || !canManageMembers) {
      return;
    }

    void onLoadShareCode().catch(() => undefined);
  }, [canManageMembers, itinerary.id, onLoadShareCode, open]);

  useEffect(() => {
    if (!pendingRemovalUser) {
      return;
    }

    const isStillMember = itinerary.memberIds.includes(pendingRemovalUser.id);
    if (!isStillMember) {
      setPendingRemovalUser(null);
    }
  }, [itinerary.memberIds, pendingRemovalUser]);

  const handleOpenRemovalDialog = (member: User) => {
    if (removingUserId) {
      return;
    }

    setPendingRemovalUser(member);
  };

  useEffect(() => {
    if (!isCodeCopied) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setIsCodeCopied(false);
    }, 1800);

    return () => window.clearTimeout(timeoutId);
  }, [isCodeCopied]);

  const handleRemoveMember = async () => {
    if (!pendingRemovalUser) {
      return;
    }

    setRemovingUserId(pendingRemovalUser.id);

    try {
      await onRemoveMember(pendingRemovalUser.id);
      setPendingRemovalUser(null);
    } catch {
      // Errors are surfaced by the shared app error/toast flow.
    } finally {
      setRemovingUserId(null);
    }
  };

  const handleCopyShareCode = async () => {
    if (!shareCode?.code) {
      return;
    }

    try {
      await navigator.clipboard.writeText(shareCode.code);
      setIsCodeCopied(true);
    } catch {
      setIsCodeCopied(false);
    }
  };

  const handleRotateShareCode = async () => {
    setRotateError(null);
    setIsRotatingCode(true);

    try {
      await onRotateShareCode();
      setIsCodeCopied(false);
    } catch (error) {
      if (error instanceof ApiError && error.status === 429) {
        setRotateError('You’ve reached the code regeneration limit. Wait a minute, then try again.');
      }
    } finally {
      setIsRotatingCode(false);
    }
  };

  return (
    <>
      <Dialog fullWidth maxWidth="sm" onClose={onClose} open={open}>
        <DialogTitle>Share itinerary</DialogTitle>
        <DialogContent>
          <Stack mt={1.5} spacing={2.5}>
            <Box>
              <Stack direction="row" justifyContent="space-between" spacing={2}>
                <Box>
                  <Typography fontWeight={600} gutterBottom variant="body2">
                    Current contributors
                  </Typography>
                  <Typography color="text.secondary" variant="body2">
                    Everyone here can collaborate on the trip. Only the owner can remove contributors or generate a join code.
                  </Typography>
                </Box>
                <Chip
                  icon={<Crown size={14} />}
                  label={users.find((user) => user.id === itinerary.createdBy)?.name ?? 'Owner'}
                  size="small"
                  variant="outlined"
                />
              </Stack>

              <Stack mt={1.5} spacing={1.1}>
                {sortedExistingMembers.map((member) => {
                  const isOwner = member.id === itinerary.createdBy;
                  const isCurrentUser = member.id === currentUserId;
                  const canRemoveMember = canManageMembers && !isOwner;

                  return (
                    <Box
                      key={member.id}
                      sx={(theme) => ({
                        alignItems: 'center',
                        border: `1px solid ${theme.palette.divider}`,
                        borderRadius: theme.app.radius.md,
                        display: 'flex',
                        justifyContent: 'space-between',
                        gap: 1.5,
                        px: 1.5,
                        py: 1.2,
                      })}
                    >
                      <Stack alignItems="center" direction="row" spacing={1.2}>
                        <Avatar sx={{ width: 34, height: 34 }}>{member.avatar}</Avatar>
                        <Box>
                          <Stack alignItems="center" direction="row" flexWrap="wrap" gap={0.8}>
                            <Typography fontWeight={600} variant="body2">
                              {member.name}
                              {isCurrentUser ? ' (You)' : ''}
                            </Typography>
                            {isOwner ? <Chip color="primary" label="Owner" size="small" variant="outlined" /> : null}
                          </Stack>
                          <Typography color="text.secondary" variant="caption">
                            {member.email}
                          </Typography>
                        </Box>
                      </Stack>

                      {canRemoveMember ? (
                        <Tooltip title={`Remove ${member.name}`}>
                          <span>
                            <IconButton
                              aria-label={`Remove ${member.name}`}
                              color="error"
                              disabled={Boolean(removingUserId)}
                              onClick={(event) => {
                                event.stopPropagation();
                                handleOpenRemovalDialog(member);
                              }}
                              size="small"
                            >
                              {removingUserId === member.id ? <CircularProgress color="inherit" size={16} /> : <UserMinus size={16} />}
                            </IconButton>
                          </span>
                        </Tooltip>
                      ) : null}
                    </Box>
                  );
                })}
              </Stack>
            </Box>

            <Divider />

            {canManageMembers ? (
              <Box>
                <Typography fontWeight={600} gutterBottom variant="body2">
                  Join code
                </Typography>
                <Typography color="text.secondary" variant="body2">
                  Share this 5-digit code with a traveler. They can join the itinerary from the home screen without being searched manually.
                </Typography>
                {rotateError ? (
                  <Alert severity="warning" sx={{ mt: 1.5 }}>
                    {rotateError}
                  </Alert>
                ) : null}

                <Box
                  sx={(theme) => ({
                    mt: 1.6,
                    border: `1px solid ${theme.palette.divider}`,
                    borderRadius: theme.app.radius.md,
                    bgcolor: theme.app.surfaces.metric,
                    px: 2,
                    py: 2,
                  })}
                >
                  {isShareCodeLoading && !shareCode ? (
                    <Stack alignItems="center" direction="row" spacing={1.2}>
                      <CircularProgress size={18} />
                      <Typography color="text.secondary" variant="body2">
                        Loading share code…
                      </Typography>
                    </Stack>
                  ) : (
                    <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" spacing={2}>
                      <Box>
                        <Typography
                          letterSpacing="0.28em"
                          sx={{ fontVariantNumeric: 'tabular-nums' }}
                          variant="h4"
                        >
                          {shareCode?.code ?? '-----'}
                        </Typography>
                        <Typography color="text.secondary" mt={0.6} variant="caption">
                          Rotate the code anytime if you only want to accept new collaborators through a fresh invite.
                        </Typography>
                      </Box>

                      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                        <Button
                          onClick={() => void handleCopyShareCode()}
                          startIcon={isCodeCopied ? <Check size={16} /> : <Copy size={16} />}
                          variant="outlined"
                        >
                          {isCodeCopied ? 'Copied' : 'Copy code'}
                        </Button>
                        <Button
                          disabled={isShareCodeLoading || isRotatingCode}
                          onClick={() => void handleRotateShareCode()}
                          startIcon={isRotatingCode ? <CircularProgress color="inherit" size={16} /> : <RefreshCw size={16} />}
                          variant="outlined"
                        >
                          Regenerate
                        </Button>
                      </Stack>
                    </Stack>
                  )}
                </Box>
              </Box>
            ) : (
              <Alert severity="info">
                Only the itinerary owner can generate or rotate the join code. If someone needs access, ask the owner to share the current 5-digit code.
              </Alert>
            )}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>

      <Dialog
        fullWidth
        maxWidth="xs"
        onClose={() => {
          if (!removingUserId) {
            setPendingRemovalUser(null);
          }
        }}
        open={open && Boolean(pendingRemovalUser)}
      >
        <DialogTitle>Remove contributor?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            {pendingRemovalUser
              ? `${pendingRemovalUser.name} will lose access to this itinerary and all of its shared events.`
              : 'This contributor will lose access to the itinerary.'}
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button disabled={Boolean(removingUserId)} onClick={() => setPendingRemovalUser(null)}>
            Cancel
          </Button>
          <Button color="error" disabled={Boolean(removingUserId)} onClick={() => void handleRemoveMember()} variant="contained">
            {removingUserId ? 'Removing...' : 'Remove contributor'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};
