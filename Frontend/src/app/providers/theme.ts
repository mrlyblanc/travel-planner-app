import darkScrollbar from '@mui/material/darkScrollbar';
import { alpha, createTheme } from '@mui/material/styles';

export type ThemeMode = 'light' | 'dark';

const getScrollbarStyles = (mode: ThemeMode) => {
  const colors =
    mode === 'light'
      ? {
          track: '#e8eef8',
          thumb: '#9ab1cf',
          active: '#6e8fba',
        }
      : {
          track: '#132131',
          thumb: '#4f6786',
          active: '#7fa1d8',
        };

  const scrollbar = darkScrollbar(colors);

  return {
    scrollbarWidth: 'thin' as const,
    scrollbarColor: scrollbar.scrollbarColor,
    '&::-webkit-scrollbar, & *::-webkit-scrollbar': {
      width: 12,
      height: 12,
      backgroundColor: colors.track,
    },
    '&::-webkit-scrollbar-thumb, & *::-webkit-scrollbar-thumb': {
      borderRadius: 10,
      backgroundColor: colors.thumb,
      minHeight: 24,
      border: `3px solid ${colors.track}`,
    },
    '&::-webkit-scrollbar-thumb:focus, & *::-webkit-scrollbar-thumb:focus': {
      backgroundColor: colors.active,
    },
    '&::-webkit-scrollbar-thumb:active, & *::-webkit-scrollbar-thumb:active': {
      backgroundColor: colors.active,
    },
    '&::-webkit-scrollbar-thumb:hover, & *::-webkit-scrollbar-thumb:hover': {
      backgroundColor: colors.active,
    },
    '&::-webkit-scrollbar-corner, & *::-webkit-scrollbar-corner': {
      backgroundColor: colors.track,
    },
  };
};

export const createAppTheme = (mode: ThemeMode) =>
  createTheme({
    palette: {
      mode,
      primary: {
        main: '#4b8dff',
        light: '#81adff',
        dark: '#2c61c8',
      },
      secondary: {
        main: '#27b1a3',
      },
      success: {
        main: '#47b15b',
      },
      warning: {
        main: '#ff9e45',
      },
      background: mode === 'light'
        ? {
            default: '#f4f8ff',
            paper: 'rgba(255, 255, 255, 0.9)',
          }
        : {
            default: '#0d1724',
            paper: 'rgba(18, 29, 43, 0.86)',
          },
      text: mode === 'light'
        ? {
            primary: '#18324d',
            secondary: '#59708a',
          }
        : {
            primary: '#edf5ff',
            secondary: '#9db1c8',
          },
      divider: mode === 'light' ? alpha('#7a94b4', 0.18) : alpha('#9ab7da', 0.16),
    },
    shape: {
      borderRadius: 20,
    },
    app: {
      radius: {
        sm: '24px',
        md: '24px',
        lg: '28px',
      },
      surfaces: {
        hero:
          mode === 'light'
            ? 'linear-gradient(135deg, rgba(43,110,220,0.95) 0%, rgba(88,146,255,0.92) 44%, rgba(65,180,168,0.92) 100%)'
            : 'radial-gradient(circle at top right, rgba(39, 177, 163, 0.24), transparent 24%), radial-gradient(circle at top left, rgba(75, 141, 255, 0.24), transparent 28%), linear-gradient(135deg, rgba(23,44,76,0.98) 0%, rgba(35,78,152,0.92) 48%, rgba(22,103,111,0.92) 100%)',
        heroBorder: alpha('#4b8dff', mode === 'light' ? 0.18 : 0.22),
        heroShadow:
          mode === 'light'
            ? '0 24px 60px rgba(32, 74, 126, 0.22)'
            : '0 24px 64px rgba(0, 0, 0, 0.34)',
        headerHero:
          mode === 'light'
            ? 'radial-gradient(circle at top right, rgba(142, 226, 205, 0.3), transparent 25%), linear-gradient(180deg, rgba(255,255,255,0.95) 0%, rgba(246,250,255,0.95) 100%)'
            : 'radial-gradient(circle at top right, rgba(39, 177, 163, 0.22), transparent 22%), radial-gradient(circle at top left, rgba(75, 141, 255, 0.2), transparent 28%), linear-gradient(180deg, rgba(21,33,48,0.96) 0%, rgba(16,27,40,0.96) 100%)',
        metric: mode === 'light' ? 'rgba(255,255,255,0.76)' : 'rgba(255,255,255,0.08)',
        metricBorder: mode === 'light' ? 'rgba(122, 148, 180, 0.12)' : 'rgba(157, 177, 200, 0.14)',
        overlayButton: alpha('#ffffff', mode === 'light' ? 0.15 : 0.12),
        overlayButtonBorder: alpha('#ffffff', mode === 'light' ? 0.14 : 0.18),
      },
      selection: {
        bg: alpha('#4b8dff', mode === 'light' ? 0.12 : 0.16),
        hoverBg: alpha('#4b8dff', mode === 'light' ? 0.16 : 0.22),
        border: alpha('#4b8dff', 0.5),
        accent: '#4b8dff',
      },
    },
    typography: {
      fontFamily: '"Plus Jakarta Sans", sans-serif',
      h3: {
        fontWeight: 700,
        letterSpacing: '-0.04em',
      },
      h4: {
        fontWeight: 700,
        letterSpacing: '-0.03em',
      },
      h5: {
        fontWeight: 700,
        letterSpacing: '-0.02em',
      },
      h6: {
        fontWeight: 700,
        letterSpacing: '-0.02em',
      },
      button: {
        fontWeight: 600,
        textTransform: 'none',
      },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            minHeight: '100vh',
            ...getScrollbarStyles(mode),
          },
          '.MuiMultiSectionDigitalClockSection-root': {
            scrollbarWidth: 'none',
            msOverflowStyle: 'none',
          },
          '.MuiMultiSectionDigitalClockSection-root::-webkit-scrollbar': {
            display: 'none',
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            backdropFilter: 'blur(16px)',
            border: mode === 'light' ? '1px solid rgba(122, 148, 180, 0.15)' : '1px solid rgba(129, 166, 214, 0.14)',
            boxShadow:
              mode === 'light'
                ? '0 18px 50px rgba(33, 72, 120, 0.08)'
                : '0 20px 60px rgba(0, 0, 0, 0.34)',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          rounded: {
            borderRadius: 24,
          },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          input: {
            '&:-webkit-autofill, &:-webkit-autofill:hover, &:-webkit-autofill:focus, &:-webkit-autofill:active': {
              WebkitTextFillColor: mode === 'light' ? '#18324d' : '#edf5ff',
              caretColor: mode === 'light' ? '#18324d' : '#edf5ff',
              WebkitBoxShadow: `0 0 0 100px ${mode === 'light' ? '#ffffff' : '#132131'} inset`,
              boxShadow: `0 0 0 100px ${mode === 'light' ? '#ffffff' : '#132131'} inset`,
              borderRadius: 'inherit',
              transition: 'background-color 9999s ease-in-out 0s',
            },
          },
        },
      },
      MuiButton: {
        defaultProps: {
          disableElevation: true,
        },
        styleOverrides: {
        root: {
          borderRadius: 24,
          paddingInline: 16,
        },
      },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: 24,
            fontWeight: 600,
          },
        },
      },
    },
  });
