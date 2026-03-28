import '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Theme {
    app: {
      radius: {
        sm: string;
        md: string;
        lg: string;
      };
      surfaces: {
        hero: string;
        heroBorder: string;
        heroShadow: string;
        headerHero: string;
        metric: string;
        metricBorder: string;
        overlayButton: string;
        overlayButtonBorder: string;
      };
      selection: {
        bg: string;
        hoverBg: string;
        border: string;
        accent: string;
      };
    };
  }

  interface ThemeOptions {
    app?: {
      radius?: {
        sm?: string;
        md?: string;
        lg?: string;
      };
      surfaces?: {
        hero?: string;
        heroBorder?: string;
        heroShadow?: string;
        headerHero?: string;
        metric?: string;
        metricBorder?: string;
        overlayButton?: string;
        overlayButtonBorder?: string;
      };
      selection?: {
        bg?: string;
        hoverBg?: string;
        border?: string;
        accent?: string;
      };
    };
  }
}
