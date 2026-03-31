import { Bell, KeyRound, LogOut, Menu as MenuIcon, MoonStar, SunMedium, Trash2 } from 'lucide-react';
import {
  AppBar,
  Avatar,
  Badge,
  Box,
  ButtonBase,
  CircularProgress,
  Divider,
  Drawer,
  IconButton,
  LinearProgress,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { Suspense, lazy, useEffect, useMemo, useState } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { alpha } from '@mui/material/styles';
import { useToast } from '../../app/providers/ToastProvider';
import { useTravelStore } from '../../app/store/useTravelStore';
import { useThemeMode } from '../../app/providers/ThemeModeProvider';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { ApiError } from '../../lib/api';
import { fromNow } from '../../lib/date';
import type { UserNotification } from '../../types/notification';
import { RouteLoadingScreen } from '../common/RouteLoadingScreen';
import { SidebarContent } from './SidebarContent';

const drawerWidth = 320;
const ChangePasswordDialog = lazy(() =>
  import('../auth/ChangePasswordDialog').then((module) => ({ default: module.ChangePasswordDialog })),
);

export const AppShell = () => {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [changePasswordOpen, setChangePasswordOpen] = useState(false);
  const [notificationMenuAnchor, setNotificationMenuAnchor] = useState<HTMLElement | null>(null);
  const [profileMenuAnchor, setProfileMenuAnchor] = useState<HTMLElement | null>(null);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const [navigatingNotificationId, setNavigatingNotificationId] = useState<string | null>(null);
  const [deletingNotificationId, setDeletingNotificationId] = useState<string | null>(null);
  const [showAllNotifications, setShowAllNotifications] = useState(false);
  const currentUser = useCurrentUser();
  const notifications = useTravelStore((state) => state.notifications);
  const markNotificationRead = useTravelStore((state) => state.markNotificationRead);
  const markAllNotificationsRead = useTravelStore((state) => state.markAllNotificationsRead);
  const deleteNotification = useTravelStore((state) => state.deleteNotification);
  const refreshAll = useTravelStore((state) => state.refreshAll);
  const refreshItineraryBundle = useTravelStore((state) => state.refreshItineraryBundle);
  const isMutating = useTravelStore((state) => state.pendingMutationCount > 0);
  const { mode, toggleMode } = useThemeMode();
  const logout = useTravelStore((state) => state.logout);
  const { showToast } = useToast();
  const navigate = useNavigate();
  const unreadNotifications = notifications.filter((notification) => !notification.readAt);
  const isNotificationMenuOpen = Boolean(notificationMenuAnchor);
  const isProfileMenuOpen = Boolean(profileMenuAnchor);
  const isNavigatingFromNotification = Boolean(navigatingNotificationId);
  const showTopProgress = isNavigatingFromNotification || isMutating;
  const visibleNotifications = showAllNotifications ? notifications : notifications.slice(0, 5);
  const hiddenNotificationCount = Math.max(notifications.length - 5, 0);

  const drawerContent = useMemo(
    () => <SidebarContent onNavigate={() => setMobileOpen(false)} />,
    [],
  );

  useEffect(() => {
    if (isNotificationMenuOpen) {
      return;
    }

    setShowAllNotifications(false);
    setDeletingNotificationId(null);
  }, [isNotificationMenuOpen]);

  useEffect(() => {
    if (notifications.length > 5 || !showAllNotifications) {
      return;
    }

    setShowAllNotifications(false);
  }, [notifications.length, showAllNotifications]);

  const handleLogout = async () => {
    setProfileMenuAnchor(null);
    setIsLoggingOut(true);

    try {
      await logout();
      showToast('Signed out');
      navigate('/login', { replace: true });
    } finally {
      setIsLoggingOut(false);
    }
  };

  const handleNotificationClick = async (notification: UserNotification) => {
    if (isNavigatingFromNotification || Boolean(deletingNotificationId)) {
      return;
    }

    setNotificationMenuAnchor(null);
    setNavigatingNotificationId(notification.id);

    try {
      if (!notification.readAt) {
        await markNotificationRead(notification.id).catch(() => undefined);
      }

      if (!notification.itineraryId) {
        return;
      }

      try {
        await refreshItineraryBundle(notification.itineraryId);
        navigate(`/itineraries/${notification.itineraryId}`);
      } catch (error) {
        await refreshAll().catch(() => undefined);
        navigate('/itineraries');

        if (error instanceof ApiError && (error.status === 403 || error.status === 404)) {
          showToast(
            notification.type === 'itinerary.member.removed'
              ? 'You’re no longer part of that itinerary, so it’s no longer available in your workspace.'
              : 'That itinerary is no longer available to your account.',
            'info',
          );
          return;
        }

        throw error;
      }
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Unable to open that notification right now.', 'error');
    } finally {
      setNavigatingNotificationId(null);
    }
  };

  const handleDeleteNotification = async (notificationId: string) => {
    if (isNavigatingFromNotification || Boolean(deletingNotificationId)) {
      return;
    }

    setDeletingNotificationId(notificationId);

    try {
      await deleteNotification(notificationId);
      showToast('Notification removed');
    } catch (error) {
      showToast(error instanceof Error ? error.message : 'Unable to remove that notification right now.', 'error');
    } finally {
      setDeletingNotificationId(null);
    }
  };

  if (isLoggingOut) {
    return (
      <Box
        sx={{
          minHeight: '100vh',
          px: 3,
          backgroundColor: theme.palette.background.default,
        }}
      >
        <RouteLoadingScreen
          description="Closing your session and returning you to the sign-in screen."
          minHeight="100vh"
          title="Signing out"
        />
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar
        color="transparent"
        elevation={0}
        position="fixed"
        sx={{
          backdropFilter: 'blur(18px)',
          bgcolor: alpha(theme.palette.background.default, mode === 'light' ? 0.78 : 0.82),
          borderBottom: `1px solid ${theme.palette.divider}`,
          ml: { lg: `${drawerWidth}px` },
          width: { lg: `calc(100% - ${drawerWidth}px)` },
        }}
      >
        <Toolbar sx={{ minHeight: 76 }}>
          {!isDesktop ? (
            <IconButton edge="start" onClick={() => setMobileOpen(true)} sx={{ mr: 1.5 }}>
              <MenuIcon size={20} />
            </IconButton>
          ) : null}

          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h6">Travel itinerary workspace</Typography>
            <Typography color="text.secondary" variant="body2">
              Plan trips like a shared calendar, now synced with the backend.
            </Typography>
          </Box>

          <Stack alignItems="center" direction="row" spacing={1.5}>
            {currentUser ? (
              <>
                <IconButton
                  aria-controls={isNotificationMenuOpen ? 'notification-menu' : undefined}
                  aria-expanded={isNotificationMenuOpen ? 'true' : undefined}
                  aria-haspopup="menu"
                  onClick={(event) => {
                    setProfileMenuAnchor(null);
                    setNotificationMenuAnchor(event.currentTarget);
                  }}
                  sx={{
                    border: `1px solid ${theme.palette.divider}`,
                    borderRadius: theme.app.radius.md,
                    color: 'text.primary',
                  }}
                >
                  <Badge badgeContent={unreadNotifications.length} color="error" max={9}>
                    <Bell size={18} />
                  </Badge>
                </IconButton>

                <Menu
                  anchorEl={notificationMenuAnchor}
                  anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                  id="notification-menu"
                  onClose={() => setNotificationMenuAnchor(null)}
                  open={isNotificationMenuOpen}
                  PaperProps={{
                    sx: {
                      mt: 1.2,
                      minWidth: 320,
                      maxWidth: 380,
                      borderRadius: theme.app.radius.md,
                      border: `1px solid ${theme.palette.divider}`,
                      backgroundImage: 'none',
                      bgcolor: theme.palette.background.paper,
                      boxShadow:
                        theme.palette.mode === 'light'
                          ? '0 18px 44px rgba(17, 38, 66, 0.16)'
                          : '0 18px 44px rgba(0, 0, 0, 0.36)',
                    },
                  }}
                  transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                >
                  <Stack
                    alignItems="center"
                    direction="row"
                    justifyContent="space-between"
                    sx={{ px: 2, py: 1.5 }}
                  >
                    <Box>
                      <Typography fontWeight={700} variant="body2">
                        Notifications
                      </Typography>
                      <Typography color="text.secondary" variant="caption">
                        Realtime itinerary updates and collaborator activity
                      </Typography>
                    </Box>
                    {unreadNotifications.length > 0 ? (
                      <ButtonBase
                        onClick={() => {
                          void markAllNotificationsRead().catch(() => undefined);
                        }}
                        sx={{
                          borderRadius: theme.app.radius.sm,
                          color: theme.palette.primary.main,
                          fontSize: theme.typography.caption.fontSize,
                          fontWeight: 600,
                          px: 1,
                          py: 0.5,
                        }}
                      >
                        Mark all read
                      </ButtonBase>
                    ) : null}
                  </Stack>

                  <Divider />

                  {notifications.length === 0 ? (
                    <Box sx={{ px: 2, py: 3 }}>
                      <Typography fontWeight={600} variant="body2">
                        No notifications yet
                      </Typography>
                      <Typography color="text.secondary" mt={0.5} variant="caption">
                        When someone joins or gets removed from a trip, we’ll show it here.
                      </Typography>
                    </Box>
                  ) : (
                    visibleNotifications.map((notification) => (
                      <MenuItem
                        disabled={isNavigatingFromNotification || deletingNotificationId === notification.id}
                        key={notification.id}
                        onClick={() => {
                          void handleNotificationClick(notification);
                        }}
                        sx={{
                          alignItems: 'flex-start',
                          whiteSpace: 'normal',
                          py: 1.4,
                          backgroundColor: notification.readAt ? 'transparent' : alpha(theme.palette.primary.main, 0.08),
                        }}
                      >
                        <Stack direction="row" spacing={1.25} sx={{ width: '100%', alignItems: 'flex-start' }}>
                          <Box
                            sx={{
                              width: 8,
                              height: 8,
                              mt: 0.9,
                              borderRadius: '50%',
                              bgcolor: notification.readAt ? theme.palette.divider : theme.palette.primary.main,
                              flexShrink: 0,
                            }}
                          />
                          <Box sx={{ minWidth: 0, flexGrow: 1 }}>
                            <Typography fontWeight={700} variant="body2">
                              {notification.title}
                            </Typography>
                            <Typography color="text.secondary" variant="caption">
                              {notification.message}
                            </Typography>
                            <Typography color="text.secondary" display="block" mt={0.6} variant="caption">
                              {fromNow(notification.createdAt)}
                            </Typography>
                          </Box>
                          <IconButton
                            aria-label="Delete notification"
                            disabled={Boolean(deletingNotificationId)}
                            onClick={(event) => {
                              event.preventDefault();
                              event.stopPropagation();
                              void handleDeleteNotification(notification.id);
                            }}
                            size="small"
                            sx={{
                              mt: -0.2,
                              color: 'text.secondary',
                              '&:hover': {
                                color: 'error.main',
                              },
                            }}
                          >
                            {deletingNotificationId === notification.id ? <CircularProgress color="inherit" size={16} /> : <Trash2 size={16} />}
                          </IconButton>
                        </Stack>
                      </MenuItem>
                    ))
                  )}

                  {notifications.length > 5 ? (
                    <>
                      <Divider />
                      <Box sx={{ px: 2, py: 1.2 }}>
                        <ButtonBase
                          onClick={() => setShowAllNotifications((current) => !current)}
                          sx={{
                            borderRadius: theme.app.radius.sm,
                            color: theme.palette.primary.main,
                            fontSize: theme.typography.caption.fontSize,
                            fontWeight: 600,
                            px: 1,
                            py: 0.5,
                          }}
                        >
                          {showAllNotifications ? 'See less' : `See more (${hiddenNotificationCount} more)`}
                        </ButtonBase>
                      </Box>
                    </>
                  ) : null}
                </Menu>

                <ButtonBase
                  aria-controls={isProfileMenuOpen ? 'profile-menu' : undefined}
                  aria-expanded={isProfileMenuOpen ? 'true' : undefined}
                  aria-haspopup="menu"
                  onClick={(event) => {
                    setNotificationMenuAnchor(null);
                    setProfileMenuAnchor(event.currentTarget);
                  }}
                  sx={{
                    borderRadius: theme.app.radius.md,
                    px: 0,
                    py: 0,
                    gap: 1.2,
                    transition: theme.transitions.create(['background-color', 'border-color'], {
                      duration: theme.transitions.duration.shorter,
                    }),
                    '&:hover': {
                      backgroundColor: 'transparent',
                    },
                  }}
                >
                  <Box sx={{ textAlign: 'right', display: { xs: 'none', sm: 'block' } }}>
                    <Typography fontWeight={600} variant="body2">
                      {currentUser.name}
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      {currentUser.email}
                    </Typography>
                  </Box>
                  <Avatar
                    sx={{
                      width: 38,
                      height: 38,
                      bgcolor: alpha(theme.palette.primary.main, mode === 'light' ? 0.14 : 0.18),
                      color: 'primary.main',
                    }}
                  >
                    {currentUser.avatar}
                  </Avatar>
                </ButtonBase>

                <Menu
                  anchorEl={profileMenuAnchor}
                  anchorOrigin={{ horizontal: 'right', vertical: 'bottom' }}
                  id="profile-menu"
                  onClose={() => setProfileMenuAnchor(null)}
                  open={isProfileMenuOpen}
                  PaperProps={{
                    sx: {
                      mt: 1.2,
                      minWidth: 260,
                      borderRadius: theme.app.radius.md,
                      border: `1px solid ${theme.palette.divider}`,
                      backgroundImage: 'none',
                      bgcolor: theme.palette.background.paper,
                      boxShadow:
                        theme.palette.mode === 'light'
                          ? '0 18px 44px rgba(17, 38, 66, 0.16)'
                          : '0 18px 44px rgba(0, 0, 0, 0.36)',
                    },
                  }}
                  transformOrigin={{ horizontal: 'right', vertical: 'top' }}
                >
                  <Box sx={{ px: 2, py: 1.5 }}>
                    <Typography fontWeight={700} variant="body2">
                      {currentUser.name}
                    </Typography>
                    <Typography color="text.secondary" variant="caption">
                      {currentUser.email}
                    </Typography>
                  </Box>

                  <Divider />

                  <MenuItem
                    onClick={() => {
                      toggleMode();
                      setProfileMenuAnchor(null);
                    }}
                  >
                    <ListItemIcon>{mode === 'light' ? <MoonStar size={18} /> : <SunMedium size={18} />}</ListItemIcon>
                    <ListItemText
                      primary={mode === 'light' ? 'Switch to night mode' : 'Switch to day mode'}
                      secondary={mode === 'light' ? 'Use darker surfaces across the app' : 'Return to the lighter theme'}
                    />
                  </MenuItem>

                  <MenuItem
                    onClick={() => {
                      setProfileMenuAnchor(null);
                      setChangePasswordOpen(true);
                    }}
                  >
                    <ListItemIcon>
                      <KeyRound size={18} />
                    </ListItemIcon>
                    <ListItemText primary="Change password" secondary="Update your backend account password" />
                  </MenuItem>

                  <Divider />

                  <MenuItem disabled={isLoggingOut} onClick={() => void handleLogout()}>
                    <ListItemIcon>
                      <LogOut size={18} />
                    </ListItemIcon>
                    <ListItemText
                      primary={isLoggingOut ? 'Signing out...' : 'Sign out'}
                      secondary="End your current session on this device"
                    />
                  </MenuItem>
                </Menu>
              </>
            ) : null}
          </Stack>
        </Toolbar>
        <Box
          sx={{
            position: 'absolute',
            insetInline: 0,
            bottom: 0,
            opacity: showTopProgress ? 1 : 0,
            pointerEvents: 'none',
            transition: theme.transitions.create('opacity', {
              duration: theme.transitions.duration.shorter,
            }),
          }}
        >
          <LinearProgress />
        </Box>
      </AppBar>

      <Box component="nav" sx={{ width: { lg: drawerWidth }, flexShrink: { lg: 0 } }}>
        <Drawer
          ModalProps={{ keepMounted: true }}
          onClose={() => setMobileOpen(false)}
          open={mobileOpen}
          sx={{
            display: { xs: 'block', lg: 'none' },
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              bgcolor: theme.palette.background.paper,
            },
          }}
          variant="temporary"
        >
          {drawerContent}
        </Drawer>
        <Drawer
          open
          sx={{
            display: { xs: 'none', lg: 'block' },
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              borderRight: `1px solid ${theme.palette.divider}`,
              bgcolor: alpha(theme.palette.background.paper, mode === 'light' ? 0.82 : 0.92),
              backdropFilter: 'blur(22px)',
            },
          }}
          variant="permanent"
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          px: { xs: 2, md: 3 },
          pb: 4,
          pt: { xs: 11, md: 12 },
        }}
      >
        <Outlet />
      </Box>

      {changePasswordOpen ? (
        <Suspense fallback={null}>
          <ChangePasswordDialog onClose={() => setChangePasswordOpen(false)} open={changePasswordOpen} />
        </Suspense>
      ) : null}
    </Box>
  );
};
