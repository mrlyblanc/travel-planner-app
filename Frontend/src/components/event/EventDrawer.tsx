import { zodResolver } from '@hookform/resolvers/zod';
import { CalendarClock, Check, Edit3, Link2, MapPinned, Plus, Trash2, UserRound } from 'lucide-react';
import {
  Alert,
  alpha,
  Autocomplete,
  Avatar,
  Box,
  Button,
  Checkbox,
  Divider,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Drawer,
  FormControlLabel,
  IconButton,
  InputAdornment,
  MenuItem,
  Tooltip,
  Stack,
  TextField,
  Typography,
  useTheme,
} from '@mui/material';
import { DatePicker, TimePicker } from '@mui/x-date-pickers';
import { Controller, useFieldArray, useForm, useWatch, type Resolver } from 'react-hook-form';
import { useEffect, useLayoutEffect, useMemo, useState } from 'react';
import type { Dayjs } from 'dayjs';
import { z } from 'zod';
import type { EventInput } from '../../app/store/useTravelStore';
import { isGeoapifyConfigured, searchLocationSuggestions } from '../../features/events/placeAutocomplete';
import {
  formatEditableCurrencyAmount,
  getCurrencyMinorUnit,
  getCurrencyOption,
  getCurrencyOptionLabel,
  hasValidCurrencyPrecision,
  normalizeCurrencyCode,
  roundCurrencyAmount,
  supportedCurrencies,
} from '../../lib/currency';
import {
  buildAllDayDateTimeRange,
  buildCurrentFallbackTimeRange,
  combineDateAndTime,
  DEFAULT_TIMED_SLOT_MINUTES,
  dayjs,
  formatDateTime,
  formatEventSchedule,
} from '../../lib/date';
import {
  eventCategoryOptions,
  findEventConflicts,
  formatTimezoneDisplayLabel,
  formatTimezoneLabel,
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
  existingEvents: ItineraryEvent[];
  auditHistory: EventAuditLog[];
  draftRange: { start: string; end: string; allDay: boolean } | null;
  usersMap: Record<string, User>;
  canManage: boolean;
  canDelete: boolean;
  onLoadHistory: (eventId: string) => Promise<void>;
  onClose: () => void;
  onSave: (input: EventInput, eventId?: string) => Promise<void> | void;
  onDelete: (eventId: string) => Promise<void> | void;
}

const eventLinkSchema = z.object({
  description: z.string().trim().min(2, 'Link description is required').max(160, 'Keep link descriptions under 160 characters'),
  url: z
    .string()
    .trim()
    .url('Enter a valid link')
    .refine((value) => /^https?:\/\//i.test(value), 'Links must start with http:// or https://'),
});

const dayjsField = z.custom<Dayjs | null>((value) => value === null || dayjs.isDayjs(value));

const eventSchema = z
  .object({
    title: z.string().trim().min(2, 'Title is required').max(160, 'Keep titles under 160 characters'),
    description: z.string().trim().min(2, 'Description is required').max(4000, 'Keep descriptions under 4000 characters'),
    remarks: z.string().trim().max(4000, 'Keep remarks under 4000 characters'),
    category: z
      .string()
      .min(1, 'Category is required')
      .refine((value) => eventCategoryOptions.includes(value as EventCategory), 'Category is required'),
    color: z.string().trim().min(4, 'Color is required'),
    isAllDay: z.boolean(),
    startDate: dayjsField,
    startTime: dayjsField,
    endDate: dayjsField,
    endTime: dayjsField,
    timezone: z.string().trim().min(1, 'Timezone is required'),
    location: z.string().trim().min(2, 'Location is required').max(200, 'Keep locations under 200 characters'),
    locationAddress: z.string().trim().max(400, 'Keep addresses under 400 characters'),
    locationLat: z.number().nullable(),
    locationLng: z.number().nullable(),
    currencyCode: z.string(),
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
    links: z.array(eventLinkSchema).max(12, 'You can add up to 12 links to an event'),
  })
  .superRefine((values, context) => {
    if (!values.startDate) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Start date is required',
        path: ['startDate'],
      });
    }

    if (!values.endDate) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'End date is required',
        path: ['endDate'],
      });
    }

    if (values.startDate && values.endDate && values.endDate.startOf('day').isBefore(values.startDate.startOf('day'))) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'End date must be on or after the start date',
        path: ['endDate'],
      });
    }

    if (values.startDate && values.endDate && !values.isAllDay) {
      if (!values.startTime) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Start time is required',
          path: ['startTime'],
        });
      }

      if (!values.endTime) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'End time is required',
          path: ['endTime'],
        });
      }

      if (values.startTime && values.endTime) {
        const startDateTime = combineDateAndTime(values.startDate, values.startTime);
        const endDateTime = combineDateAndTime(values.endDate, values.endTime);

        if (!startDateTime || !endDateTime || !endDateTime.isAfter(startDateTime)) {
          context.addIssue({
            code: z.ZodIssueCode.custom,
            message: 'End date and time must be after the start date and time',
            path: ['endTime'],
          });
        }
      }
    }

    if (values.cost > 0 && !normalizeCurrencyCode(values.currencyCode)) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Currency is required when a cost is set',
        path: ['currencyCode'],
      });
    }

    if (values.cost > 0 && normalizeCurrencyCode(values.currencyCode) && !hasValidCurrencyPrecision(values.cost, values.currencyCode)) {
      const minorUnit = getCurrencyMinorUnit(values.currencyCode);
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: `Cost supports up to ${minorUnit} decimal place${minorUnit === 1 ? '' : 's'} for ${normalizeCurrencyCode(values.currencyCode)}.`,
        path: ['cost'],
      });
    }
  });

