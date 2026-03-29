import { zodResolver } from '@hookform/resolvers/zod';
import { CalendarClock, MapPinned, Trash2, UserRound } from 'lucide-react';
import {
  Alert,
  alpha,
  Autocomplete,
  Avatar,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  MenuItem,
  Tooltip,
  Stack,
  TextField,
  Typography,
  useTheme,
} from '@mui/material';
import { DatePicker, TimePicker } from '@mui/x-date-pickers';
import { Controller, useForm, type Resolver } from 'react-hook-form';
import { useEffect, useMemo, useState } from 'react';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';
import type { EventInput } from '../../app/store/useTravelStore';
import { searchMockLocations } from '../../features/events/placeAutocomplete';
import { dayjs, formatDateTime } from '../../lib/date';
import {
  eventCategoryOptions,
  getEventColorOptions,
  getEventTextColor,
  normalizeEventColor,
  timezoneOptions,
} from '../../lib/events';
import type { EventAuditLog, EventCategory, ItineraryEvent, LocationSuggestion } from '../../types/event';
import type { Itinerary } from '../../types/itinerary';
import type { User } from '../../types/user';
import { EventCategoryChip } from './EventCategoryChip';

interface EventDrawerProps {
  open: boolean;
  itinerary: Itinerary;
  event: ItineraryEvent | null;
  auditHistory: EventAuditLog[];
  draftRange: { start: string; end: string } | null;
  usersMap: Record<string, User>;
  canManage: boolean;
  onLoadHistory: (eventId: string) => Promise<void>;
  onClose: () => void;
  onSave: (input: EventInput, eventId?: string) => void;
  onDelete: (eventId: string) => void;
}

const eventSchema = z
  .object({
    title: z.string().min(2, 'Title is required'),
    description: z.string().min(2, 'Description is required'),
    category: z
      .string()
      .min(1, 'Category is required')
      .refine((value) => eventCategoryOptions.includes(value as EventCategory), 'Category is required'),
    color: z.string().min(4, 'Color is required'),
    startDate: z.custom<Dayjs | null>((value) => Boolean(value), 'Start date is required'),
    startTime: z.custom<Dayjs | null>((value) => Boolean(value), 'Start time is required'),
    endDate: z.custom<Dayjs | null>((value) => Boolean(value), 'End date is required'),
    endTime: z.custom<Dayjs | null>((value) => Boolean(value), 'End time is required'),
    timezone: z.string().min(1, 'Timezone is required'),
    location: z.string().min(2, 'Location is required'),
    locationAddress: z.string(),
    locationLat: z.number().nullable(),
    locationLng: z.number().nullable(),
    cost: z.preprocess(
      (value) => {
        if (value === '' || value === null || value === undefined) {
          return 0;
        }

        if (typeof value === 'number') {
          return value;
        }

        return Number(value);
      },
      z.number().min(0, 'Cost cannot be negative'),
    ),
  })
  .refine(
    (values) => {
      if (!values.startDate || !values.startTime || !values.endDate || !values.endTime) {
        return true;
      }

      const startDateTime = combineDateAndTime(values.startDate, values.startTime);
      const endDateTime = combineDateAndTime(values.endDate, values.endTime);

      return startDateTime !== null && endDateTime !== null && endDateTime.isAfter(startDateTime);
    },
    {
      message: 'End time must be after the start time',
      path: ['endTime'],
    },
  );

type ParsedEventFormValues = z.output<typeof eventSchema>;

interface EventFormValues {
  title: string;
  description: string;
  category: EventCategory | '';
  color: string;
  startDate: Dayjs | null;
  startTime: Dayjs | null;
  endDate: Dayjs | null;
  endTime: Dayjs | null;
  timezone: string;
  location: string;
  locationAddress: string;
  locationLat: number | null;
  locationLng: number | null;
  cost: string;
}

