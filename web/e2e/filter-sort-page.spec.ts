import { test, expect, type Page } from '@playwright/test';

// Coverage: template.md §4 read (table, pagination, sort) + the search/status-filter/clear toolbar.
// State-invariant: unique titles per test, created/deleted via API, counts compared against the API total.

const unique = (prefix: string) => `${prefix}-${Date.now()}`;

async function apiTotal(page: Page): Promise<number> {
  const res = await page.request.get('/api/Todos?take=1&requireTotalCount=true');
  expect(res.status()).toBe(200);
  const body = (await res.json()) as { totalCount: number };
  return Number(body.totalCount);
}

async function createViaApi(page: Page, title: string, status: string): Promise<{ id: number }> {
  const res = await page.request.post('/api/Todos', { data: { title, status } });
  expect(res.status()).toBe(201);
  return (await res.json()) as { id: number };
}

async function visibleTitles(page: Page): Promise<string[]> {
  const rows = page.locator('tbody tr');
  const count = await rows.count();
  const titles: string[] = [];
  for (let i = 0; i < count; i++) {
    titles.push((await rows.nth(i).locator('td').nth(1).innerText()).trim());
  }
  return titles;
}

test.describe('filter / sort / pagination (§4)', () => {
  test('table renders with full headers and no empty data cells', async ({ page }) => {
    await page.goto('/todos');

    for (const header of ['ID', 'Title', 'Status', 'Created At', 'Actions']) {
      await expect(page.locator('th').filter({ hasText: header }).first()).toBeVisible();
    }

    const rows = page.locator('tbody tr');
    await expect(rows.first()).toBeVisible();
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const cells = rows.nth(i).locator('td');
      expect(await cells.count()).toBe(5);
      // data columns must be non-empty; the Actions column holds icon buttons
      for (let c = 0; c < 4; c++) {
        expect((await cells.nth(c).innerText()).trim()).not.toBe('');
      }
      expect(await rows.nth(i).locator('button:has(.pi-pencil), button:has(.pi-trash)').count()).toBeGreaterThanOrEqual(2);
    }
  });

  test('page 1 shows min(10, total) rows and page 2 shows different rows', async ({ page }) => {
    await page.goto('/todos');
    const total = await apiTotal(page);

    const rows = page.locator('tbody tr');
    await expect(rows.first()).toBeVisible();
    expect(await rows.count()).toBe(Math.min(10, total));
    const firstIdPage1 = (await rows.first().locator('td').first().innerText()).trim();

    if (total > 10) {
      await page.getByRole('button', { name: 'Next Page' }).click();
      await expect(rows.first().locator('td').first()).not.toHaveText(firstIdPage1);
      expect(await rows.count()).toBe(10);
    }
  });

  test('sort by title returns the exact ascending and descending order for a known subset', async ({ page }) => {
    const ts = Date.now();
    const names = ['alpha', 'mid', 'zeta'].map((s) => `e2e-sort-${ts}-${s}`);
    const ids: number[] = [];
    for (const name of names) {
      ids.push((await createViaApi(page, name, 'Pending')).id);
    }

    await page.goto('/todos');
    await page.getByPlaceholder('Search title...').fill(`e2e-sort-${ts}`);
    await expect(page.locator('tbody tr')).toHaveCount(3);

    const titleHeader = page.locator('th').filter({ hasText: 'Title' });
    await titleHeader.click();
    await expect.poll(() => visibleTitles(page)).toEqual(names);

    await titleHeader.click();
    await expect.poll(() => visibleTitles(page)).toEqual([...names].reverse());

    for (const id of ids) {
      await page.request.delete(`/api/Todos/${id}`);
    }
  });

  test('search is debounced and filters rows by title', async ({ page }) => {
    const title = unique('e2e-search');
    const dto = await createViaApi(page, title, 'Pending');

    await page.goto('/todos');
    const requestPromise = page.waitForRequest(
      (r) => r.url().includes('/api/Todos') && r.url().includes('contains') && r.url().includes(title)
    );
    await page.getByPlaceholder('Search title...').fill(title);
    const request = await requestPromise;
    expect(request.url()).toContain(encodeURIComponent(title));

    await expect(page.getByRole('cell', { name: title, exact: true })).toBeVisible();
    await expect(page.locator('tbody tr')).toHaveCount(1);

    await page.request.delete(`/api/Todos/${dto.id}`);
  });

  test('status filter shows only the selected status', async ({ page }) => {
    const title = unique('e2e-status');
    const dto = await createViaApi(page, title, 'Completed');

    await page.goto('/todos');
    await page.getByRole('combobox', { name: 'All' }).click();
    await page.getByRole('option', { name: 'Completed', exact: true }).click();

    await expect(page.getByRole('cell', { name: title, exact: true })).toBeVisible();
    const statuses = await page.locator('tbody tr').locator('td').nth(2).allInnerTexts();
    expect(statuses.length).toBeGreaterThan(0);
    for (const status of statuses) {
      expect(status.trim()).toBe('Completed');
    }

    await page.getByRole('combobox', { name: 'Completed' }).click();
    await page.getByRole('option', { name: 'Pending', exact: true }).click();
    await expect(page.getByRole('cell', { name: title, exact: true })).toBeHidden();

    await page.request.delete(`/api/Todos/${dto.id}`);
  });

  test('clear resets search and status filter and restores the full page', async ({ page }) => {
    const title = unique('e2e-clear');
    const dto = await createViaApi(page, title, 'Pending');

    await page.goto('/todos');
    await page.getByPlaceholder('Search title...').fill(title);
    await expect(page.locator('tbody tr')).toHaveCount(1);

    const total = await apiTotal(page);
    await page.locator('button:has(.pi-filter-slash)').click();

    await expect(page.getByPlaceholder('Search title...')).toHaveValue('');
    await expect(page.getByRole('combobox', { name: 'All' })).toBeVisible();
    await expect(page.locator('tbody tr')).toHaveCount(Math.min(10, total));

    await page.request.delete(`/api/Todos/${dto.id}`);
  });
});
