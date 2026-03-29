import { Navigate, Outlet, createBrowserRouter, useLocation } from 'react-router-dom';
import { AppShell } from '../../components/layout/AppShell';
import { useTravelStore } from '../store/useTravelStore';
import { ItineraryDetailsPage } from '../../pages/ItineraryDetailsPage';
import { ItineraryListPage } from '../../pages/ItineraryListPage';
import { LoginPage } from '../../pages/LoginPage';
import { RegisterPage } from '../../pages/RegisterPage';

const RootRedirect = () => {
  const accessToken = useTravelStore((state) => state.accessToken);
  return <Navigate replace to={accessToken ? '/itineraries' : '/login'} />;
};

const PublicOnlyRoute = () => {
  const accessToken = useTravelStore((state) => state.accessToken);
  return accessToken ? <Navigate replace to="/itineraries" /> : <Outlet />;
};

const ProtectedRoute = () => {
  const accessToken = useTravelStore((state) => state.accessToken);
  const location = useLocation();

  if (!accessToken) {
    return <Navigate replace state={{ from: location }} to="/login" />;
  }

  return <AppShell />;
};

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootRedirect />,
  },
  {
    element: <PublicOnlyRoute />,
    children: [
      {
        path: '/login',
        element: <LoginPage />,
      },
      {
        path: '/register',
        element: <RegisterPage />,
      },
    ],
  },
  {
    path: '/',
    element: <ProtectedRoute />,
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
    element: <RootRedirect />,
  },
]);
