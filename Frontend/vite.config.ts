import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { defineConfig, loadEnv, type Plugin } from 'vite';
import react from '@vitejs/plugin-react-swc';
import { buildSecurityHeaders, buildStaticWebAppConfig } from './config/csp';

const staticWebAppConfigPlugin = (config: ReturnType<typeof buildStaticWebAppConfig>): Plugin => {
  let outDir = 'dist';

  return {
    name: 'travel-planner-staticwebapp-config',
    configResolved(resolvedConfig) {
      outDir = resolvedConfig.build.outDir;
    },
    async writeBundle() {
      const outputPath = path.resolve(process.cwd(), outDir, 'staticwebapp.config.json');
      await mkdir(path.dirname(outputPath), { recursive: true });
      await writeFile(outputPath, `${JSON.stringify(config, null, 2)}\n`, 'utf8');
    },
  };
};

export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const securityHeaders = buildSecurityHeaders(env, command === 'serve' ? 'serve' : 'build');
  const staticWebAppConfig = buildStaticWebAppConfig(env);

  return {
    plugins: [react(), staticWebAppConfigPlugin(staticWebAppConfig)],
    build: {
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (!id.includes('node_modules')) {
              return undefined;
            }

            if (id.includes('@fullcalendar')) {
              return 'calendar';
            }

            if (id.includes('@mui/x-date-pickers')) {
              return 'pickers';
            }

            if (
              id.includes('react-hook-form') ||
              id.includes('@hookform') ||
              id.includes('zod')
            ) {
              return 'forms';
            }

            if (id.includes('@microsoft/signalr')) {
              return 'realtime';
            }

            if (id.includes('react-router')) {
              return 'router';
            }

            if (
              id.includes('/react/') ||
              id.includes('/react-dom/') ||
              id.includes('scheduler')
            ) {
              return 'react-core';
            }

            if (id.includes('dayjs')) {
              return 'date-utils';
            }

            if (
              id.includes('@mui') ||
              id.includes('@emotion') ||
              id.includes('@fontsource')
            ) {
              return undefined;
            }

            return 'vendor';
          },
        },
      },
    },
    server: {
      fs: {
        allow: [path.resolve(process.cwd(), '..')],
      },
      headers: securityHeaders,
    },
    preview: {
      headers: securityHeaders,
    },
  };
});
