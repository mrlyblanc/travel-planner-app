import { Check, UserPlus } from 'lucide-react';
import {
  Avatar,
  Autocomplete,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import type { Itinerary } from '../../types/itinerary';
import type { User } from '../../types/user';

interface ShareItineraryDialogProps {
  itinerary: Itinerary;
  users: User[];
  open: boolean;
  onClose: () => void;
  onSearchUsers: (query: string) => Promise<User[]>;
  onSubmit: (memberIds: string[]) => void;
}

export const ShareItineraryDialog = ({
  itinerary,
  users,
  open,
  onClose,
  onSearchUsers,
  onSubmit,
}: ShareItineraryDialogProps) => {
  const [selectedUsers, setSelectedUsers] = useState<User[]>([]);
  const [searchResults, setSearchResults] = useState<User[]>([]);

  const existingMembers = useMemo(
    () => users.filter((user) => itinerary.memberIds.includes(user.id)),
    [itinerary.memberIds, users],
  );
  const availableUsers = useMemo(
    () => users.filter((user) => !itinerary.memberIds.includes(user.id)),
    [itinerary.memberIds, users],
  );

  useEffect(() => {
    setSelectedUsers([]);
    setSearchResults(availableUsers);
  }, [availableUsers, open, itinerary.id]);

  return (
    <Dialog fullWidth maxWidth="sm" onClose={onClose} open={open}>
      <DialogTitle>Share itinerary</DialogTitle>
      <DialogContent>
        <Stack mt={1.5} spacing={2.5}>
          <Box>
            <Typography fontWeight={600} gutterBottom variant="body2">
              Existing members
            </Typography>
            <Stack direction="row" flexWrap="wrap" gap={1}>
              {existingMembers.map((member) => (
                <Chip
                  key={member.id}
                  avatar={<Avatar>{member.avatar}</Avatar>}
                  label={member.name}
                  variant="outlined"
                />
              ))}
            </Stack>
          </Box>

          <Autocomplete
            multiple
            getOptionLabel={(option) => `${option.name} (${option.email})`}
            onChange={(_, value) => setSelectedUsers(value)}
            onInputChange={(_, value, reason) => {
              if (reason === 'reset') {
                return;
              }

              const trimmedValue = value.trim();
              if (trimmedValue.length < 2) {
                setSearchResults(availableUsers);
                return;
              }

              void onSearchUsers(trimmedValue).then((results) => {
                setSearchResults(results.filter((user) => !itinerary.memberIds.includes(user.id)));
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
          onClick={() => onSubmit(Array.from(new Set([...itinerary.memberIds, ...selectedUsers.map((user) => user.id)])))}
          startIcon={<Check size={16} />}
          variant="contained"
        >
          Save members
        </Button>
      </DialogActions>
    </Dialog>
  );
};
