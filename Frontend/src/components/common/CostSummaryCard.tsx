import { Box, Card, CardContent, Stack, Tooltip, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import {
  formatCurrencyAmount,
  formatCurrencySummary,
  getCostTotalsByCurrency,
  getCurrencyOptionLabel,
  normalizeCurrencyCode,
} from '../../lib/currency';
import { eventCategoryMeta, getDefaultEventColor } from '../../lib/events';
import type { ItineraryEvent } from '../../types/event';

interface CostSummaryCardProps {
  events: ItineraryEvent[];
}

export const CostSummaryCard = ({ events }: CostSummaryCardProps) => {
  const currencyTotals = getCostTotalsByCurrency(events);
  const sections = buildCostSummarySections(events);

  return (
    <Card>
      <CardContent>
        <Stack spacing={2.5}>
          <Box>
            <Typography color="text.secondary" variant="body2">
              Trip cost summary
            </Typography>
            <Typography mt={1} variant="h5">
              {formatCurrencySummary(currencyTotals, {
                emptyLabel: 'No costed events yet',
                maxVisible: 3,
              })}
            </Typography>
          </Box>

          {sections.length === 0 ? (
            <Typography color="text.secondary" variant="body2">
              Add estimated costs to events to see a breakdown by currency and category.
            </Typography>
          ) : (
            <Stack spacing={2.2}>
              {sections.map((section) => (
                <Stack key={section.currencyCode} spacing={1.4}>
                  <Stack direction="row" justifyContent="space-between" spacing={1.5}>
                    <Typography variant="body2">{getCurrencyOptionLabel(section.currencyCode)}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {formatCurrencyAmount(section.total, section.currencyCode)}
                    </Typography>
                  </Stack>

                  <Stack spacing={1.6}>
                    {section.rows.map((row) => {
                      const meta = eventCategoryMeta[row.category];
                      const ratio = section.total > 0 ? (row.amount / section.total) * 100 : 0;

                      return (
                        <Box key={`${section.currencyCode}-${row.category}`}>
                          <Stack direction="row" justifyContent="space-between" mb={0.8}>
                            <Typography variant="body2">{meta?.label ?? row.category}</Typography>
                            <Typography color="text.secondary" variant="body2">
                              {formatCurrencyAmount(row.amount, section.currencyCode)}
                            </Typography>
                          </Stack>
                          <BarTrack>
                            {row.segments.map((segment) => (
                              <Tooltip
                                key={segment.eventId}
                                title={`${segment.title} • ${formatCurrencyAmount(segment.amount, section.currencyCode)}`}
                              >
                                <BarSegment
                                  sx={{
                                    width: `${section.total > 0 ? (segment.amount / section.total) * 100 : 0}%`,
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
              ))}
            </Stack>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

const buildCostSummarySections = (events: ItineraryEvent[]) =>
  Array.from(
    events.reduce<
      Map<
        string,
        {
          currencyCode: string;
          total: number;
          rows: Map<
            ItineraryEvent['category'],
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
          >;
        }
      >
    >((accumulator, event) => {
      const currencyCode = normalizeCurrencyCode(event.currencyCode);
      if (!currencyCode || event.cost <= 0) {
        return accumulator;
      }

      const section = accumulator.get(currencyCode) ?? {
        currencyCode,
        total: 0,
        rows: new Map(),
      };

      const row = section.rows.get(event.category) ?? {
        category: event.category,
        amount: 0,
        segments: [],
      };

      row.amount += event.cost;
      row.segments.push({
        eventId: event.id,
        title: event.title,
        amount: event.cost,
        color: event.color || getDefaultEventColor(event.category),
      });

      section.total += event.cost;
      section.rows.set(event.category, row);
      accumulator.set(currencyCode, section);
      return accumulator;
    }, new Map()),
  )
    .map(([, section]) => ({
      currencyCode: section.currencyCode,
      total: section.total,
      rows: Array.from(section.rows.values())
        .map((row) => ({
          ...row,
          segments: [...row.segments].sort((left, right) => right.amount - left.amount),
        }))
        .sort((left, right) => right.amount - left.amount),
    }))
    .sort((left, right) => right.total - left.total);

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
