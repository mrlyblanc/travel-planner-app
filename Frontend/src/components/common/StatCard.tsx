import type { ReactNode } from 'react';
import { Box, Card, CardContent, Stack, Typography } from '@mui/material';

interface StatCardProps {
  label: string;
  value: ReactNode;
  valueMeta?: ReactNode;
  helper: string;
  icon: ReactNode;
}

export const StatCard = ({ label, value, valueMeta, helper, icon }: StatCardProps) => (
  <Card sx={{ height: '100%', minHeight: 192 }}>
    <CardContent sx={{ height: '100%' }}>
      <Stack direction="row" justifyContent="space-between" spacing={2} sx={{ height: '100%' }}>
        <Box sx={{ minWidth: 0, flex: 1, display: 'flex', flexDirection: 'column' }}>
          <Typography color="text.secondary" variant="body2">
            {label}
          </Typography>
          {typeof value === 'string' || typeof value === 'number' ? (
            <Typography mt={1} variant="h5">
              {value}
            </Typography>
          ) : (
            <Box mt={1}>{value}</Box>
          )}
          <Box sx={{ minHeight: 44, mt: 0.6 }}>
            {valueMeta ? (
              <Typography color="text.secondary" variant="body2">
                {valueMeta}
              </Typography>
            ) : null}
          </Box>
          <Typography color="text.secondary" mt="auto" variant="body2">
            {helper}
          </Typography>
        </Box>
        <Box
          sx={{
            alignSelf: 'flex-start',
            display: 'grid',
            placeItems: 'center',
            width: 44,
            height: 44,
            borderRadius: 3,
            bgcolor: 'rgba(43, 110, 220, 0.09)',
            color: 'primary.main',
          }}
        >
          {icon}
        </Box>
      </Stack>
    </CardContent>
  </Card>
);
