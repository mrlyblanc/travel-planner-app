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
