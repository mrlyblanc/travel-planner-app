import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, KeyRound } from 'lucide-react';
import { Alert, Box, Button, Divider, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore } from '../app/store/useTravelStore';
import { AuthShell } from '../components/auth/AuthShell';
import { backendConfig } from '../lib/api';
import { createExistingPasswordSchema } from '../lib/passwordPolicy';

const loginSchema = z.object({
  email: z.string().email('Enter a valid email address'),
  password: createExistingPasswordSchema('Password'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

interface LoginLocationState {
  from?: {
    pathname?: string;
  };
}

export const LoginPage = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { showToast } = useToast();
  const login = useTravelStore((state) => state.login);
  const clearError = useTravelStore((state) => state.clearError);
  const error = useTravelStore((state) => state.error);
  const isBootstrapping = useTravelStore((state) => state.isBootstrapping);
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  useEffect(() => {
    clearError();
  }, [clearError]);

  const from = (location.state as LoginLocationState | null)?.from?.pathname ?? '/itineraries';
  const hasSeededDevLogin = backendConfig.hasSeededDevLogin;

  const handleFillDemoAccount = () => {
    if (!hasSeededDevLogin) {
      return;
    }

    setValue('email', backendConfig.defaultLoginEmail, { shouldDirty: true });
    setValue('password', backendConfig.defaultLoginPassword, { shouldDirty: true });
    clearError();
  };

  const handleLogin = handleSubmit(async (values) => {
    try {
      await login(values.email.trim().toLowerCase(), values.password);
      showToast('Signed in successfully');
      navigate(from, { replace: true });
    } catch {
      // Store error state drives the inline message.
    }
  });

  return (
    <AuthShell
      alternateActionLabel="Create one"
      alternateActionTo="/register"
      alternatePrompt="Need a new account?"
      eyebrow="Sign in"
      subtitle="Sign in to review upcoming trips, update shared plans, and keep every booking, stop, and schedule change in one place."
      supplemental={hasSeededDevLogin ? (
        <DemoCard elevation={0}>
          <Stack direction="row" justifyContent="space-between" spacing={2}>
            <Box>
              <Typography fontWeight={700} variant="body1">
                Quick start account
              </Typography>
              <Typography color="text.secondary" mt={0.5} variant="body2">
                Jump straight into the shared itineraries with a ready-to-use traveler account for local development.
              </Typography>
            </Box>
            <Button onClick={handleFillDemoAccount} variant="outlined">
              Fill account
            </Button>
          </Stack>

          <Divider sx={{ my: 2 }} />

          <Stack spacing={1}>
            <Typography variant="body2">
              <strong>Email:</strong> {backendConfig.defaultLoginEmail}
            </Typography>
            <Typography variant="body2">
              <strong>Password:</strong> {backendConfig.defaultLoginPassword}
            </Typography>
          </Stack>
        </DemoCard>
      ) : null}
      title="Welcome back to Trip Board"
    >
      <form onSubmit={handleLogin}>
        <Stack spacing={2}>
          {error ? <Alert severity="error">{error}</Alert> : null}

          <TextField
            autoComplete="email"
            autoFocus
            error={Boolean(errors.email)}
            helperText={errors.email?.message}
            label="Email"
            slotProps={{ inputLabel: { shrink: true } }}
            {...register('email')}
          />

          <TextField
            autoComplete="current-password"
            error={Boolean(errors.password)}
            helperText={errors.password?.message}
            label="Password"
            slotProps={{ inputLabel: { shrink: true } }}
            type="password"
            {...register('password')}
          />

          <Box display="flex" justifyContent="flex-end" mt={-0.5}>
            <Link component={RouterLink} to="/forgot-password" underline="hover" variant="body2">
              Forgot password?
            </Link>
          </Box>

          <Button endIcon={<ArrowRight size={16} />} size="large" type="submit" variant="contained">
            {isBootstrapping ? 'Signing in...' : 'Sign in'}
          </Button>
        </Stack>
      </form>

      <Stack alignItems="center" direction="row" mt={2.5} spacing={1}>
        <KeyRound size={16} />
        <Typography color="text.secondary" variant="caption">
          Your account unlocks shared itineraries, real-time trip updates, and a full event activity trail for every stop on the calendar.
        </Typography>
      </Stack>
    </AuthShell>
  );
};

const DemoCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2.5),
  borderRadius: theme.app.radius.lg,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.mode === 'light' ? 'rgba(255,255,255,0.84)' : 'rgba(255,255,255,0.04)',
}));
