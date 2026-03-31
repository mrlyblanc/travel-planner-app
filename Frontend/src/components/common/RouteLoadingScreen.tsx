import { Box, CircularProgress, Stack, Typography } from '@mui/material';

interface RouteLoadingScreenProps {
  title?: string;
  description?: string;
  minHeight?: number | string;
}

export const RouteLoadingScreen = ({
  title = 'Loading workspace',
  description = 'Preparing the next view and its travel data.',
  minHeight = '40vh',
}: RouteLoadingScreenProps) => (
  <Box sx={{ display: 'grid', placeItems: 'center', minHeight, px: 3 }}>
    <Stack alignItems="center" spacing={1.4} sx={{ textAlign: 'center', maxWidth: 360 }}>
      <CircularProgress size={26} />
      <Typography fontWeight={700} variant="body1">
        {title}
      </Typography>
      <Typography color="text.secondary" variant="body2">
        {description}
      </Typography>
    </Stack>
  </Box>
);
