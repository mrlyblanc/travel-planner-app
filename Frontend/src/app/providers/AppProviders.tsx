import { CssBaseline } from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { RouterProvider } from 'react-router-dom';
import { router } from '../router';
import { ThemeModeProvider } from './ThemeModeProvider';
import { ToastProvider } from './ToastProvider';
import { TravelAppBootstrap } from './TravelAppBootstrap';

export const AppProviders = () => (
  <ThemeModeProvider>
    <CssBaseline />
    <LocalizationProvider dateAdapter={AdapterDayjs}>
      <ToastProvider>
        <TravelAppBootstrap>
          <RouterProvider router={router} />
        </TravelAppBootstrap>
      </ToastProvider>
    </LocalizationProvider>
  </ThemeModeProvider>
);