const combineDateAndTime = (dateValue: Dayjs | null, timeValue: Dayjs | null) => {
  if (!dateValue || !timeValue) {
    return null;
  }

  return dateValue.hour(timeValue.hour()).minute(timeValue.minute()).second(0).millisecond(0);
};

const buildDefaultValues = (
  _itinerary: Itinerary,
  event: ItineraryEvent | null,
  _draftRange: { start: string; end: string } | null,
): EventFormValues => {
  if (event) {
    const startDateTime = dayjs(event.startDateTime);
    const endDateTime = dayjs(event.endDateTime);

    return {
      title: event.title,
      description: event.description,
      category: event.category,
      color: normalizeEventColor(event.color),
      startDate: startDateTime,
      startTime: startDateTime,
      endDate: endDateTime,
      endTime: endDateTime,
      timezone: event.timezone,
      location: event.location,
      locationAddress: event.locationAddress,
      locationLat: event.locationLat,
      locationLng: event.locationLng,
      cost: event.cost === 0 ? '0' : String(event.cost),
    };
  }

  return {
    title: '',
    description: '',
    category: '',
    color: '',
    startDate: null,
    startTime: null,
    endDate: null,
    endTime: null,
    timezone: '',
    location: '',
    locationAddress: '',
    locationLat: null,
    locationLng: null,
    cost: '',
  };
};

