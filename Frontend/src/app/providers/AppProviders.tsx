import { CssBaseline } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { RouterProvider } from 'react-router-dom';
import { router } from '../router';
import { ThemeModeProvider } from './ThemeModeProvider';
import { ToastProvider } from './ToastProvider';

export const AppProviders = () => (
  <ThemeModeProvider>
    <CssBaseline />
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <ToastProvider>
        <RouterProvider router={router} />
      </ToastProvider>
    </LocalizationProvider>
  </ThemeModeProvider>
);