type ParsedEventFormValues = z.output<typeof eventSchema>;

interface EventFormValues {
  title: string;
  description: string;
  remarks: string;
  category: EventCategory | '';
  color: string;
  isAllDay: boolean;
  startDate: Dayjs | null;
  startTime: Dayjs | null;
  endDate: Dayjs | null;
  endTime: Dayjs | null;
  timezone: string;
  location: string;
  locationAddress: string;
  locationLat: number | null;
  locationLng: number | null;
  currencyCode: string;
  cost: string;
  links: Array<{
    description: string;
    url: string;
  }>;
}

const emptyLink = () => ({
  description: '',
  url: '',
});

const scheduleFieldTextFieldProps = {
  fullWidth: true,
  sx: {
    flex: 1,
    '& input': { fontSize: '0.8rem' },
  },
} as const;

const buildFormDateRange = ({
  isAllDay,
  startDate,
  startTime,
  endDate,
  endTime,
}: Pick<EventFormValues, 'isAllDay' | 'startDate' | 'startTime' | 'endDate' | 'endTime'>) => {
  if (!startDate || !endDate) {
    return null;
  }

  if (isAllDay) {
    return buildAllDayDateTimeRange(startDate, endDate);
  }

  const startDateTime = combineDateAndTime(startDate, startTime);
  const endDateTime = combineDateAndTime(endDate, endTime);

  if (!startDateTime || !endDateTime || !endDateTime.isAfter(startDateTime)) {
    return null;
  }

  return {
    startDateTime: startDateTime.format(),
    endDateTime: endDateTime.format(),
  };
};

const buildDraftRangeValues = (draftRange: { start: string; end: string; allDay: boolean } | null) => {
  if (!draftRange) {
    return null;
  }

  const startDateTime = dayjs(draftRange.start);
  const parsedEndDateTime = dayjs(draftRange.end);

  if (!startDateTime.isValid()) {
    return null;
  }

  if (draftRange.allDay) {
    const startDate = startDateTime.startOf('day');
    const endDate = parsedEndDateTime.isValid() && parsedEndDateTime.isAfter(startDateTime)
      ? parsedEndDateTime.subtract(1, 'day').startOf('day')
      : startDate;

    return {
      isAllDay: true,
      startDate,
      startTime: null,
      endDate,
      endTime: null,
    };
  }

  const endDateTime = parsedEndDateTime.isValid() && parsedEndDateTime.isAfter(startDateTime)
    ? parsedEndDateTime
    : startDateTime.add(DEFAULT_TIMED_SLOT_MINUTES, 'minute');
  const startDate = startDateTime.startOf('day');

  return {
    isAllDay: false,
    startDate,
    startTime: startDateTime,
    endDate: endDateTime.startOf('day'),
    endTime: endDateTime,
  };
};