export const EventDrawer = ({
  open,
  itinerary,
  event,
  auditHistory,
  draftRange,
  usersMap,
  canManage,
  onLoadHistory,
  onClose,
  onSave,
  onDelete,
}: EventDrawerProps) => {
  const theme = useTheme();
  const [locationOptions, setLocationOptions] = useState<LocationSuggestion[]>([]);
  const [locationInput, setLocationInput] = useState('');
  const defaultValues = useMemo(() => buildDefaultValues(itinerary, event, draftRange), [draftRange, event, itinerary]);

  const {
    control,
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventSchema) as unknown as Resolver<EventFormValues>,
    defaultValues,
  });

  const locationAddress = watch('locationAddress');

  useEffect(() => {
    reset(defaultValues);
    setLocationInput(defaultValues.location);
  }, [defaultValues, reset]);

  useEffect(() => {
    if (!open || !event) {
      return;
    }

    void onLoadHistory(event.id);
  }, [event, onLoadHistory, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    let active = true;
    searchMockLocations(locationInput).then((results) => {
      if (active) {
        setLocationOptions(results);
      }
    });

    return () => {
      active = false;
    };
  }, [locationInput, open]);

  return (
    <Drawer
      anchor="right"
      onClose={onClose}
      open={open}
      PaperProps={{
        sx: {
          width: { xs: '100%', sm: 520 },
          p: 3,
          bgcolor: alpha(theme.palette.background.paper, theme.palette.mode === 'light' ? 0.97 : 0.98),
          color: theme.palette.text.primary,
          borderLeft: `1px solid ${theme.palette.divider}`,
          backgroundImage: 'none',
        },
      }}
    >
      <Stack spacing={2.5} sx={{ height: '100%' }}>
        <Stack direction="row" justifyContent="space-between" spacing={2}>
          <Box>
            <Typography variant="h6">{event ? 'Edit travel event' : 'Create event'}</Typography>
            <Typography color="text.secondary" variant="body2">
              {itinerary.title} • {itinerary.destination}
            </Typography>
          </Box>
          {event ? <EventCategoryChip category={event.category} /> : null}
        </Stack>

        {!canManage ? (
          <Alert severity="info">You can view this event, but only itinerary members can make changes.</Alert>
        ) : null}

        <Stack
          component="form"
          spacing={2.2}
          sx={{ overflowY: 'auto', pr: 0.5 }}
          onSubmit={handleSubmit((values) => {
            const parsedValues = eventSchema.parse(values) as ParsedEventFormValues;
            const startDateTime = combineDateAndTime(parsedValues.startDate, parsedValues.startTime);
            const endDateTime = combineDateAndTime(parsedValues.endDate, parsedValues.endTime);

            if (!startDateTime || !endDateTime) {
              return;
            }

            onSave(
              {
                title: parsedValues.title,
                description: parsedValues.description,
                category: parsedValues.category as EventCategory,
                color: normalizeEventColor(parsedValues.color),
                startDateTime: startDateTime.format(),
                endDateTime: endDateTime.format(),
                timezone: parsedValues.timezone,
                location: parsedValues.location,
                locationAddress: parsedValues.locationAddress,
                locationLat: parsedValues.locationLat,
                locationLng: parsedValues.locationLng,
                cost: parsedValues.cost,
              },
              event?.id,
            );
          })}
        >
          <TextField
            autoFocus={canManage}
            disabled={!canManage}
            error={Boolean(errors.title)}
            helperText={errors.title?.message}
            label="Title"
            {...register('title')}
          />

          <TextField
            disabled={!canManage}
            error={Boolean(errors.description)}
            helperText={errors.description?.message}
            label="Description"
            minRows={3}
            multiline
            {...register('description')}
          />

          <Controller
            control={control}
            name="category"
            render={({ field }) => (
              <TextField
                disabled={!canManage}
                error={Boolean(errors.category)}
                helperText={errors.category?.message}
                label="Category"
                onChange={field.onChange}
                select
                slotProps={{
                  inputLabel: { shrink: true },
                  select: {
                    displayEmpty: true,
                    renderValue: (value) => {
                      const selectedValue = typeof value === 'string' ? value : '';

                      return selectedValue ? (
                        selectedValue
                      ) : (
                        <Typography color="text.secondary" variant="body2">
                          Select category
                        </Typography>
                      );
                    },
                  },
                }}
                value={field.value || ''}
              >
                <MenuItem disabled value="">
                  Select category
                </MenuItem>
                {eventCategoryOptions.map((category) => (
                  <MenuItem key={category} value={category}>
                    {category}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />

          <Controller
            control={control}
            name="color"
            render={({ field }) => (
              <Box>
                <Typography gutterBottom fontWeight={600} variant="body2">
                  Calendar color
                </Typography>
                <Stack direction="row" flexWrap="wrap" gap={1}>
                  {getEventColorOptions(field.value).map((color) => {
                    const isSelected = Boolean(field.value) && normalizeEventColor(field.value) === normalizeEventColor(color);
                    const textColor = getEventTextColor(color);

                    return (
                      <Tooltip key={color} title={color}>
                        <Button
                          disabled={!canManage}
                          onClick={() => field.onChange(color)}
                          sx={{
                            minWidth: 0,
                            width: 42,
                            height: 42,
                            borderRadius: '50%',
                            bgcolor: color,
                            color: textColor,
                            border: isSelected
                              ? `3px solid ${theme.palette.mode === 'light' ? 'rgba(22, 48, 75, 0.9)' : 'rgba(237, 245, 255, 0.92)'}`
                              : `2px solid ${theme.palette.mode === 'light' ? 'rgba(255,255,255,0.95)' : 'rgba(18, 29, 43, 0.92)'}`,
                            boxShadow: isSelected
                              ? `0 0 0 3px ${alpha(theme.palette.primary.main, 0.24)}`
                              : theme.palette.mode === 'light'
                                ? '0 6px 18px rgba(21, 53, 90, 0.14)'
                                : '0 8px 20px rgba(0, 0, 0, 0.34)',
                            '&:hover': {
                              bgcolor: color,
                            },
                          }}
                          type="button"
                          variant="contained"
                        >
                          {isSelected ? '✓' : ''}
                        </Button>
                      </Tooltip>
                    );
                  })}
                </Stack>
                {errors.color ? (
                  <Typography color="error.main" mt={1} variant="caption">
                    {errors.color.message}
                  </Typography>
                ) : null}
              </Box>
            )}
          />

          <Stack spacing={2}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <Controller
                control={control}
                name="startDate"
                render={({ field }) => (
                  <DatePicker
                    disabled={!canManage}
                    format="MM/DD/YYYY"
                    label="Start date"
                    onChange={field.onChange}
                    slotProps={{
                      textField: {
                        error: Boolean(errors.startDate),
                        helperText: errors.startDate?.message,
                        fullWidth: true,
                        sx: { flex: 1.2, '& input': { fontSize: '0.76rem' } },
                      },
                    }}
                    value={field.value}
                  />
                )}
              />
              <Controller
                control={control}
                name="startTime"
                render={({ field }) => (
                  <TimePicker
                    ampm
                    disabled={!canManage}
                    label="Start time"
                    onChange={field.onChange}
                    slotProps={{
                      textField: {
                        error: Boolean(errors.startTime),
                        helperText: errors.startTime?.message,
                        fullWidth: true,
                        sx: { flex: 0.9, '& input': { fontSize: '0.76rem' } },
                      },
                    }}
                    value={field.value}
                  />
                )}
              />
            </Stack>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
              <Controller
                control={control}
                name="endDate"
                render={({ field }) => (
                  <DatePicker
                    disabled={!canManage}
                    format="MM/DD/YYYY"
                    label="End date"
                    onChange={field.onChange}
                    slotProps={{
                      textField: {
                        error: Boolean(errors.endDate),
                        helperText: errors.endDate?.message,
                        fullWidth: true,
                        sx: { flex: 1.2, '& input': { fontSize: '0.76rem' } },
                      },
                    }}
                    value={field.value}
                  />
                )}
              />
              <Controller
                control={control}
                name="endTime"
                render={({ field }) => (
                  <TimePicker
                    ampm
                    disabled={!canManage}
                    label="End time"
                    onChange={field.onChange}
                    slotProps={{
                      textField: {
                        error: Boolean(errors.endTime),
                        helperText: errors.endTime?.message,
                        fullWidth: true,
                        sx: { flex: 0.9, '& input': { fontSize: '0.76rem' } },
                      },
                    }}
                    value={field.value}
                  />
                )}
              />
            </Stack>
          </Stack>

          <Controller
            control={control}
            name="timezone"
            render={({ field }) => (
              <TextField
                disabled={!canManage}
                error={Boolean(errors.timezone)}
                helperText={errors.timezone?.message}
                label="Timezone"
                onChange={field.onChange}
                select
                slotProps={{
                  inputLabel: { shrink: true },
                  select: {
                    displayEmpty: true,
                    renderValue: (value) => {
                      const selectedValue = typeof value === 'string' ? value : '';

                      return selectedValue ? (
                        selectedValue
                      ) : (
                        <Typography color="text.secondary" variant="body2">
                          Select timezone
                        </Typography>
                      );
                    },
                  },
                }}
                value={field.value || ''}
              >
                <MenuItem disabled value="">
                  Select timezone
                </MenuItem>
                {timezoneOptions.map((timezone) => (
                  <MenuItem key={timezone} value={timezone}>
                    {timezone}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />

          <Controller
            control={control}
            name="location"
            render={({ field }) => (
              <Autocomplete
                disabled={!canManage}
                freeSolo
                getOptionLabel={(option) => (typeof option === 'string' ? option : option.name)}
                inputValue={locationInput}
                onChange={(_, value) => {
                  if (typeof value === 'string') {
                    field.onChange(value);
                    setValue('locationAddress', value);
                    setValue('locationLat', null);
                    setValue('locationLng', null);
                    return;
                  }

                  if (value) {
                    field.onChange(value.name);
                    setValue('locationAddress', value.address);
                    setValue('locationLat', value.lat);
                    setValue('locationLng', value.lng);
                  }
                }}
                onInputChange={(_, value) => {
                  setLocationInput(value);
                  field.onChange(value);
                }}
                options={locationOptions}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    error={Boolean(errors.location)}
                    helperText={errors.location?.message ?? 'Mock place search, structured for later API integration'}
                    label="Location"
                  />
                )}
                renderOption={(props, option) => (
                  <Box component="li" {...props}>
                    <Stack spacing={0.3}>
                      <Typography variant="body2">{option.name}</Typography>
                      <Typography color="text.secondary" variant="caption">
                        {option.address}
                      </Typography>
                    </Stack>
                  </Box>
                )}
                value={field.value}
              />
            )}
          />

          <TextField disabled={!canManage} label="Matched address" value={locationAddress} {...register('locationAddress')} />

          <Controller
            control={control}
            name="cost"
            render={({ field }) => (
              <TextField
                disabled={!canManage}
                error={Boolean(errors.cost)}
                helperText={errors.cost?.message}
                inputProps={{ min: 0, step: '0.01' }}
                label="Cost (USD)"
                onChange={field.onChange}
                type="number"
                value={field.value}
              />
            )}
          />

          {event ? (
            <>
              <Divider />
              <Stack spacing={1.2}>
                <Typography fontWeight={600} variant="body2">
                  Collaboration trail
                </Typography>
                <Stack alignItems="center" direction="row" spacing={1.2}>
                  <Avatar sx={{ width: 30, height: 30 }}>{usersMap[event.createdBy]?.avatar ?? 'U'}</Avatar>
                  <Box>
                    <Stack alignItems="center" direction="row" spacing={0.8}>
                      <UserRound size={14} />
                      <Typography variant="body2">Created by {usersMap[event.createdBy]?.name ?? 'Unknown'}</Typography>
                    </Stack>
                    <Typography color="text.secondary" variant="caption">
                      {formatDateTime(event.createdAt)}
                    </Typography>
                  </Box>
                </Stack>
                <Stack alignItems="center" direction="row" spacing={1.2}>
                  <Avatar sx={{ width: 30, height: 30 }}>{usersMap[event.updatedBy]?.avatar ?? 'U'}</Avatar>
                  <Box>
                    <Stack alignItems="center" direction="row" spacing={0.8}>
                      <CalendarClock size={14} />
                      <Typography variant="body2">
                        Last updated by {usersMap[event.updatedBy]?.name ?? 'Unknown'}
                      </Typography>
                    </Stack>
                    <Typography color="text.secondary" variant="caption">
                      {formatDateTime(event.updatedAt)}
                    </Typography>
                  </Box>
                </Stack>
                <Stack alignItems="center" direction="row" spacing={1.2}>
                  <MapPinned size={14} />
                  <Typography color="text.secondary" variant="body2">
                    {event.locationAddress || event.location}
                  </Typography>
                </Stack>

                {auditHistory.length > 0 ? (
                  <Stack spacing={1}>
                    <Typography fontWeight={600} mt={0.8} variant="body2">
                      Audit history
                    </Typography>
                    {auditHistory.slice(0, 4).map((historyItem) => (
                      <Box
                        key={historyItem.id}
                        sx={{
                          border: `1px solid ${theme.palette.divider}`,
                          borderRadius: theme.app.radius.md,
                          px: 1.5,
                          py: 1.2,
                        }}
                      >
                        <Typography variant="body2">{historyItem.summary}</Typography>
                        <Typography color="text.secondary" variant="caption">
                          {usersMap[historyItem.changedBy]?.name ?? 'Unknown'} • {formatDateTime(historyItem.changedAt)}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                ) : null}
              </Stack>
            </>
          ) : null}

          <Box sx={{ mt: 'auto' }} />

          <Stack direction="row" justifyContent="space-between" pt={1}>
            <Box>
              {event && canManage ? (
                <IconButton color="error" onClick={() => onDelete(event.id)}>
                  <Trash2 size={18} />
                </IconButton>
              ) : null}
            </Box>
            <Stack direction="row" spacing={1.2}>
              <Button onClick={onClose}>Cancel</Button>
              <Button loading={isSubmitting} type="submit" variant="contained">
                {event ? 'Save changes' : 'Create event'}
              </Button>
            </Stack>
          </Stack>
        </Stack>
      </Stack>
    </Drawer>
  );
};
