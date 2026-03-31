import { Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material';
import { alpha, styled } from '@mui/material/styles';
import { useEffect, useState } from 'react';

interface SigningInExperienceProps {
  email: string;
}

const signInStages = [
  {
    title: 'Waking up the database',
    description: 'Cold-starting the free-tier database so your workspace can respond.',
  },
  {
    title: 'Verifying your account',
    description: 'Checking your credentials and refreshing your secure session.',
  },
  {
    title: 'Pulling your itineraries',
    description: 'Loading trip summaries, collaborators, and recent notifications.',
  },
  {
    title: 'Opening the shared workspace',
    description: 'Finishing the handoff to your travel calendar and event timeline.',
  },
];

export const SigningInExperience = ({ email }: SigningInExperienceProps) => {
  const [activeStageIndex, setActiveStageIndex] = useState(0);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);

  useEffect(() => {
    const elapsedTimerId = window.setInterval(() => {
      setElapsedSeconds((current) => current + 1);
    }, 1000);

    const stageTimerId = window.setInterval(() => {
      setActiveStageIndex((current) => (current < signInStages.length - 1 ? current + 1 : current));
    }, 1600);

    return () => {
      window.clearInterval(elapsedTimerId);
      window.clearInterval(stageTimerId);
    };
  }, []);

  return (
    <ExperienceCard elevation={0}>
      <Stack spacing={2.25}>
        <Stack alignItems="center" direction="row" justifyContent="space-between" spacing={1}>
          <Typography color="primary.main" fontWeight={700} variant="overline">
            Signing you in
          </Typography>
          <Chip label={`${elapsedSeconds}s`} size="small" variant="outlined" />
        </Stack>

        <Stack spacing={0.9}>
          <Typography variant="h5">Waking up your travel workspace</Typography>
          <Typography color="text.secondary" variant="body2">
            Free-tier services can take a little time to wake up after being idle. We are keeping your sign-in active
            while the database and trip data come online.
          </Typography>
        </Stack>

        <LinearProgress />

        <Stack spacing={1}>
          {signInStages.map((stage, index) => {
            const isActive = index === activeStageIndex;
            const isComplete = index < activeStageIndex;

            return (
              <StageCard key={stage.title} $active={isActive} $complete={isComplete}>
                <StageMarker $active={isActive} $complete={isComplete}>
                  {isComplete ? '✓' : index + 1}
                </StageMarker>
                <Stack spacing={0.2}>
                  <Typography fontWeight={700} variant="body2">
                    {stage.title}
                  </Typography>
                  <Typography color="text.secondary" variant="caption">
                    {stage.description}
                  </Typography>
                </Stack>
              </StageCard>
            );
          })}
        </Stack>

        <StatusStrip>
          <Typography fontWeight={600} variant="body2">
            Signing in as {email}
          </Typography>
          <Typography color="text.secondary" variant="caption">
            Keep this tab open. The first request after an idle period can take a bit longer than usual.
          </Typography>
        </StatusStrip>
      </Stack>
    </ExperienceCard>
  );
};

const ExperienceCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2.5),
  borderRadius: theme.app.radius.lg,
  border: `1px solid ${theme.palette.divider}`,
  background:
    theme.palette.mode === 'light'
      ? 'linear-gradient(180deg, rgba(255,255,255,0.95) 0%, rgba(245,249,255,0.92) 100%)'
      : 'linear-gradient(180deg, rgba(255,255,255,0.05) 0%, rgba(255,255,255,0.03) 100%)',
}));

const StageCard = styled(Stack, {
  shouldForwardProp: (prop) => prop !== '$active' && prop !== '$complete',
})<{ $active: boolean; $complete: boolean }>(({ theme, $active, $complete }) => ({
  alignItems: 'flex-start',
  border: `1px solid ${
    $active ? alpha(theme.palette.primary.main, 0.45) : $complete ? alpha(theme.palette.success.main, 0.28) : theme.palette.divider
  }`,
  borderRadius: theme.app.radius.md,
  backgroundColor: $active
    ? alpha(theme.palette.primary.main, theme.palette.mode === 'light' ? 0.08 : 0.12)
    : $complete
      ? alpha(theme.palette.success.main, theme.palette.mode === 'light' ? 0.08 : 0.12)
      : alpha(theme.palette.background.default, theme.palette.mode === 'light' ? 0.28 : 0.18),
  flexDirection: 'row',
  gap: theme.spacing(1.2),
  padding: theme.spacing(1.2, 1.35),
}));

const StageMarker = styled('span', {
  shouldForwardProp: (prop) => prop !== '$active' && prop !== '$complete',
})<{ $active: boolean; $complete: boolean }>(({ theme, $active, $complete }) => ({
  alignItems: 'center',
  backgroundColor: $complete
    ? theme.palette.success.main
    : $active
      ? theme.palette.primary.main
      : alpha(theme.palette.text.secondary, 0.18),
  borderRadius: '999px',
  color: $complete || $active ? '#ffffff' : theme.palette.text.secondary,
  display: 'inline-flex',
  fontSize: '0.76rem',
  fontWeight: 700,
  height: 24,
  justifyContent: 'center',
  minWidth: 24,
  paddingInline: 6,
}));

const StatusStrip = styled(Stack)(({ theme }) => ({
  padding: theme.spacing(1.4, 1.6),
  borderRadius: theme.app.radius.md,
  backgroundColor: alpha(theme.palette.primary.main, theme.palette.mode === 'light' ? 0.08 : 0.12),
  border: `1px solid ${alpha(theme.palette.primary.main, 0.22)}`,
}));
