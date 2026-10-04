import { test, expect, type Page } from '@playwright/test';

// Coverage: template.md §0 prerequisites, §1 backend API endpoints, §8 build verification.
// §8 is covered implicitly: the app only loads if backend + frontend built and both serve.

async function getJson<T>(page: Page, url: string): Promise<T> {
  const res = await page.request.get(url);
  expect(res.status()).toBe(200);
  return (await res.json()) as T;
}

test.describe('smoke: app boot + API surface', () => {
  test('app loads at /, redirects to /todos, shows title and table without console errors', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    await page.goto('/');
    await expect(page).toHaveURL(/\/todos$/);
    await expect(page.getByRole('heading', { name: 'my-app' })).toBeVisible();
    await expect(page.locator('.p-datatable')).toBeVisible();
    await expect(page.locator('tbody tr').first()).toBeVisible();

    expect(errors).toHaveLength(0);
  });

  test('health and version endpoints respond through the dev proxy', async ({ page }) => {
    const health = await getJson<{ status: string }>(page, '/health');
    expect(health.status).toBe('healthy');

    const version = await getJson<{ version: string }>(page, '/version');
    expect(version.version).toMatch(/^1\.0\.\d+(\.\d+)?$/);
  });

  test('todos list returns camelCase items with string status', async ({ page }) => {
    const body = await getJson<{ data: Record<string, unknown>[]; totalCount: number }>(
      page,
      '/api/Todos?take=100&requireTotalCount=true'
    );

    expect(Array.isArray(body.data)).toBe(true);
    expect(body.data.length).toBeGreaterThanOrEqual(2);
    expect(body.totalCount).toBeGreaterThanOrEqual(2);

    for (const todo of body.data) {
      expect(typeof todo.id).toBe('number');
      expect(typeof todo.title).toBe('string');
      expect(['Pending', 'InProgress', 'Completed']).toContain(todo.status as string);
      expect(typeof todo.createdAt).toBe('string');
    }
  });

  test('openapi spec and scalar are reachable through the proxy', async ({ page }) => {
    const spec = await page.request.get('/openapi/v1.json');
    expect(spec.status()).toBe(200);
    expect(await spec.text()).toContain('/api/Todos');

    const scalar = await page.request.get('/scalar/v1');
    expect(scalar.status()).toBe(200);
  });

  test('todos API create/update/delete lifecycle with validation', async ({ page }) => {
    const title = `e2e-smoke-${Date.now()}`;

    const created = await page.request.post('/api/Todos', { data: { title, status: 'Pending' } });
    expect(created.status()).toBe(201);
    const dto = (await created.json()) as { id: number };
    expect(dto.id).toBeGreaterThan(0);

    const updated = await page.request.put(`/api/Todos/${dto.id}`, { data: { title, status: 'Completed' } });
    expect(updated.status()).toBe(200);
    expect(((await updated.json()) as { status: string }).status).toBe('Completed');

    const deleted = await page.request.delete(`/api/Todos/${dto.id}`);
    expect(deleted.status()).toBe(204);

    const putMissing = await page.request.put('/api/Todos/999999', { data: { title, status: 'Pending' } });
    expect(putMissing.status()).toBe(404);
    const deleteMissing = await page.request.delete('/api/Todos/999999');
    expect(deleteMissing.status()).toBe(404);

    const emptyTitle = await page.request.post('/api/Todos', { data: { title: '', status: 'Pending' } });
    expect(emptyTitle.status()).toBe(400);
  });
});
