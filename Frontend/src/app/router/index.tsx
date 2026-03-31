import { Suspense, lazy, type ReactNode } from 'react';
import { Navigate, Outlet, createBrowserRouter, useLocation } from 'react-router-dom';
import { RouteLoadingScreen } from '../../components/common/RouteLoadingScreen';
import { useTravelStore } from '../store/useTravelStore';

const AppShell = lazy(() =>
  import('../../components/layout/AppShell').then((module) => ({ default: module.AppShell })),
);
const ItineraryDetailsPage = lazy(() =>
  import('../../pages/ItineraryDetailsPage').then((module) => ({ default: module.ItineraryDetailsPage })),
);
const ItineraryListPage = lazy(() =>
  import('../../pages/ItineraryListPage').then((module) => ({ default: module.ItineraryListPage })),
);
const LoginPage = lazy(() =>
  import('../../pages/LoginPage').then((module) => ({ default: module.LoginPage })),
);
const RegisterPage = lazy(() =>
  import('../../pages/RegisterPage').then((module) => ({ default: module.RegisterPage })),
);

const withRouteSuspense = (element: ReactNode) => (
  <Suspense fallback={<RouteLoadingScreen />}>
    {element}
  </Suspense>
);

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

  return withRouteSuspense(<AppShell />);
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
        element: withRouteSuspense(<LoginPage />),
      },
      {
        path: '/register',
        element: withRouteSuspense(<RegisterPage />),
      },
    ],
  },
  {
    path: '/',
    element: <ProtectedRoute />,
    children: [
      {
        path: 'itineraries',
        element: withRouteSuspense(<ItineraryListPage />),
      },
      {
        path: 'itineraries/:itineraryId',
        element: withRouteSuspense(<ItineraryDetailsPage />),
      },
    ],
  },
  {
    path: '*',
    element: <RootRedirect />,
  },
]);
