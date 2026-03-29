import type { ReactNode } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';
import { alpha, styled } from '@mui/material/styles';

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
  <EmptyStateContainer alignItems="center" justifyContent="center" spacing={2}>
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
  </EmptyStateContainer>
);

const EmptyStateContainer = styled(Stack)(({ theme }) => ({
  minHeight: 220,
  padding: theme.spacing(4),
  textAlign: 'center',
  borderRadius: theme.app.radius.md,
  border: `1px dashed ${alpha(theme.palette.primary.main, theme.palette.mode === 'light' ? 0.28 : 0.34)}`,
  backgroundColor:
    theme.palette.mode === 'light'
      ? alpha('#ffffff', 0.64)
      : alpha('#ffffff', 0.05),
  boxShadow:
    theme.palette.mode === 'light'
      ? 'inset 0 1px 0 rgba(255,255,255,0.45)'
      : 'inset 0 1px 0 rgba(255,255,255,0.04)',
}));
