import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useToast } from '../../app/providers/ToastProvider';
import { useTravelStore } from '../../app/store/useTravelStore';
import { createExistingPasswordSchema, createStrongPasswordSchema, passwordPolicyHelperText } from '../../lib/passwordPolicy';

interface ChangePasswordDialogProps {
  open: boolean;
  onClose: () => void;
}

const changePasswordSchema = z
  .object({
    currentPassword: createExistingPasswordSchema('Current password'),
    newPassword: createStrongPasswordSchema('New password'),
    confirmNewPassword: z.string().min(1, 'Confirm your new password'),
  })
  .refine((values) => values.currentPassword !== values.newPassword, {
    message: 'New password must be different from your current password',
    path: ['newPassword'],
  })
  .refine((values) => values.newPassword === values.confirmNewPassword, {
    message: 'Passwords must match',
    path: ['confirmNewPassword'],
  });

type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;

const defaultValues: ChangePasswordFormValues = {
  currentPassword: '',
  newPassword: '',
  confirmNewPassword: '',
};

export const ChangePasswordDialog = ({ open, onClose }: ChangePasswordDialogProps) => {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const changePassword = useTravelStore((state) => state.changePassword);
  const logout = useTravelStore((state) => state.logout);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordFormValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues,
  });

  useEffect(() => {
    if (!open) {
      reset(defaultValues);
      setSubmitError(null);
    }
  }, [open, reset]);

  const handleChangePassword = handleSubmit(async (values) => {
    setSubmitError(null);

    try {
      await changePassword(values);
      showToast('Password updated. Sign in again with your new password.');
      onClose();
      await logout();
      navigate('/login', { replace: true });
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Unable to change password.');
    }
  });

  return (
    <Dialog
      fullWidth
      maxWidth="sm"
      onClose={() => {
        if (!isSubmitting) {
          onClose();
        }
      }}
      open={open}
    >
      <DialogTitle>Change password</DialogTitle>
      <DialogContent>
        <Stack component="form" onSubmit={handleChangePassword} spacing={2.2} sx={{ pt: 1 }}>
          <Typography color="text.secondary" variant="body2">
            Update your password for this backend account. You&apos;ll be signed out right after saving.
          </Typography>

          {submitError ? <Alert severity="error">{submitError}</Alert> : null}

          <TextField
            autoComplete="current-password"
            autoFocus
            error={Boolean(errors.currentPassword)}
            helperText={errors.currentPassword?.message}
            label="Current password"
            slotProps={{ inputLabel: { shrink: true } }}
            type="password"
            {...register('currentPassword')}
          />

          <TextField
            autoComplete="new-password"
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

          <DialogActions sx={{ px: 0, pb: 0, pt: 1 }}>
            <Button disabled={isSubmitting} onClick={onClose}>
              Cancel
            </Button>
            <Button loading={isSubmitting} type="submit" variant="contained">
              Save password
            </Button>
          </DialogActions>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};
