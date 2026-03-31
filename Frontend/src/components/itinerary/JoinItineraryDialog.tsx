import { useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';

interface JoinItineraryDialogProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (code: string) => Promise<void>;
}

export const JoinItineraryDialog = ({ open, onClose, onSubmit }: JoinItineraryDialogProps) => {
  const [code, setCode] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!open) {
      setCode('');
      setIsSubmitting(false);
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
          <TextField
            autoFocus
            helperText="Codes are numeric and exactly 5 digits long."
            inputProps={{ inputMode: 'numeric', maxLength: 5, pattern: '[0-9]*' }}
            label="Itinerary code"
            onChange={(event) => {
              const nextValue = event.target.value.replace(/\D/g, '').slice(0, 5);
              setCode(nextValue);
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
            void onSubmit(normalizedCode)
              .catch(() => undefined)
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