const buildDefaultValues = (
  event: ItineraryEvent | null,
  draftRange: { start: string; end: string; allDay: boolean } | null,
): EventFormValues => {
  if (event) {
    const startDateTime = dayjs(event.startDateTime);
    const endDateTime = dayjs(event.endDateTime);
    const startDate = startDateTime.startOf('day');
    const endDate = endDateTime.startOf('day');

    return {
      title: event.title,
      description: event.description,
      remarks: event.remarks,
      category: event.category,
      color: normalizeEventColor(event.color),
      isAllDay: event.isAllDay,
      startDate,
      startTime: event.isAllDay ? null : startDateTime,
      endDate,
      endTime: event.isAllDay ? null : endDateTime,
      timezone: event.timezone,
      location: event.location,
      locationAddress: event.locationAddress,
      locationLat: event.locationLat,
      locationLng: event.locationLng,
      currencyCode: event.currencyCode ?? '',
      cost: formatEditableCurrencyAmount(event.cost, event.currencyCode),
      links: event.links.map((link) => ({
        description: link.description,
        url: link.url,
      })),
    };
  }

  const draftRangeValues = buildDraftRangeValues(draftRange);
  if (draftRangeValues) {
    return {
      title: '',
      description: '',
      remarks: '',
      category: '',
      color: '',
      isAllDay: draftRangeValues.isAllDay,
      startDate: draftRangeValues.startDate,
      startTime: draftRangeValues.startTime,
      endDate: draftRangeValues.endDate,
      endTime: draftRangeValues.endTime,
      timezone: '',
      location: '',
      locationAddress: '',
      locationLat: null,
      locationLng: null,
      currencyCode: '',
      cost: '',
      links: [],
    };
  }

  return {
    title: '',
    description: '',
    remarks: '',
    category: '',
    color: '',
    isAllDay: false,
    startDate: null,
    startTime: null,
    endDate: null,
    endTime: null,
    timezone: '',
    location: '',
    locationAddress: '',
    locationLat: null,
    locationLng: null,
    currencyCode: '',
    cost: '',
    links: [],
  };
};

