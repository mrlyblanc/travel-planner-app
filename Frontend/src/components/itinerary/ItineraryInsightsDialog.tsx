import { X } from 'lucide-react';
import {
  Box,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Tab,
  Tabs,
  Typography,
  useMediaQuery,
} from '@mui/material';
import { alpha, useTheme } from '@mui/material/styles';
import type { SyntheticEvent } from 'react';
import type { ItineraryEvent } from '../../types/event';
import type { User } from '../../types/user';
import { CostSummaryCard } from '../common/CostSummaryCard';
import { EventListPanel } from '../event/EventListPanel';

export type InsightsTab = 'events' | 'costs';

interface ItineraryInsightsDialogProps {
  open: boolean;
  tab: InsightsTab;
  events: ItineraryEvent[];
  selectedEventId?: string | null;
  usersMap: Record<string, User>;
  onClose: () => void;
  onTabChange: (tab: InsightsTab) => void;
  onSelectEvent: (event: ItineraryEvent) => void;
}

export const ItineraryInsightsDialog = ({
  open,
  tab,
  events,
  selectedEventId,
  usersMap,
  onClose,
  onTabChange,
  onSelectEvent,
}: ItineraryInsightsDialogProps) => {
  const theme = useTheme();
  const fullScreen = useMediaQuery(theme.breakpoints.down('md'));

  return (
    <Dialog
      fullScreen={fullScreen}
      fullWidth
      maxWidth="md"
      onClose={onClose}
      open={open}
      PaperProps={{
        sx: {
          minHeight: { md: 620 },
          bgcolor: alpha(theme.palette.background.paper, theme.palette.mode === 'light' ? 0.98 : 0.99),
          backgroundImage: 'none',
          border: `1px solid ${theme.palette.divider}`,
        },
      }}
    >
      <DialogTitle sx={{ pb: 1.2 }}>
        <Box alignItems="flex-start" display="flex" justifyContent="space-between" gap={2}>
          <Box>
            <Typography variant="h6">Trip insights</Typography>
            <Typography color="text.secondary" mt={0.5} variant="body2">
              Review the live event list and cost breakdown without leaving the calendar.
            </Typography>
          </Box>
          <IconButton aria-label="Close insights" onClick={onClose} sx={{ mt: -0.3 }}>
            <X size={18} />
          </IconButton>
        </Box>
      </DialogTitle>

      <Tabs
        onChange={(_event: SyntheticEvent, value: InsightsTab) => onTabChange(value)}
        sx={{ px: 3 }}
        value={tab}
      >
        <Tab label="Events" value="events" />
        <Tab label="Costs" value="costs" />
      </Tabs>

      <DialogContent sx={{ p: { xs: 2, md: 3 } }}>
        {tab === 'events' ? (
          <EventListPanel
            events={events}
            onSelectEvent={onSelectEvent}
            selectedEventId={selectedEventId}
            usersMap={usersMap}
          />
        ) : (
          <CostSummaryCard events={events} />
        )}
      </DialogContent>
    </Dialog>
  );
};
