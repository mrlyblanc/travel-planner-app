import type { ReactNode } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';

interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
}

export const EmptyState = ({
  icon,
  title,
  description,
  actionLabel,
  onAction,
}: EmptyStateProps) => (
  <Stack
    alignItems="center"
    justifyContent="center"
    spacing={2}
    sx={{
      minHeight: 220,
      borderRadius: 4,
      border: '1px dashed rgba(122, 148, 180, 0.34)',
      bgcolor: 'rgba(255, 255, 255, 0.56)',
      p: 4,
      textAlign: 'center',
    }}
  >
    <Box sx={{ color: 'primary.main' }}>{icon}</Box>
    <Typography variant="h6">{title}</Typography>
    <Typography color="text.secondary" maxWidth={360}>
      {description}
    </Typography>
    {actionLabel && onAction ? (
      <Button onClick={onAction} variant="contained">
        {actionLabel}
      </Button>
    ) : null}
  </Stack>
);