export const EventDrawer = ({
  open,
  itinerary,
  event,
  existingEvents,
  auditHistory,
  draftRange,
  usersMap,
  canManage,
  canDelete,
  onLoadHistory,
  onClose,
  onSave,
  onDelete,
}: EventDrawerProps) => {
  const theme = useTheme();
  const [locationOptions, setLocationOptions] = useState<LocationSuggestion[]>([]);
  const [locationInput, setLocationInput] = useState('');
  const [isSearchingLocations, setIsSearchingLocations] = useState(false);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showFullAuditHistory, setShowFullAuditHistory] = useState(false);
  const [editingLinkIds, setEditingLinkIds] = useState<string[]>([]);
  const [pendingNewLinkEdit, setPendingNewLinkEdit] = useState(false);
  const defaultValues = useMemo(() => buildDefaultValues(event, draftRange), [draftRange, event]);

  const {
    control,
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    trigger,
    formState: { errors, isSubmitting },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventSchema) as unknown as Resolver<EventFormValues>,
    defaultValues,
  });

  const { fields: linkFields, append: appendLink, remove: removeLink } = useFieldArray({
    control,
    name: 'links',
  });

  const locationAddress = watch('locationAddress');
  const [
    selectedCategoryValue,
    selectedColorValue,
    isAllDayValue,
    startDateValue,
    startTimeValue,
    endDateValue,
    endTimeValue,
    selectedCurrencyCode,
  ] = useWatch({
    control,
    name: ['category', 'color', 'isAllDay', 'startDate', 'startTime', 'endDate', 'endTime', 'currencyCode'],
  });
  const selectedCurrencyOption = useMemo(() => getCurrencyOption(selectedCurrencyCode), [selectedCurrencyCode]);
  const headerChipCategory = useMemo(() => {
    return selectedCategoryValue && eventCategoryOptions.includes(selectedCategoryValue as EventCategory)
      ? (selectedCategoryValue as EventCategory)
      : null;
  }, [selectedCategoryValue]);
  const headerChipColor = useMemo(() => {
    return selectedColorValue ? normalizeEventColor(selectedColorValue) : undefined;
  }, [selectedColorValue]);
  const hasEventBeenUpdated = useMemo(() => {
    if (!event) {
      return false;
    }

    return event.updatedBy !== event.createdBy || event.updatedAt !== event.createdAt;
  }, [event]);
  const conflictingEvents = useMemo(() => {
    const range = buildFormDateRange({
      isAllDay: isAllDayValue,
      startDate: startDateValue,
      startTime: startTimeValue,
      endDate: endDateValue,
      endTime: endTimeValue,
    });

    if (!range) {
      return [];
    }

    return findEventConflicts({
      events: existingEvents,
      startDateTime: range.startDateTime,
      endDateTime: range.endDateTime,
      excludeEventId: event?.id,
    });
  }, [endDateValue, endTimeValue, event?.id, existingEvents, isAllDayValue, startDateValue, startTimeValue]);

  useLayoutEffect(() => {
    reset(defaultValues);
    setLocationInput(defaultValues.location);
    setEditingLinkIds([]);
    setPendingNewLinkEdit(false);
  }, [defaultValues, reset]);

  useEffect(() => {
    if (!open) {
      setConfirmDeleteOpen(false);
      setIsDeleting(false);
      setIsSearchingLocations(false);
      setShowFullAuditHistory(false);
      setEditingLinkIds([]);
      setPendingNewLinkEdit(false);
    }
  }, [open]);

  useEffect(() => {
    if (!pendingNewLinkEdit || linkFields.length === 0) {
      return;
    }

    const newestLinkId = linkFields[linkFields.length - 1]?.id;
    if (!newestLinkId) {
      return;
    }

    setEditingLinkIds((current) => (current.includes(newestLinkId) ? current : [...current, newestLinkId]));
    setPendingNewLinkEdit(false);
  }, [linkFields, pendingNewLinkEdit]);

  useEffect(() => {
    if (!open || !event) {
      return;
    }

    setShowFullAuditHistory(false);
    void onLoadHistory(event.id);
  }, [event, onLoadHistory, open]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const controller = new AbortController();
    let active = true;

    setIsSearchingLocations(true);

    const timeoutId = window.setTimeout(() => {
      void searchLocationSuggestions({
        query: locationInput,
        signal: controller.signal,
      })
        .then((results) => {
          if (active) {
            setLocationOptions(results);
          }
        })
        .finally(() => {
          if (active && !controller.signal.aborted) {
            setIsSearchingLocations(false);
          }
        });
    }, 280);

    return () => {
      active = false;
      controller.abort();
      window.clearTimeout(timeoutId);
    };
  }, [locationInput, open]);

  useEffect(() => {
    if (!startDateValue) {
      return;
    }

    if (!endDateValue || endDateValue.isBefore(startDateValue, 'day')) {
      setValue('endDate', startDateValue, { shouldValidate: false, shouldDirty: false });
    }
  }, [endDateValue, setValue, startDateValue]);

  useEffect(() => {
    if (!open || isAllDayValue) {
      return;
    }

    if (startDateValue && (!startTimeValue || !endTimeValue)) {
      const fallbackTimes = buildCurrentFallbackTimeRange(startDateValue, endDateValue ?? startDateValue);

      if (!startTimeValue) {
        setValue('startTime', fallbackTimes.startTime, { shouldValidate: true });
      }

      if (!endTimeValue) {
        setValue('endTime', fallbackTimes.endTime, { shouldValidate: true });
      }

      if (endDateValue && fallbackTimes.endDate.isAfter(endDateValue, 'day')) {
        setValue('endDate', fallbackTimes.endDate, { shouldValidate: true });
      }
    }
  }, [endDateValue, endTimeValue, isAllDayValue, open, setValue, startDateValue, startTimeValue]);

  return (
    <>
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
            {headerChipCategory ? <EventCategoryChip category={headerChipCategory} color={headerChipColor} /> : null}
          </Stack>

          {!canManage ? (
            <Alert severity="info">You can view this event, but only itinerary members can make changes.</Alert>
          ) : null}

          <Stack
            component="form"
            spacing={2.2}
            sx={{ overflowY: 'auto', pr: 0.5 }}
            onSubmit={handleSubmit(async (values) => {
              const parsedValues = eventSchema.parse(values) as ParsedEventFormValues;
              const dateRange = buildFormDateRange(parsedValues);

              if (!dateRange) {
                return;
              }

              await onSave(
                {
                  title: parsedValues.title.trim(),
                  description: parsedValues.description.trim(),
                  remarks: parsedValues.remarks.trim(),
                  category: parsedValues.category as EventCategory,
                  color: normalizeEventColor(parsedValues.color),
                  isAllDay: parsedValues.isAllDay,
                  startDateTime: dateRange.startDateTime,
                  endDateTime: dateRange.endDateTime,
                  timezone: parsedValues.timezone,
                  location: parsedValues.location.trim(),
                  locationAddress: parsedValues.locationAddress.trim(),
                  locationLat: parsedValues.locationLat,
                  locationLng: parsedValues.locationLng,
                  currencyCode: normalizeCurrencyCode(parsedValues.currencyCode),
                  cost: roundCurrencyAmount(parsedValues.cost, parsedValues.currencyCode),
                  links: parsedValues.links.map((link) => ({
                    description: link.description.trim(),
                    url: link.url.trim(),
                  })),
                },
                event?.id,
              );
            })}
          >
            <TextField
              autoFocus={canManage}
              disabled={!canManage}
              error={Boolean(errors.title)}
              helperText={errors.title?.message ?? 'Name the stop clearly, like an airport transfer, check-in, or dinner reservation.'}
              label="Title"
              placeholder="Dinner at Shibuya Yokocho"
              {...register('title')}
            />

            <TextField
              disabled={!canManage}
              error={Boolean(errors.description)}
              helperText={errors.description?.message ?? 'Add reservation notes, meeting details, transport context, or anything the group should know.'}
              label="Description"
              minRows={3}
              multiline
              placeholder="Meet in the hotel lobby at 6:15 PM, confirm reservation under Ava Santos, and keep 15 minutes for the train ride."
              {...register('description')}
            />

            <TextField
              disabled={!canManage}
              error={Boolean(errors.remarks)}
              helperText={errors.remarks?.message ?? 'Use remarks for internal reminders, follow-ups, or extra trip context that may help later.'}
              label="Remarks"
              minRows={2}
              multiline
              placeholder="Bring passport copies, confirm gate details again in the morning, and keep lounge vouchers handy."
              {...register('remarks')}
            />

            <Controller
              control={control}
              name="category"
              render={({ field }) => (
                <TextField
                  disabled={!canManage}
                  error={Boolean(errors.category)}
                  helperText={errors.category?.message ?? 'Choose the stop type so the calendar and trip cost summary stay organized.'}
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

            <Stack spacing={1}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.2} sx={{ alignItems: { sm: 'flex-start' } }}>
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
                          ...scheduleFieldTextFieldProps,
                          error: Boolean(errors.startDate),
                          helperText: errors.startDate?.message,
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
                      disabled={!canManage}
                      format="MM/DD/YYYY"
                      label="End date"
                      minDate={startDateValue ?? undefined}
                      onChange={field.onChange}
                      slotProps={{
                        textField: {
                          ...scheduleFieldTextFieldProps,
                          error: Boolean(errors.endDate),
                          helperText: errors.endDate?.message,
                        },
                      }}
                      value={field.value}
                    />
                  )}
                />
              </Stack>

              {!isAllDayValue ? (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.2} sx={{ alignItems: { sm: 'flex-start' } }}>
                  <Controller
                    control={control}
                    name="startTime"
                    render={({ field }) => (
                      <TimePicker
                        ampm
                        disabled={!canManage}
                        label="Start time"
                        onChange={field.onChange}
                        timeSteps={{ minutes: 15 }}
                        slotProps={{
                          textField: {
                            ...scheduleFieldTextFieldProps,
                            error: Boolean(errors.startTime),
                            helperText: errors.startTime?.message,
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
                        timeSteps={{ minutes: 15 }}
                        slotProps={{
                          textField: {
                            ...scheduleFieldTextFieldProps,
                            error: Boolean(errors.endTime),
                            helperText: errors.endTime?.message,
                          },
                        }}
                        value={field.value}
                      />
                    )}
                  />
                </Stack>
              ) : null}

              <Controller
                control={control}
                name="isAllDay"
                render={({ field }) => (
                  <FormControlLabel
                    control={<Checkbox checked={field.value} disabled={!canManage} onChange={(_, checked) => field.onChange(checked)} />}
                    label={
                      <Typography color="text.primary" variant="body2">
                        All day
                      </Typography>
                    }
                    sx={{ ml: 0, mr: 0, mt: 0.2, '& .MuiFormControlLabel-label': { fontWeight: 500 } }}
                  />
                )}
              />
            </Stack>

            {!isSubmitting && conflictingEvents.length > 0 ? (
              <Alert severity="warning">
                <Stack spacing={0.8}>
                  <Typography fontWeight={700} variant="body2">
                    Date conflict detected
                  </Typography>
                  <Typography variant="body2">
                    This event overlaps with {conflictingEvents.length} existing {conflictingEvents.length === 1 ? 'event' : 'events'} in
                    the itinerary.
                  </Typography>
                  <Stack spacing={0.5}>
                    {conflictingEvents.slice(0, 3).map((conflict) => (
                      <Typography key={conflict.id} variant="caption">
                        {conflict.title} • {formatEventSchedule(conflict)}
                      </Typography>
                    ))}
                    {conflictingEvents.length > 3 ? (
                      <Typography variant="caption">+{conflictingEvents.length - 3} more overlapping events</Typography>
                    ) : null}
                  </Stack>
                </Stack>
              </Alert>
            ) : null}

          <Controller
            control={control}
            name="timezone"
            render={({ field }) => (
              <Autocomplete
                disabled={!canManage}
                filterOptions={(options, state) => {
                  const query = state.inputValue.trim().toLowerCase();
                  if (!query) {
                    return options;
                  }

                  return options.filter((option) => {
                    const rawValue = option.toLowerCase();
                    const friendlyValue = formatTimezoneLabel(option).toLowerCase();
                    const displayValue = formatTimezoneDisplayLabel(option).toLowerCase();

                    return rawValue.includes(query) || friendlyValue.includes(query) || displayValue.includes(query);
                  });
                }}
                getOptionLabel={(option) => formatTimezoneDisplayLabel(option)}
                isOptionEqualToValue={(option, value) => option === value}
                noOptionsText="No matching timezone"
                onChange={(_, value) => field.onChange(value ?? '')}
                options={timezoneOptions}
                renderOption={(props, option) => (
                  <Box component="li" {...props}>
                    <Typography variant="body2">{formatTimezoneDisplayLabel(option)}</Typography>
                  </Box>
                )}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    error={Boolean(errors.timezone)}
                    helperText={errors.timezone?.message ?? 'Search the destination timezone from the full IANA timezone list.'}
                    label="Timezone"
                    placeholder="Search timezone or city"
                  />
                )}
                value={field.value || null}
              />
            )}
          />

          <Controller
            control={control}
            name="location"
            render={({ field }) => (
              <Autocomplete
                disabled={!canManage}
                filterOptions={(options) => options}
                freeSolo
                getOptionLabel={(option) => (typeof option === 'string' ? option : option.name)}
                inputValue={locationInput}
                loading={isSearchingLocations}
                loadingText="Searching places..."
                noOptionsText={locationInput.trim() ? 'No matching places' : 'Start typing to search places'}
                onChange={(_, value) => {
                  if (!value) {
                    field.onChange('');
                    setLocationInput('');
                    setValue('locationAddress', '');
                    setValue('locationLat', null);
                    setValue('locationLng', null);
                    return;
                  }

                  if (typeof value === 'string') {
                    field.onChange(value);
                    setLocationInput(value);
                    setValue('locationAddress', value);
                    setValue('locationLat', null);
                    setValue('locationLng', null);
                    return;
                  }

                  if (value) {
                    field.onChange(value.name);
                    setLocationInput(value.name);
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
                    placeholder="Search for a hotel, airport, restaurant, landmark, or neighborhood"
                    helperText={
                      errors.location?.message ??
                      (isGeoapifyConfigured
                        ? 'Find the exact stop and select a result to fill the address automatically.'
                        : 'Search real places when Geoapify is configured, or use the built-in trip locations for local development.')
                    }
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

          <TextField
            disabled={!canManage}
            helperText="Filled automatically from the place you selected above."
            label="Matched address"
            placeholder="Selected address details will appear here"
            value={locationAddress}
            {...register('locationAddress')}
          />

          <Stack spacing={1}>
            <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={2}>
              <Stack alignItems="center" direction="row" spacing={0.9}>
                <Link2 size={15} />
                <Typography fontWeight={600} variant="body2">
                  Links
                </Typography>
              </Stack>
              {canManage ? (
                <Button
                  onClick={() => {
                    appendLink(emptyLink());
                    setPendingNewLinkEdit(true);
                  }}
                  startIcon={<Plus size={14} />}
                  type="button"
                  variant="text"
                >
                  Add link
                </Button>
              ) : null}
            </Stack>

            {linkFields.length === 0 ? (
              <Typography color="text.secondary" variant="caption">
                No links added yet.
              </Typography>
            ) : (
              <Stack spacing={1}>
                {linkFields.map((field, index) => {
                  const linkDescription = watch(`links.${index}.description`);
                  const linkUrl = watch(`links.${index}.url`);

                  return (
                    <Box
                      key={field.id}
                      sx={{
                        border: `1px solid ${alpha(theme.palette.divider, 0.6)}`,
                        borderRadius: theme.app.radius.md,
                        px: 2,
                        py: 1.5,
                      }}
                    >
                      {editingLinkIds.includes(field.id) ? (
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ alignItems: { sm: 'flex-start' } }}>
                          <TextField
                            disabled={!canManage}
                            error={Boolean(errors.links?.[index]?.description)}
                            helperText={errors.links?.[index]?.description?.message}
                            label="Description"
                            placeholder="Booking confirmation"
                            size="small"
                            sx={{ flex: { sm: '0 0 36%' } }}
                            {...register(`links.${index}.description` as const)}
                          />
                          <TextField
                            disabled={!canManage}
                            error={Boolean(errors.links?.[index]?.url)}
                            helperText={errors.links?.[index]?.url?.message}
                            label="URL"
                            placeholder="https://example.com/booking/ABC123"
                            size="small"
                            sx={{ flex: 1 }}
                            {...register(`links.${index}.url` as const)}
                          />
                          {canManage ? (
                            <Stack direction="row" spacing={0.25} sx={{ mt: { sm: 0.25 } }}>
                              <IconButton
                                color="primary"
                                onClick={async () => {
                                  const isValid = await trigger([
                                    `links.${index}.description`,
                                    `links.${index}.url`,
                                  ]);

                                  if (isValid) {
                                    setEditingLinkIds((current) => current.filter((id) => id !== field.id));
                                  }
                                }}
                                type="button"
                              >
                                <Check size={16} />
                              </IconButton>
                              <IconButton
                                color="error"
                                onClick={() => {
                                  removeLink(index);
                                  setEditingLinkIds((current) => current.filter((id) => id !== field.id));
                                }}
                                type="button"
                              >
                                <Trash2 size={16} />
                              </IconButton>
                            </Stack>
                          ) : null}
                        </Stack>
                      ) : (
                        <Stack alignItems="flex-start" direction="row" justifyContent="space-between" spacing={1.2}>
                          <Stack minWidth={0} spacing={0.35} sx={{ flex: 1 }}>
                            <Typography fontWeight={600} variant="body2">
                              {linkDescription || 'Untitled link'}
                            </Typography>
                            <Typography
                              color={linkUrl ? 'primary.main' : 'text.secondary'}
                              component={linkUrl ? 'a' : 'span'}
                              href={linkUrl || undefined}
                              rel={linkUrl ? 'noreferrer' : undefined}
                              sx={{
                                overflow: 'hidden',
                                textDecoration: 'none',
                                textOverflow: 'ellipsis',
                                whiteSpace: 'nowrap',
                                '&:hover': linkUrl
                                  ? {
                                      textDecoration: 'underline',
                                    }
                                  : undefined,
                              }}
                              target={linkUrl ? '_blank' : undefined}
                              variant="caption"
                            >
                              {linkUrl || 'No URL yet'}
                            </Typography>
                          </Stack>
                          {canManage ? (
                            <Stack direction="row" spacing={0.25}>
                              <IconButton
                                color="primary"
                                onClick={() => setEditingLinkIds((current) => (current.includes(field.id) ? current : [...current, field.id]))}
                                type="button"
                              >
                                <Edit3 size={16} />
                              </IconButton>
                              <IconButton color="error" onClick={() => removeLink(index)} type="button">
                                <Trash2 size={16} />
                              </IconButton>
                            </Stack>
                          ) : null}
                        </Stack>
                      )}
                    </Box>
                  );
                })}
              </Stack>
            )}

            {typeof errors.links?.message === 'string' ? (
              <Typography color="error.main" variant="caption">
                {errors.links.message}
              </Typography>
            ) : null}
          </Stack>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <Controller
              control={control}
              name="currencyCode"
              render={({ field }) => (
                <Autocomplete
                  disabled={!canManage}
                  filterOptions={(options, state) => {
                    const query = state.inputValue.trim().toLowerCase();
                    if (!query) {
                      return options;
                    }

                    return options.filter((option) =>
                      option.code.toLowerCase().includes(query) || option.name.toLowerCase().includes(query),
                    );
                  }}
                  getOptionLabel={(option) => getCurrencyOptionLabel(option.code)}
                  isOptionEqualToValue={(option, value) => option.code === value.code}
                  noOptionsText="No matching currency"
                  onChange={(_, value) => field.onChange(value?.code ?? '')}
                  options={supportedCurrencies}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      error={Boolean(errors.currencyCode)}
                      helperText={errors.currencyCode?.message ?? 'Search by ISO code or currency name.'}
                      label="Currency"
                      placeholder="Search currency"
                    />
                  )}
                  renderOption={(props, option) => (
                    <Box component="li" {...props}>
                      <Stack spacing={0.2}>
                        <Typography variant="body2">{getCurrencyOptionLabel(option.code)}</Typography>
                        <Typography color="text.secondary" variant="caption">
                          {option.minorUnit} decimal place{option.minorUnit === 1 ? '' : 's'}
                        </Typography>
                      </Stack>
                    </Box>
                  )}
                  sx={{ flex: 1 }}
                  value={selectedCurrencyOption}
                />
              )}
            />

            <Controller
              control={control}
              name="cost"
              render={({ field }) => (
                <TextField
                  disabled={!canManage}
                  error={Boolean(errors.cost)}
                  helperText={
                    errors.cost?.message ??
                    `Optional budget estimate for this stop, booking, or activity${selectedCurrencyOption ? ` in ${selectedCurrencyOption.code}` : ''}.`
                  }
                  inputProps={{
                    inputMode: 'decimal',
                    pattern: `[0-9]+([.][0-9]{0,${getCurrencyMinorUnit(selectedCurrencyCode)}})?`,
                  }}
                  label="Cost"
                  slotProps={{
                    input: {
                      startAdornment: selectedCurrencyCode ? (
                        <InputAdornment position="start">{selectedCurrencyCode}</InputAdornment>
                      ) : undefined,
                    },
                  }}
                  onChange={field.onChange}
                  onBlur={(blurEvent) => {
                    field.onBlur();

                    const rawValue = blurEvent.target.value.trim();
                    if (!rawValue) {
                      return;
                    }

                    const parsedValue = Number(rawValue);
                    if (Number.isNaN(parsedValue)) {
                      return;
                    }

                    field.onChange(formatEditableCurrencyAmount(parsedValue, selectedCurrencyCode));
                  }}
                  placeholder={getCurrencyMinorUnit(selectedCurrencyCode) > 0 ? '0.00' : '0'}
                  sx={{ flex: 1.05 }}
                  type="text"
                  value={field.value}
                />
              )}
            />
          </Stack>

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
                {hasEventBeenUpdated ? (
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
                ) : null}
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
                    {(showFullAuditHistory ? auditHistory : auditHistory.slice(0, 3)).map((historyItem) => (
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
                    {auditHistory.length > 3 ? (
                      <Button
                        onClick={() => setShowFullAuditHistory((current) => !current)}
                        sx={{ alignSelf: 'flex-start', px: 0.5 }}
                        variant="text"
                      >
                        {showFullAuditHistory ? 'See less' : `See more (${auditHistory.length - 3} more)`}
                      </Button>
                    ) : null}
                  </Stack>
                ) : null}
              </Stack>
            </>
          ) : null}

          <Box sx={{ mt: 'auto' }} />

          <Stack direction="row" justifyContent="space-between" pt={1}>
            <Box>
              {event && canDelete ? (
                <IconButton color="error" onClick={() => setConfirmDeleteOpen(true)}>
                  <Trash2 size={18} />
                </IconButton>
              ) : null}
            </Box>
            <Stack direction="row" spacing={1.2}>
              <Button onClick={onClose}>Cancel</Button>
              <Button disabled={!canManage} loading={isSubmitting} type="submit" variant="contained">
                {event ? 'Save changes' : 'Create event'}
              </Button>
            </Stack>
          </Stack>
          </Stack>
        </Stack>
      </Drawer>

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
            {event
              ? `This will permanently remove "${event.title}" from the itinerary and add a deleted entry to its audit history.`
              : 'This event will be permanently removed from the itinerary.'}
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
              if (!event) {
                return;
              }

              setIsDeleting(true);

              try {
                await onDelete(event.id);
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
