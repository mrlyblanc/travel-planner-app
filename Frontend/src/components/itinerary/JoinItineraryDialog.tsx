import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { ApiError } from '../../lib/api';

interface JoinItineraryDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (code: string) => Promise<void>;
}

export const JoinItineraryDialog = ({ open, onClose, onSubmit }: JoinItineraryDialogProps) => {
  const [code, setCode] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setCode('');
      setIsSubmitting(false);
      setSubmitError(null);
    }
  }, [open]);

  const normalizedCode = code.trim();
  const hasValidLength = /^[0-9]{5}$/.test(normalizedCode);

  return (
    <Dialog fullWidth maxWidth="xs" onClose={() => (isSubmitting ? undefined : onClose())} open={open}>
      <DialogTitle>Join itinerary</DialogTitle>
      <DialogContent>
        <Stack mt={1.5} spacing={2}>
          <Typography color="text.secondary" variant="body2">
            Enter the 5-digit code shared by the trip owner to join the itinerary and start collaborating in realtime.
          </Typography>
          {submitError ? <Alert severity="warning">{submitError}</Alert> : null}
          <TextField
            autoFocus
            error={Boolean(submitError)}
            helperText={submitError ?? 'Codes are numeric and exactly 5 digits long.'}
            inputProps={{ inputMode: 'numeric', maxLength: 5, pattern: '[0-9]*' }}
            label="Itinerary code"
            onChange={(event) => {
              const nextValue = event.target.value.replace(/\D/g, '').slice(0, 5);
              setCode(nextValue);
              if (submitError) {
                setSubmitError(null);
              }
            }}
            placeholder="48152"
            value={code}
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button disabled={isSubmitting} onClick={onClose}>
          Cancel
        </Button>
        <Button
          disabled={!hasValidLength || isSubmitting}
          onClick={() => {
            setIsSubmitting(true);
            setSubmitError(null);
            void onSubmit(normalizedCode)
              .catch((error) => {
                if (error instanceof ApiError && error.status === 429) {
                  setSubmitError('You’ve reached the join-code attempt limit. Wait a minute, then try again.');
                  return;
                }

                setSubmitError(error instanceof Error ? error.message : 'Unable to join this itinerary right now.');
              })
              .finally(() => setIsSubmitting(false));
          }}
          variant="contained"
        >
          {isSubmitting ? 'Joining...' : 'Join itinerary'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
