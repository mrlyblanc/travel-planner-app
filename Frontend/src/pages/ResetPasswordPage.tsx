import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, ShieldCheck } from 'lucide-react';
import { Alert, Button, Stack, TextField, Typography } from '@mui/material';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom';
import { z } from 'zod';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore } from '../app/store/useTravelStore';
import { AuthShell } from '../components/auth/AuthShell';
import { createStrongPasswordSchema, passwordPolicyHelperText } from '../lib/passwordPolicy';

const resetPasswordSchema = z
  .object({
    newPassword: createStrongPasswordSchema('New password'),
    confirmNewPassword: z.string().min(1, 'Confirm your new password'),
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: 'Passwords must match',
    path: ['confirmNewPassword'],
  });

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

export const ResetPasswordPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { showToast } = useToast();
  const resetPassword = useTravelStore((state) => state.resetPassword);
  const clearError = useTravelStore((state) => state.clearError);
  const error = useTravelStore((state) => state.error);
  const isBootstrapping = useTravelStore((state) => state.isBootstrapping);
  const resetToken = searchParams.get('token')?.trim() ?? '';
  const hasResetToken = resetToken.length > 0;
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      newPassword: '',
      confirmNewPassword: '',
    },
  });

  useEffect(() => {
    clearError();
  }, [clearError]);

  const handleResetPassword = handleSubmit(async (values) => {
    if (!hasResetToken) {
      return;
    }

    try {
      await resetPassword({
        token: resetToken,
        newPassword: values.newPassword,
        confirmNewPassword: values.confirmNewPassword,
      });

      showToast('Password updated. Sign in with your new password.', 'success');
      navigate('/login', { replace: true });
    } catch {
      // Store error state drives the inline message.
    }
  });

  return (
    <AuthShell
      alternateActionLabel="Request another link"
      alternateActionTo="/forgot-password"
      alternatePrompt="Need a fresh reset link?"
      eyebrow="Reset password"
      subtitle="Choose a new password for your Trip Board account so you can get back to collaborative itinerary planning."
      title="Create a new password"
    >
      {!hasResetToken ? (
        <Stack spacing={2.5}>
          <Alert severity="warning">
            This reset link is missing its secure token. Request a new password reset link to continue.
          </Alert>
          <Button component={RouterLink} to="/forgot-password" variant="contained">
            Request new reset link
          </Button>
        </Stack>
      ) : (
        <>
          <form onSubmit={handleResetPassword}>
            <Stack spacing={2}>
              {error ? <Alert severity="error">{error}</Alert> : null}

              <TextField
                autoComplete="new-password"
                autoFocus
                error={Boolean(errors.newPassword)}
                helperText={errors.newPassword?.message ?? passwordPolicyHelperText}
                label="New password"
                slotProps={{ inputLabel: { shrink: true } }}
                type="password"
                {...register('newPassword')}
              />

              <TextField
                autoComplete="new-password"
                error={Boolean(errors.confirmNewPassword)}
                helperText={errors.confirmNewPassword?.message}
                label="Confirm new password"
                slotProps={{ inputLabel: { shrink: true } }}
                type="password"
                {...register('confirmNewPassword')}
              />

              <Button endIcon={<ArrowRight size={16} />} size="large" type="submit" variant="contained">
                {isBootstrapping ? 'Updating password...' : 'Update password'}
              </Button>
            </Stack>
          </form>

          <Stack alignItems="center" direction="row" mt={2.5} spacing={1}>
            <ShieldCheck size={16} />
            <Typography color="text.secondary" variant="caption">
              Resetting your password signs out any old sessions so your itinerary workspace stays protected.
            </Typography>
          </Stack>
        </>
      )}
    </AuthShell>
  );
};
