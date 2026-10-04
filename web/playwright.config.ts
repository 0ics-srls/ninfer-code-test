import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  workers: 1,
  retries: 0,
  reporter: 'list',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL: 'http://localhost:4200',
    animation: 'disabled',
    trace: 'retain-on-failure'
  },
  webServer: [
    {
      command: 'dotnet run --project src/MyApp.Server',
      cwd: '..',
      url: 'http://localhost:5000/health',
      reuseExistingServer: true,
      timeout: 180_000
    },
    {
      command: 'npm start',
      url: 'http://localhost:4200',
      reuseExistingServer: true,
      timeout: 180_000
    }
  ],
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
});
