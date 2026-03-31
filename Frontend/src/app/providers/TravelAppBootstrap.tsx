import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material';
import { useEffect, useMemo, useRef } from 'react';
import type { PropsWithChildren } from 'react';
import { useToast } from './ToastProvider';
import { useTravelStore } from '../store/useTravelStore';
import { itineraryRealtimeClient } from '../../lib/realtime';

export const TravelAppBootstrap = ({ children }: PropsWithChildren) => {
  const bootstrap = useTravelStore((state) => state.bootstrap);
  const refreshAll = useTravelStore((state) => state.refreshAll);
  const refreshItineraryBundle = useTravelStore((state) => state.refreshItineraryBundle);
  const addRealtimeNotification = useTravelStore((state) => state.addRealtimeNotification);
  const accessToken = useTravelStore((state) => state.accessToken);
  const itineraries = useTravelStore((state) => state.itineraries);
  const isBootstrapping = useTravelStore((state) => state.isBootstrapping);
  const isReady = useTravelStore((state) => state.isReady);
  const error = useTravelStore((state) => state.error);
  const { showToast } = useToast();
  const bootstrappedRef = useRef(false);
  const itineraryIdsRef = useRef<string[]>([]);
  const itineraryIds = useMemo(() => itineraries.map((itinerary) => itinerary.id), [itineraries]);
  const itineraryKey = useMemo(() => itineraryIds.join('|'), [itineraryIds]);

  useEffect(() => {
    itineraryIdsRef.current = itineraryIds;
  }, [itineraryIds]);

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
    let reconnectTimeoutId: number | null = null;

    const connectRealtime = async () => {
      try {
        await itineraryRealtimeClient.connect(accessToken, {
          onItineraryNotification: (notification) => {
            if (!active) {
              return;
            }

            if (notification.type === 'itinerary.created') {
              void refreshAll();
              return;
            }

            void refreshItineraryBundle(notification.itineraryId);
          },
          onUserNotification: (notification) => {
            if (!active) {
              return;
            }

            addRealtimeNotification(notification);
            showToast(notification.title, 'info');

            if (notification.type === 'itinerary.member.added' || notification.type === 'itinerary.member.removed') {
              void refreshAll().then(() => {
                if (!notification.itineraryId) {
                  return;
                }

                const nextState = useTravelStore.getState();
                if (!nextState.itineraries.some((itinerary) => itinerary.id === notification.itineraryId)) {
                  return;
                }

                void refreshItineraryBundle(notification.itineraryId).catch(() => undefined);
              });
              return;
            }

            if (notification.itineraryId) {
              void refreshItineraryBundle(notification.itineraryId);
            }
          },
        });

        if (!active) {
          return;
        }

        await itineraryRealtimeClient.syncItineraries(itineraryIdsRef.current);
      } catch {
        if (!active) {
          return;
        }

        reconnectTimeoutId = window.setTimeout(() => {
          void connectRealtime();
        }, 2500);
      }
    };

    void connectRealtime();

    return () => {
      active = false;
      if (reconnectTimeoutId) {
        window.clearTimeout(reconnectTimeoutId);
      }
      void itineraryRealtimeClient.disconnect();
    };
  }, [accessToken, addRealtimeNotification, isReady, refreshAll, refreshItineraryBundle, showToast]);

  useEffect(() => {
    if (!accessToken || !isReady) {
      return;
    }

    void itineraryRealtimeClient.syncItineraries(itineraryIdsRef.current).catch(() => {
      // Best effort while the connection is establishing or reconnecting.
    });
  }, [accessToken, isReady, itineraryKey]);

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
                Restoring your session and loading your itinerary workspace. Trip details will stream in as you open or browse them.
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
