import { KeyRound, LogOut, Menu as MenuIcon, MoonStar, SunMedium } from 'lucide-react';
import {
  AppBar,
  Avatar,
  Box,
  ButtonBase,
  Divider,
  Drawer,
  IconButton,
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
import { useMemo, useState } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { alpha } from '@mui/material/styles';
import { useToast } from '../../app/providers/ToastProvider';
import { useTravelStore } from '../../app/store/useTravelStore';
import { useThemeMode } from '../../app/providers/ThemeModeProvider';
import { useCurrentUser } from '../../hooks/useCurrentUser';
import { ChangePasswordDialog } from '../auth/ChangePasswordDialog';
import { SidebarContent } from './SidebarContent';

const drawerWidth = 320;

export const AppShell = () => {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'));
  const [mobileOpen, setMobileOpen] = useState(false);
  const [changePasswordOpen, setChangePasswordOpen] = useState(false);
  const [profileMenuAnchor, setProfileMenuAnchor] = useState<HTMLElement | null>(null);
  const [isLoggingOut, setIsLoggingOut] = useState(false);
  const currentUser = useCurrentUser();
  const { mode, toggleMode } = useThemeMode();
  const logout = useTravelStore((state) => state.logout);
  const { showToast } = useToast();
  const navigate = useNavigate();
  const isProfileMenuOpen = Boolean(profileMenuAnchor);

  const drawerContent = useMemo(
    () => <SidebarContent onNavigate={() => setMobileOpen(false)} />,
    [],
  );

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
                <ButtonBase
                  aria-controls={isProfileMenuOpen ? 'profile-menu' : undefined}
                  aria-expanded={isProfileMenuOpen ? 'true' : undefined}
                  aria-haspopup="menu"
                  onClick={(event) => setProfileMenuAnchor(event.currentTarget)}
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

      <ChangePasswordDialog onClose={() => setChangePasswordOpen(false)} open={changePasswordOpen} />
    </Box>
  );
};
