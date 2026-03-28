import { Chip } from '@mui/material';
import { eventCategoryMeta } from '../../lib/events';
import type { EventCategory } from '../../types/event';

export const EventCategoryChip = ({ category }: { category: EventCategory }) => {
  const meta = eventCategoryMeta[category];

  return (
    <Chip
      label={meta.label}
      size="small"
      sx={{
        bgcolor: meta.softColor,
        color: meta.color,
      }}
    />
  );
};
