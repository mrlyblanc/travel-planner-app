import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, Copy, MailSearch } from 'lucide-react';
import { Alert, Button, Link, Paper, Stack, TextField, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink } from 'react-router-dom';
import { z } from 'zod';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore } from '../app/store/useTravelStore';
import { AuthShell } from '../components/auth/AuthShell';

const forgotPasswordSchema = z.object({
  email: z.string().email('Enter a valid email address'),
});

type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

export const ForgotPasswordPage = () => {
  const { showToast } = useToast();
  const forgotPassword = useTravelStore((state) => state.forgotPassword);
  const clearError = useTravelStore((state) => state.clearError);
  const error = useTravelStore((state) => state.error);
  const isBootstrapping = useTravelStore((state) => state.isBootstrapping);
  const [requestResult, setRequestResult] = useState<{ message: string; devResetToken: string | null } | null>(null);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: {
      email: '',
    },
  });

  useEffect(() => {
    clearError();
  }, [clearError]);

  const enteredEmail = watch('email');
  const devResetPath = useMemo(() => {
    if (!requestResult?.devResetToken) {
      return null;
    }

    return `/reset-password?token=${encodeURIComponent(requestResult.devResetToken)}`;
  }, [requestResult?.devResetToken]);

  const devResetLink = useMemo(() => {
    if (!devResetPath || typeof window === 'undefined') {
      return null;
    }

    return `${window.location.origin}${devResetPath}`;
  }, [devResetPath]);

  const handleRequestReset = handleSubmit(async (values) => {
    try {
      const response = await forgotPassword({
        email: values.email.trim().toLowerCase(),
      });
      setRequestResult(response);
      showToast('Password reset instructions are ready.', 'success');
    } catch {
      setRequestResult(null);
    }
  });

  const handleCopyLink = async () => {
    if (!devResetLink || !navigator.clipboard) {
      return;
    }

    await navigator.clipboard.writeText(devResetLink);
    showToast('Reset link copied.', 'success');
  };

  return (
    <AuthShell
      alternateActionLabel="Back to sign in"
      alternateActionTo="/login"
      alternatePrompt="Remembered it already?"
      eyebrow="Forgot password"
      subtitle="Enter the email tied to your Trip Board account and we will prepare a secure reset link so you can get back to your shared itineraries."
      title="Reset your password"
    >
      <form onSubmit={handleRequestReset}>
        <Stack spacing={2}>
          {error ? <Alert severity="error">{error}</Alert> : null}
          {requestResult ? <Alert severity="success">{requestResult.message}</Alert> : null}

          <TextField
            autoComplete="email"
            autoFocus
            error={Boolean(errors.email)}
            helperText={errors.email?.message ?? 'We will send reset instructions for this account if it exists.'}
            label="Email"
            slotProps={{ inputLabel: { shrink: true } }}
            {...register('email')}
          />

          <Button endIcon={<ArrowRight size={16} />} size="large" type="submit" variant="contained">
            {isBootstrapping ? 'Preparing reset...' : 'Send reset instructions'}
          </Button>
        </Stack>
      </form>

      {requestResult?.devResetToken && devResetPath ? (
        <DevResetCard elevation={0}>
          <Stack alignItems="flex-start" spacing={1.25}>
            <Stack alignItems="center" direction="row" spacing={1}>
              <MailSearch size={18} />
              <Typography fontWeight={700} variant="body1">
                Development reset link
              </Typography>
            </Stack>

            <Typography color="text.secondary" variant="body2">
              Local development is exposing the reset link directly so you can finish the flow before real email delivery
              is configured{enteredEmail ? ` for ${enteredEmail.trim()}.` : '.'}
            </Typography>

            {devResetLink ? (
              <Link
                href={devResetLink}
                rel="noreferrer"
                sx={{ wordBreak: 'break-all' }}
                target="_blank"
                underline="hover"
                variant="body2"
              >
                {devResetLink}
              </Link>
            ) : null}

            <Stack direction="row" flexWrap="wrap" gap={1}>
              <Button component={RouterLink} size="small" to={devResetPath} variant="contained">
                Open reset form
              </Button>
              <Button onClick={() => void handleCopyLink()} size="small" startIcon={<Copy size={14} />} variant="outlined">
                Copy link
              </Button>
            </Stack>
          </Stack>
        </DevResetCard>
      ) : null}
    </AuthShell>
  );
};

const DevResetCard = styled(Paper)(({ theme }) => ({
  marginTop: theme.spacing(2.5),
  padding: theme.spacing(2.5),
  borderRadius: theme.app.radius.lg,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.mode === 'light' ? 'rgba(255,255,255,0.84)' : 'rgba(255,255,255,0.04)',
}));
