import { Check, Crown, UserMinus, UserPlus } from 'lucide-react';
import {
  Alert,
  Avatar,
  Autocomplete,
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
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useRef, useState } from 'react';
import type { Itinerary } from '../../types/itinerary';
import type { User } from '../../types/user';

interface ShareItineraryDialogProps {
  canManageMembers: boolean;
  currentUserId: string;
  itinerary: Itinerary;
  users: User[];
  open: boolean;
  onClose: () => void;
  onRemoveMember: (userId: string) => Promise<void>;
  onSearchUsers: (query: string) => Promise<User[]>;
  onSubmit: (memberIds: string[]) => void;
}

export const ShareItineraryDialog = ({
  canManageMembers,
  currentUserId,
  itinerary,
  users,
  open,
  onClose,
  onRemoveMember,
  onSearchUsers,
  onSubmit,
}: ShareItineraryDialogProps) => {
  const [selectedUsers, setSelectedUsers] = useState<User[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [searchResults, setSearchResults] = useState<User[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [pendingRemovalUser, setPendingRemovalUser] = useState<User | null>(null);
  const [removingUserId, setRemovingUserId] = useState<string | null>(null);
  const searchRequestId = useRef(0);

  const existingMembers = useMemo(
    () => users.filter((user) => itinerary.memberIds.includes(user.id)),
    [itinerary.memberIds, users],
  );
  const availableUsers = useMemo(
    () => users.filter((user) => !itinerary.memberIds.includes(user.id)),
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
      return;
    }

    setSelectedUsers([]);
    setSearchInput('');
    setSearchResults(availableUsers);
    setIsSearching(false);
    setPendingRemovalUser(null);
    setRemovingUserId(null);
    searchRequestId.current += 1;
  }, [itinerary.id, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    setSelectedUsers((current) => current.filter((user) => !itinerary.memberIds.includes(user.id)));

    if (searchInput.trim().length < 2) {
      setSearchResults(availableUsers);
    } else {
      setSearchResults((current) => current.filter((user) => !itinerary.memberIds.includes(user.id)));
    }
  }, [availableUsers, itinerary.memberIds, open, searchInput]);

  const handleRemoveMember = async () => {
    if (!pendingRemovalUser) {
      return;
    }

    setRemovingUserId(pendingRemovalUser.id);

    try {
      await onRemoveMember(pendingRemovalUser.id);
      setPendingRemovalUser(null);
    } finally {
      setRemovingUserId(null);
    }
  };

  return (
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
                  Everyone here can collaborate on the trip. Only the owner can add or remove contributors.
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
                            onClick={() => setPendingRemovalUser(member)}
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
            <Autocomplete
              disableCloseOnSelect
              filterOptions={(options) => options}
              filterSelectedOptions
              inputValue={searchInput}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              loading={isSearching}
              loadingText="Searching travelers..."
              multiple
              getOptionLabel={(option) => `${option.name} (${option.email})`}
              noOptionsText={searchInput.trim().length < 2 ? 'Type at least 2 characters' : 'No matching travelers'}
              onChange={(_, value, reason) => {
                setSelectedUsers(value);

                if (reason === 'selectOption' || reason === 'clear') {
                  setSearchInput('');
                  setSearchResults(availableUsers);
                  setIsSearching(false);
                  searchRequestId.current += 1;
                }
              }}
              onInputChange={(_, value, reason) => {
                if (reason === 'reset') {
                  return;
                }

                setSearchInput(value);
                const trimmedValue = value.trim();
                if (trimmedValue.length < 2) {
                  setSearchResults(availableUsers);
                  setIsSearching(false);
                  searchRequestId.current += 1;
                  return;
                }

                const requestId = searchRequestId.current + 1;
                searchRequestId.current = requestId;
                setIsSearching(true);

                void onSearchUsers(trimmedValue)
                  .then((results) => {
                    if (searchRequestId.current !== requestId) {
                      return;
                    }

                    setSearchResults(results.filter((user) => !itinerary.memberIds.includes(user.id)));
                  })
                  .catch(() => {
                    if (searchRequestId.current !== requestId) {
                      return;
                    }

                    setSearchResults([]);
                  })
                  .finally(() => {
                    if (searchRequestId.current === requestId) {
                      setIsSearching(false);
                    }
                  });
              }}
              options={searchResults}
              renderInput={(params) => (
                <TextField
                  {...params}
                  helperText="Invite people who should help plan, update, and keep this trip on schedule."
                  label="Add travelers"
                  placeholder="Search teammates by name or email"
                />
              )}
              renderOption={(props, option) => (
                <Box component="li" {...props}>
                  <Stack alignItems="center" direction="row" spacing={1.2}>
                    <Avatar sx={{ width: 30, height: 30 }}>{option.avatar}</Avatar>
                    <Box>
                      <Typography variant="body2">{option.name}</Typography>
                      <Typography color="text.secondary" variant="caption">
                        {option.email}
                      </Typography>
                    </Box>
                  </Stack>
                </Box>
              )}
              value={selectedUsers}
            />
          ) : (
            <Alert severity="info">You can view the contributor list, but only the itinerary owner can update it.</Alert>
          )}

          <Stack direction="row" spacing={1.2}>
            <UserPlus size={18} />
            <Typography color="text.secondary" variant="body2">
              Added members can build the itinerary together by creating, editing, and rescheduling trip events.
            </Typography>
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          disabled={!canManageMembers || selectedUsers.length === 0}
          onClick={() => onSubmit(Array.from(new Set([...itinerary.memberIds, ...selectedUsers.map((user) => user.id)])))}
          startIcon={<Check size={16} />}
          variant="contained"
        >
          Add contributors
        </Button>
      </DialogActions>

      <Dialog maxWidth="xs" onClose={() => (removingUserId ? undefined : setPendingRemovalUser(null))} open={Boolean(pendingRemovalUser)}>
        <DialogTitle>Remove contributor?</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            {pendingRemovalUser
              ? `${pendingRemovalUser.name} will lose access to this itinerary and its events.`
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
    </Dialog>
  );
};
