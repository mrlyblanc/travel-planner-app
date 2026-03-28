import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField } from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers';
import type { Dayjs } from 'dayjs';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import type { ItineraryInput } from '../../app/store/useTravelStore';
import { dayjs } from '../../lib/date';

const itinerarySchema = z
  .object({
    title: z.string().min(2, 'Title is required'),
    description: z.string().min(8, 'Description should be at least 8 characters'),
    destination: z.string().min(2, 'Destination is required'),
    startDate: z.custom<Dayjs | null>((value) => Boolean(value), 'Start date is required'),
    endDate: z.custom<Dayjs | null>((value) => Boolean(value), 'End date is required'),
  })
  .refine((values) => values.startDate !== null && values.endDate !== null && !values.endDate.isBefore(values.startDate, 'day'), {
    message: 'End date must be after the start date',
    path: ['endDate'],
  });

type ItineraryFormValues = z.infer<typeof itinerarySchema>;

interface ItineraryFormDialogProps {
  open: boolean;
  title: string;
  initialValues?: ItineraryInput;
  onClose: () => void;
  onSubmit: (values: ItineraryInput) => void;
}

const buildDefaultValues = (initialValues?: ItineraryInput): ItineraryFormValues => ({
  title: initialValues?.title ?? '',
  description: initialValues?.description ?? '',
  destination: initialValues?.destination ?? '',
  startDate: initialValues?.startDate ? dayjs(initialValues.startDate) : null,
  endDate: initialValues?.endDate ? dayjs(initialValues.endDate) : null,
});

export const ItineraryFormDialog = ({
  open,
  title,
  initialValues,
  onClose,
  onSubmit,
}: ItineraryFormDialogProps) => {
  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ItineraryFormValues>({
    resolver: zodResolver(itinerarySchema),
    defaultValues: buildDefaultValues(initialValues),
  });

  useEffect(() => {
    reset(buildDefaultValues(initialValues));
  }, [initialValues, open, reset]);

  return (
    <Dialog fullWidth maxWidth="sm" onClose={onClose} open={open}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack mt={1} spacing={2}>
          <TextField
            autoFocus
            error={Boolean(errors.title)}
            helperText={errors.title?.message}
            label="Title"
            {...register('title')}
          />
          <TextField
            error={Boolean(errors.destination)}
            helperText={errors.destination?.message}
            label="Destination"
            {...register('destination')}
          />
          <TextField
            error={Boolean(errors.description)}
            helperText={errors.description?.message}
            label="Description"
            minRows={3}
            multiline
            {...register('description')}
          />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <Controller
              control={control}
              name="startDate"
              render={({ field }) => (
                <DatePicker
                  format="MM/DD/YYYY"
                  label="Start date"
                  onChange={field.onChange}
                  slotProps={{
                    textField: {
                      error: Boolean(errors.startDate),
                      helperText: errors.startDate?.message,
                      fullWidth: true,
                      sx: { '& input': { fontSize: '0.76rem' } },
                    },
                  }}
                  value={field.value}
                />
              )}
            />
            <Controller
              control={control}
              name="endDate"
              render={({ field }) => (
                <DatePicker
                  format="MM/DD/YYYY"
                  label="End date"
                  onChange={field.onChange}
                  slotProps={{
                    textField: {
                      error: Boolean(errors.endDate),
                      helperText: errors.endDate?.message,
                      fullWidth: true,
                      sx: { '& input': { fontSize: '0.76rem' } },
                    },
                  }}
                  value={field.value}
                />
              )}
            />
          </Stack>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 3 }}>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          onClick={handleSubmit((values) =>
            onSubmit({
              title: values.title,
              description: values.description,
              destination: values.destination,
              startDate: values.startDate?.format('YYYY-MM-DD') ?? '',
              endDate: values.endDate?.format('YYYY-MM-DD') ?? '',
            }),
          )}
          variant="contained"
        >
          {isSubmitting ? 'Saving...' : 'Save itinerary'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
