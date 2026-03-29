import { BadgeCheck, Compass, History, Radio, Users2 } from 'lucide-react';
import { alpha, Box, Chip, Link, Paper, Stack, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import type { PropsWithChildren, ReactNode } from 'react';
import { Link as RouterLink } from 'react-router-dom';

interface AuthShellProps extends PropsWithChildren {
  eyebrow: string;
  title: string;
  subtitle: string;
  alternatePrompt: string;
  alternateActionLabel: string;
  alternateActionTo: string;
  supplemental?: ReactNode;
}

const featureHighlights = [
  {
    icon: <BadgeCheck size={18} />,
    label: 'Secure access',
    description: 'Pick up your itineraries with a protected account built for shared travel planning.',
  },
  {
    icon: <Radio size={18} />,
    label: 'Live trip updates',
    description: 'Watch the calendar stay current as flights, stays, and plans change.',
  },
  {
    icon: <History size={18} />,
    label: 'Clear activity history',
    description: 'See who changed reservations, timings, and day plans across the itinerary.',
  },
  {
    icon: <Users2 size={18} />,
    label: 'Shared planning',
    description: 'Coordinate destinations, bookings, and daily stops with the rest of the group.',
  },
];

export const AuthShell = ({
  eyebrow,
  title,
  subtitle,
  alternatePrompt,
  alternateActionLabel,
  alternateActionTo,
  supplemental,
  children,
}: AuthShellProps) => (
  <AuthViewport>
    <AuthGrid>
      <HeroPanel>
        <Stack spacing={4} sx={{ height: '100%' }}>
          <BrandRow direction="row" spacing={1.5}>
            <BrandMark>
              <Compass size={22} />
            </BrandMark>
            <Box>
              <Typography variant="h6">Trip Board</Typography>
              <Typography color="rgba(255,255,255,0.72)" variant="body2">
                Shared travel itineraries
              </Typography>
            </Box>
          </BrandRow>

          <Box>
            <Typography color="rgba(255,255,255,0.72)" variant="overline">
              Travel planning workspace
            </Typography>
            <Typography mt={1.2} variant="h3">
              Organize every trip in one shared timeline.
            </Typography>
            <Typography color="rgba(255,255,255,0.84)" mt={1.8} variant="body1">
              Sign in to map out stays, transport, meals, and key moments together so everyone knows what is booked,
              when plans change, and what comes next.
            </Typography>
          </Box>

          <Stack direction="row" flexWrap="wrap" gap={1}>
            <Chip label="Shared calendars" />
            <Chip label="Live updates" />
            <Chip label="Shared itineraries" />
            <Chip label="Activity history" />
          </Stack>

          <FeatureGrid>
            {featureHighlights.map((feature) => (
              <FeatureCard key={feature.label} elevation={0}>
                <FeatureIcon>{feature.icon}</FeatureIcon>
                <Typography fontWeight={700} variant="body2">
                  {feature.label}
                </Typography>
                <Typography color="rgba(255,255,255,0.74)" mt={0.6} variant="caption">
                  {feature.description}
                </Typography>
              </FeatureCard>
            ))}
          </FeatureGrid>
        </Stack>
      </HeroPanel>

      <Stack spacing={2.5}>
        <FormCard elevation={0}>
          <Stack spacing={1}>
            <Typography color="primary.main" variant="overline">
              {eyebrow}
            </Typography>
            <Typography variant="h4">{title}</Typography>
            <Typography color="text.secondary" variant="body1">
              {subtitle}
            </Typography>
          </Stack>

          <Typography color="text.secondary" mt={2.5} variant="body2">
            {alternatePrompt}{' '}
            <Link component={RouterLink} to={alternateActionTo} underline="hover">
              {alternateActionLabel}
            </Link>
          </Typography>

          <Box mt={3}>{children}</Box>
        </FormCard>

        {supplemental}
      </Stack>
    </AuthGrid>
  </AuthViewport>
);

const AuthViewport = styled(Box)(({ theme }) => ({
  minHeight: '100vh',
  padding: theme.spacing(2),
  background:
    theme.palette.mode === 'light'
      ? 'radial-gradient(circle at top left, rgba(75, 141, 255, 0.12), transparent 28%), radial-gradient(circle at right, rgba(39, 177, 163, 0.12), transparent 24%), linear-gradient(180deg, rgba(244,248,255,1) 0%, rgba(239,246,255,1) 100%)'
      : 'radial-gradient(circle at top left, rgba(75, 141, 255, 0.16), transparent 30%), radial-gradient(circle at right, rgba(39, 177, 163, 0.12), transparent 24%), linear-gradient(180deg, rgba(13,23,36,1) 0%, rgba(10,18,30,1) 100%)',
  [theme.breakpoints.up('md')]: {
    padding: theme.spacing(3),
  },
}));

const AuthGrid = styled(Box)(({ theme }) => ({
  display: 'grid',
  gap: theme.spacing(2.5),
  alignItems: 'stretch',
  maxWidth: 1380,
  marginInline: 'auto',
  [theme.breakpoints.up('lg')]: {
    gridTemplateColumns: 'minmax(0, 1.2fr) minmax(420px, 0.88fr)',
    minHeight: 'calc(100vh - 48px)',
  },
}));

const HeroPanel = styled(Box)(({ theme }) => ({
  borderRadius: theme.app.radius.lg,
  border: `1px solid ${theme.app.surfaces.heroBorder}`,
  background: theme.app.surfaces.hero,
  color: '#ffffff',
  boxShadow: theme.app.surfaces.heroShadow,
  padding: theme.spacing(3),
  overflow: 'hidden',
  position: 'relative',
  [theme.breakpoints.up('md')]: {
    padding: theme.spacing(4),
  },
}));

const BrandRow = styled(Stack)({
  alignItems: 'center',
});

const BrandMark = styled(Box)(({ theme }) => ({
  display: 'grid',
  placeItems: 'center',
  width: 46,
  height: 46,
  borderRadius: theme.app.radius.md,
  backgroundColor: alpha('#ffffff', 0.16),
  border: `1px solid ${alpha('#ffffff', 0.18)}`,
}));

const FeatureGrid = styled(Box)(({ theme }) => ({
  display: 'grid',
  gap: theme.spacing(1.5),
  gridTemplateColumns: 'repeat(1, minmax(0, 1fr))',
  marginTop: 'auto',
  [theme.breakpoints.up('sm')]: {
    gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
  },
}));

const FeatureCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2),
  borderRadius: theme.app.radius.md,
  backgroundColor: alpha('#ffffff', 0.1),
  border: `1px solid ${alpha('#ffffff', 0.12)}`,
  color: '#ffffff',
}));

const FeatureIcon = styled(Box)(({ theme }) => ({
  display: 'inline-grid',
  placeItems: 'center',
  width: 34,
  height: 34,
  borderRadius: theme.app.radius.md,
  marginBottom: theme.spacing(1.5),
  backgroundColor: alpha('#ffffff', 0.14),
}));

const FormCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(3),
  borderRadius: theme.app.radius.lg,
  backgroundColor: theme.palette.background.paper,
  border: `1px solid ${theme.palette.divider}`,
  [theme.breakpoints.up('md')]: {
    padding: theme.spacing(4),
  },
}));
