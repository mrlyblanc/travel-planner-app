import { Box, Card, CardContent, LinearProgress, Stack, Typography } from '@mui/material';
import { eventCategoryMeta } from '../../lib/events';
import { currencyFormatter } from '../../lib/utils';

interface CostSummaryCardProps {
  total: number;
  byCategory: Record<string, number>;
}

export const CostSummaryCard = ({ total, byCategory }: CostSummaryCardProps) => {
  const rows = Object.entries(byCategory).sort(([, left], [, right]) => right - left);

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
            {rows.map(([category, amount]) => {
              const meta = eventCategoryMeta[category as keyof typeof eventCategoryMeta];
              const ratio = total > 0 ? (amount / total) * 100 : 0;

              return (
                <Box key={category}>
                  <Stack direction="row" justifyContent="space-between" mb={0.8}>
                    <Typography variant="body2">{meta?.label ?? category}</Typography>
                    <Typography color="text.secondary" variant="body2">
                      {currencyFormatter.format(amount)}
                    </Typography>
                  </Stack>
                  <LinearProgress
                    sx={{
                      height: 8,
                      borderRadius: 10,
                      bgcolor: 'rgba(43, 110, 220, 0.08)',
                      '& .MuiLinearProgress-bar': {
                        bgcolor: meta?.color ?? 'primary.main',
                      },
                    }}
                    value={ratio}
                    variant="determinate"
                  />
                </Box>
              );
            })}
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};
