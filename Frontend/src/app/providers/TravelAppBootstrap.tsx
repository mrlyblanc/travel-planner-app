import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material';
import { useEffect, useMemo, useRef } from 'react';
import type { PropsWithChildren } from 'react';
import { useTravelStore } from '../store/useTravelStore';
import { itineraryRealtimeClient } from '../../lib/realtime';

export const TravelAppBootstrap = ({ children }: PropsWithChildren) => {
  const bootstrap = useTravelStore((state) => state.bootstrap);
  const refreshAll = useTravelStore((state) => state.refreshAll);
  const refreshItineraryBundle = useTravelStore((state) => state.refreshItineraryBundle);
  const accessToken = useTravelStore((state) => state.accessToken);
  const itineraries = useTravelStore((state) => state.itineraries);
  const isBootstrapping = useTravelStore((state) => state.isBootstrapping);
  const isReady = useTravelStore((state) => state.isReady);
  const error = useTravelStore((state) => state.error);
  const bootstrappedRef = useRef(false);
  const itineraryIds = useMemo(() => itineraries.map((itinerary) => itinerary.id), [itineraries]);

  const itineraryKey = useMemo(() => itineraryIds.join('|'), [itineraryIds]);

  useEffect(() => {
    if (bootstrappedRef.current) {
      return;
    }

    bootstrappedRef.current = true;
    void bootstrap();
  }, [bootstrap]);

  useEffect(() => {
    if (!accessToken || !isReady) {
      void itineraryRealtimeClient.disconnect();
      return;
    }

    let active = true;

    void itineraryRealtimeClient
      .connect(accessToken, (notification) => {
        if (!active) {
          return;
        }

        if (notification.type === 'itinerary.created') {
          void refreshAll();
          return;
        }

        void refreshItineraryBundle(notification.itineraryId);
      })
      .then(() => itineraryRealtimeClient.syncItineraries(itineraryIds))
      .catch(() => {
        // Keep the app usable even if realtime fails to connect.
      });

    return () => {
      active = false;
      void itineraryRealtimeClient.disconnect();
    };
  }, [accessToken, isReady, refreshAll, refreshItineraryBundle, itineraryKey, itineraryIds]);

  if (!isReady) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '100vh', px: 3 }}>
        <Stack alignItems="center" spacing={2.5} sx={{ maxWidth: 480, textAlign: 'center' }}>
          {error ? (
            <>
              <Alert severity="error" sx={{ width: '100%' }}>
                {error}
              </Alert>
              <Typography color="text.secondary" variant="body2">
                Make sure the backend API is running and seeded, then try again.
              </Typography>
              <Button onClick={() => void bootstrap()} variant="contained">
                Retry connection
              </Button>
            </>
          ) : (
            <>
              <CircularProgress />
              <Typography variant="h6">Connecting to TravelPlannerApp backend</Typography>
              <Typography color="text.secondary" variant="body2">
                Restoring your session, then syncing itineraries, members, and events from the backend.
              </Typography>
            </>
          )}
          {isBootstrapping ? <Typography color="text.secondary" variant="caption">Loading workspace…</Typography> : null}
        </Stack>
      </Box>
    );
  }

  return <>{children}</>;
};
