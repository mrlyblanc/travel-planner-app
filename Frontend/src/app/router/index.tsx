import { Navigate, createBrowserRouter } from 'react-router-dom';
import { AppShell } from '../../components/layout/AppShell';
import { ItineraryDetailsPage } from '../../pages/ItineraryDetailsPage';
import { ItineraryListPage } from '../../pages/ItineraryListPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Navigate replace to="/itineraries" />,
  },
  {
    path: '/',
    element: <AppShell />,
    children: [
      {
        path: 'itineraries',
        element: <ItineraryListPage />,
      },
      {
        path: 'itineraries/:itineraryId',
        element: <ItineraryDetailsPage />,
      },
    ],
  },
  {
    path: '*',
    element: <Navigate replace to="/itineraries" />,
  },
]);
