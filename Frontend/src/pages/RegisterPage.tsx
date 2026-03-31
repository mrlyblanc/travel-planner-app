import { zodResolver } from '@hookform/resolvers/zod';
import { ArrowRight, Sparkles } from 'lucide-react';
import { Alert, Avatar, Box, Button, Paper, Stack, TextField, Typography } from '@mui/material';
import { styled } from '@mui/material/styles';
import { useEffect, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useToast } from '../app/providers/ToastProvider';
import { useTravelStore } from '../app/store/useTravelStore';
import { AuthShell } from '../components/auth/AuthShell';
import { createStrongPasswordSchema, passwordPolicyHelperText } from '../lib/passwordPolicy';
import { initialsFromName } from '../lib/utils';

const registrationSchema = z
  .object({
    name: z.string().min(2, 'Name must be at least 2 characters').max(120, 'Name is too long'),
    email: z.string().email('Enter a valid email address'),
    password: createStrongPasswordSchema('Password'),
    confirmPassword: z.string().min(1, 'Confirm your password'),
    avatar: z.string().max(16, 'Avatar must be 16 characters or fewer').optional(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'Passwords must match',
    path: ['confirmPassword'],
  });

type RegistrationFormValues = z.infer<typeof registrationSchema>;

export const RegisterPage = () => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const registerUser = useTravelStore((state) => state.register);
  const clearError = useTravelStore((state) => state.clearError);
  const error = useTravelStore((state) => state.error);
  const isBootstrapping = useTravelStore((state) => state.isBootstrapping);
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<RegistrationFormValues>({
    resolver: zodResolver(registrationSchema),
    defaultValues: {
      name: '',
      email: '',
      password: '',
      confirmPassword: '',
      avatar: '',
    },
  });

  useEffect(() => {
    clearError();
  }, [clearError]);

  const name = watch('name');
  const avatar = watch('avatar');
  const avatarPreview = useMemo(() => avatar?.trim() || initialsFromName(name) || 'TB', [avatar, name]);

  const handleRegister = handleSubmit(async (values) => {
    try {
      await registerUser({
        name: values.name.trim(),
        email: values.email.trim().toLowerCase(),
        password: values.password,
        avatar: values.avatar?.trim() || initialsFromName(values.name),
      });
      showToast('Account created successfully');
      navigate('/itineraries', { replace: true });
    } catch {
      // Store error state drives the inline message.
    }
  });

  return (
    <AuthShell
      alternateActionLabel="Sign in"
      alternateActionTo="/login"
      alternatePrompt="Already have an account?"
      eyebrow="Register"
      subtitle="Create a backend-backed account so you can join shared itineraries and manage collaborative event planning."
      supplemental={
        <PreviewCard elevation={0}>
          <Stack alignItems="center" direction="row" spacing={1.5}>
            <Avatar
              sx={{
                width: 44,
                height: 44,
                bgcolor: 'primary.main',
                color: '#ffffff',
                fontWeight: 700,
              }}
            >
              {avatarPreview}
            </Avatar>
            <Box>
              <Typography fontWeight={700} variant="body1">
                Avatar preview
              </Typography>
              <Typography color="text.secondary" variant="body2">
                Leave the avatar field blank and we will use your initials automatically.
              </Typography>
            </Box>
          </Stack>
        </PreviewCard>
      }
      title="Create your Trip Board account"
    >
      <form onSubmit={handleRegister}>
        <Stack spacing={2}>
          {error ? <Alert severity="error">{error}</Alert> : null}

          <TextField
            autoComplete="name"
            autoFocus
            error={Boolean(errors.name)}
            helperText={errors.name?.message}
            label="Full name"
            slotProps={{ inputLabel: { shrink: true } }}
            {...register('name')}
          />

          <TextField
            autoComplete="email"
            error={Boolean(errors.email)}
            helperText={errors.email?.message}
            label="Email"
            slotProps={{ inputLabel: { shrink: true } }}
            {...register('email')}
          />

          <TextField
            error={Boolean(errors.avatar)}
            helperText={errors.avatar?.message ?? 'Optional. Short initials or label, up to 16 characters.'}
            label="Avatar"
            slotProps={{ inputLabel: { shrink: true } }}
            {...register('avatar')}
          />

          <TextField
            autoComplete="new-password"
            error={Boolean(errors.password)}
            helperText={errors.password?.message ?? passwordPolicyHelperText}
            label="Password"
            slotProps={{ inputLabel: { shrink: true } }}
            type="password"
            {...register('password')}
          />

          <TextField
            autoComplete="new-password"
            error={Boolean(errors.confirmPassword)}
            helperText={errors.confirmPassword?.message}
            label="Confirm password"
            slotProps={{ inputLabel: { shrink: true } }}
            type="password"
            {...register('confirmPassword')}
          />

          <Button endIcon={<ArrowRight size={16} />} size="large" type="submit" variant="contained">
            {isBootstrapping ? 'Creating account...' : 'Create account'}
          </Button>
        </Stack>
      </form>

      <Stack alignItems="center" direction="row" mt={2.5} spacing={1}>
        <Sparkles size={16} />
        <Typography color="text.secondary" variant="caption">
          Registration uses the backend `POST /api/users` endpoint, then signs you in through `POST /api/auth/login`.
        </Typography>
      </Stack>
    </AuthShell>
  );
};

const PreviewCard = styled(Paper)(({ theme }) => ({
  padding: theme.spacing(2.5),
  borderRadius: theme.app.radius.lg,
  border: `1px solid ${theme.palette.divider}`,
  backgroundColor: theme.palette.mode === 'light' ? 'rgba(255,255,255,0.84)' : 'rgba(255,255,255,0.04)',
}));
