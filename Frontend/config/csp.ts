const LOCAL_API_BASE_URLS = [
  'http://localhost:5290',
  'https://localhost:7051',
  'http://localhost:5070',
  'https://localhost:7291',
];

const STATIC_WEB_APP_NAVIGATION_FALLBACK = {
  rewrite: '/index.html',
  exclude: [
    '/assets/*',
    '/*.css',
    '/*.js',
    '/*.map',
    '/*.png',
    '/*.jpg',
    '/*.jpeg',
    '/*.gif',
    '/*.svg',
    '/*.ico',
    '/*.webmanifest',
    '/*.woff',
    '/*.woff2',
    '/*.ttf',
    '/*.json',
  ],
};

type Command = 'serve' | 'build';
type CspMode = 'report-only' | 'enforce';

const normalizeOrigin = (value: string) => {
  try {
    return new URL(value).origin;
  } catch {
    return null;
  }
};

const toWebSocketOrigin = (origin: string) => origin.replace(/^http/i, 'ws');

const unique = (values: Array<string | null | undefined>) =>
  values.filter((value, index, array): value is string => Boolean(value) && array.indexOf(value) === index);

const getConfiguredApiOrigins = (env: Record<string, string>, command: Command) => {
  const configuredOrigin = normalizeOrigin(env.VITE_API_BASE_URL?.trim() ?? '');
  const localOrigins = command === 'serve'
    ? LOCAL_API_BASE_URLS.map(normalizeOrigin)
    : [];

  return unique([configuredOrigin, ...localOrigins]);
};

const getReportUri = (env: Record<string, string>) => {
  const explicitReportUri = env.VITE_CSP_REPORT_URI?.trim();
  if (explicitReportUri) {
    return explicitReportUri;
  }

  const configuredApiOrigin = normalizeOrigin(env.VITE_API_BASE_URL?.trim() ?? '');
  return configuredApiOrigin ? `${configuredApiOrigin}/security/csp-reports` : null;
};

export const getCspMode = (env: Record<string, string>): CspMode => {
  return env.VITE_CSP_MODE?.trim().toLowerCase() === 'enforce' ? 'enforce' : 'report-only';
};

export const buildCspHeaderName = (mode: CspMode) => {
  return mode === 'enforce' ? 'Content-Security-Policy' : 'Content-Security-Policy-Report-Only';
};

export const buildCspValue = (env: Record<string, string>, command: Command) => {
  const apiOrigins = getConfiguredApiOrigins(env, command);
  const connectSources = unique([
    "'self'",
    ...apiOrigins,
    ...apiOrigins.map(toWebSocketOrigin),
    'https://api.geoapify.com',
    command === 'serve' ? 'http://localhost:5173' : null,
    command === 'serve' ? 'ws://localhost:5173' : null,
    command === 'serve' ? 'http://127.0.0.1:5173' : null,
    command === 'serve' ? 'ws://127.0.0.1:5173' : null,
  ]);

  const scriptSources = command === 'serve'
    ? ["'self'", "'unsafe-eval'"]
    : ["'self'"];

  const directives: Array<[string, string[]]> = [
    ['default-src', ["'self'"]],
    ['base-uri', ["'self'"]],
    ['object-src', ["'none'"]],
    ['frame-ancestors', ["'none'"]],
    ['form-action', ["'self'"]],
    ['img-src', ["'self'", 'data:', 'blob:']],
    ['font-src', ["'self'", 'data:']],
    ['style-src', ["'self'", "'unsafe-inline'"]],
    ['script-src', scriptSources],
    ['connect-src', connectSources],
    ['worker-src', ["'self'", 'blob:']],
    ['manifest-src', ["'self'"]],
    ['frame-src', ["'none'"]],
  ];

  const reportUri = getReportUri(env);
  if (reportUri) {
    directives.push(['report-uri', [reportUri]]);
  }

  return directives.map(([directive, values]) => `${directive} ${values.join(' ')}`).join('; ');
};

export const buildSecurityHeaders = (env: Record<string, string>, command: Command) => {
  const mode = getCspMode(env);
  const headerName = buildCspHeaderName(mode);

  return {
    'X-Content-Type-Options': 'nosniff',
    'X-Frame-Options': 'DENY',
    'Referrer-Policy': 'strict-origin-when-cross-origin',
    [headerName]: buildCspValue(env, command),
  };
};

export const buildStaticWebAppConfig = (env: Record<string, string>) => ({
  navigationFallback: STATIC_WEB_APP_NAVIGATION_FALLBACK,
  routes: [
    {
      route: '/assets/*',
      headers: {
        'Cache-Control': 'public, max-age=31536000, immutable',
      },
    },
  ],
  globalHeaders: buildSecurityHeaders(env, 'build'),
});
