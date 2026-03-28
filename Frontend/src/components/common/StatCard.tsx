import type { ReactNode } from 'react';
import { Box, Card, CardContent, Stack, Typography } from '@mui/material';

interface StatCardProps {
  label: string;
  value: string;
  helper: string;
  icon: ReactNode;
}

export const StatCard = ({ label, value, helper, icon }: StatCardProps) => (
  <Card>
    <CardContent>
      <Stack direction="row" justifyContent="space-between" spacing={2}>
        <Box>
          <Typography color="text.secondary" variant="body2">
            {label}
          </Typography>
          <Typography mt={1} variant="h5">
            {value}
          </Typography>
          <Typography color="text.secondary" mt={1} variant="body2">
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
