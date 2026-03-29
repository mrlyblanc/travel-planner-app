import { Box, Card, CardContent, Stack, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { eventCategoryMeta, getDefaultEventColor } from '../../lib/events';
import { currencyFormatter } from '../../lib/utils';
import type { ItineraryEvent } from '../../types/event';

interface CostSummaryCardProps {
  total: number;
  events: ItineraryEvent[];
}

export const CostSummaryCard = ({ total, events }: CostSummaryCardProps) => {
  const rows = buildCostSummaryRows(events);

  return (
    <Card>
      <CardContent>
        <Stack spacing={2.5}>
          <Box>
            <Typography color="text.secondary" variant="body2">
              Trip cost summary
            </Typography>
            <Typography mt={1} variant="h5">
              {currencyFormatter.format(total)}
            </Typography>
          </Box>

          <Stack spacing={1.6}>
            {rows.map((row) => {
              const meta = eventCategoryMeta[row.category];
              const ratio = total > 0 ? (row.amount / total) * 100 : 0;

              return (
                <Box key={row.category}>
                  <Stack direction="row" justifyContent="space-between" mb={0.8}>
                    <Typography variant="body2">{meta?.label ?? row.category}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {currencyFormatter.format(row.amount)}
                    </Typography>
                  </Stack>
                  <BarTrack>
                    {row.segments.map((segment) => (
                      <Tooltip
                        key={segment.eventId}
                        title={`${segment.title} • ${currencyFormatter.format(segment.amount)}`}
                      >
                        <BarSegment
                          sx={{
                            width: `${total > 0 ? (segment.amount / total) * 100 : 0}%`,
                            bgcolor: segment.color,
                          }}
                        />
                      </Tooltip>
                    ))}
                    <BarRemainder sx={{ width: `${Math.max(0, 100 - ratio)}%` }} />
                  </BarTrack>
                </Box>
              );
            })}
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

const buildCostSummaryRows = (events: ItineraryEvent[]) =>
  Object.values(
    events.reduce<
      Record<
        string,
        {
          category: ItineraryEvent['category'];
          amount: number;
          segments: Array<{
            eventId: string;
            title: string;
            amount: number;
            color: string;
          }>;
        }
      >
    >((accumulator, event) => {
      const existingRow = accumulator[event.category] ?? {
        category: event.category,
        amount: 0,
        segments: [],
      };

      existingRow.amount += event.cost;
      existingRow.segments.push({
        eventId: event.id,
        title: event.title,
        amount: event.cost,
        color: event.color || getDefaultEventColor(event.category),
      });

      accumulator[event.category] = existingRow;
      return accumulator;
    }, {}),
  )
    .map((row) => ({
      ...row,
      segments: [...row.segments].sort((left, right) => right.amount - left.amount),
    }))
    .sort((left, right) => right.amount - left.amount);

const BarTrack = ({ children }: { children: React.ReactNode }) => (
  <Box
    sx={{
      display: 'flex',
      overflow: 'hidden',
      height: 8,
      borderRadius: '999px',
      bgcolor: 'rgba(43, 110, 220, 0.08)',
    }}
  >
    {children}
  </Box>
);

const BarSegment = ({ sx }: { sx?: object }) => (
  <Box
    sx={{
      flexShrink: 0,
      minWidth: 6,
      ...sx,
    }}
  />
);

const BarRemainder = ({ sx }: { sx?: object }) => (
  <Box
    sx={{
      flexShrink: 0,
      bgcolor: (theme) =>
        theme.palette.mode === 'light'
          ? alpha(theme.palette.primary.main, 0.08)
          : alpha(theme.palette.primary.main, 0.14),
      ...sx,
    }}
  />
);
