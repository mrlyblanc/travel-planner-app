import { Chip } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { eventCategoryMeta, normalizeEventColor } from '../../lib/events';
import type { EventCategory } from '../../types/event';

export const EventCategoryChip = ({
  category,
  color,
}: {
  category: EventCategory;
  color?: string;
}) => {
  const meta = eventCategoryMeta[category];
  const chipColor = normalizeEventColor(color) || meta.color;

  return (
    <Chip
      label={meta.label}
      size="small"
      sx={(theme) => ({
        bgcolor:
          color
            ? alpha(chipColor, theme.palette.mode === 'light' ? 0.14 : 0.22)
            : meta.softColor,
        color: chipColor,
        border: `1px solid ${alpha(chipColor, theme.palette.mode === 'light' ? 0.22 : 0.28)}`,
      })}
    />
  );
};
