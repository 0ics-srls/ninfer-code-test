import { test, expect, type Page } from '@playwright/test';

// Coverage: template.md §2 dark mode, §3 layout + stats card invariants.

async function domStats(page: Page): Promise<{ total: number; completed: number; pending: number }> {
  const value = (label: string) =>
    page
      .locator(`span.font-semibold:text-is("${label}")`)
      .locator('xpath=following-sibling::span')
      .first()
      .innerText()
      .then((s) => Number(s.trim()));
  return {
    total: await value('Total'),
    completed: await value('Completed'),
    pending: await value('Pending')
  };
}

test.describe('theme + stats (§2, §3)', () => {
  test('dark mode toggles the app-dark class and persists across reload', async ({ page }) => {
    await page.goto('/todos');
    const html = page.locator('html');
    // fresh context: no stored preference, prefers-color-scheme=light
    await expect(html).not.toHaveClass(/app-dark/);

    const toggle = page.locator('button:has(.pi-sun), button:has(.pi-moon)');
    await expect(toggle).toHaveCount(1);

    await toggle.click();
    await expect(html).toHaveClass(/app-dark/);
    await expect.poll(() => page.evaluate(() => localStorage.getItem('my-app-theme'))).toBe('dark');

    await toggle.click();
    await expect(html).not.toHaveClass(/app-dark/);
    await expect.poll(() => page.evaluate(() => localStorage.getItem('my-app-theme'))).toBe('light');

    await page.evaluate(() => localStorage.setItem('my-app-theme', 'dark'));
    await page.reload();
    await expect(html).toHaveClass(/app-dark/);
  });

  test('page shows two p-cards with Stats and Todos headers', async ({ page }) => {
    await page.goto('/todos');

    await expect(page.locator('p-card')).toHaveCount(2);
    await expect(page.getByText('Stats', { exact: true })).toBeVisible();
    await expect(page.getByText('Todos', { exact: true })).toBeVisible();
  });

  test('stats card invariants: Total = Completed + Pending and matches the API', async ({ page }) => {
    await page.goto('/todos');

    // stats load asynchronously; wait until the API total is reflected
    await expect
      .poll(async () => (await domStats(page)).total, { timeout: 30_000 })
      .toBeGreaterThan(0);

    const stats = await domStats(page);
    expect(stats.total).toBe(stats.completed + stats.pending);

    const res = await page.request.get('/api/Todos/stats');
    expect(res.status()).toBe(200);
    const api = (await res.json()) as { total: number; completed: number; pending: number };
    expect(stats.total).toBe(Number(api.total));
    expect(stats.completed).toBe(Number(api.completed));
    expect(stats.pending).toBe(Number(api.pending));
  });
});
