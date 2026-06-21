import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:3000";
const useBundledChromium = process.env.PLAYWRIGHT_USE_BUNDLED_CHROMIUM === "true";
const devCommand =
  process.platform === "win32"
    ? ".node\\node.exe node_modules\\next\\dist\\bin\\next dev"
    : "npm run dev";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: [["list"], ["html", { open: "never" }]],
  timeout: 30_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  webServer: process.env.E2E_SKIP_WEBSERVER
    ? undefined
    : {
        command: devCommand,
        url: baseURL,
        reuseExistingServer: true,
        timeout: 120_000,
        env: {
          NEXT_PUBLIC_API_BASE_URL:
            process.env.E2E_API_BASE_URL ?? "http://localhost:8080/api/v1",
        },
      },
  projects: [
    {
      name: "desktop-chrome",
      use: { ...devices["Desktop Chrome"], channel: useBundledChromium ? undefined : "chrome" },
    },
    {
      name: "mobile-chrome",
      use: { ...devices["Pixel 7"], channel: useBundledChromium ? undefined : "chrome" },
    },
  ],
});
