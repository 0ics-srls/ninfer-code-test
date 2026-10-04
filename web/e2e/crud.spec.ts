import { test, expect, type Page } from '@playwright/test';

// Coverage: template.md §5 create, §6 update, §7 delete.
// State-invariant: every test creates its own todo with a unique title and deletes it via API afterwards.

const unique = (prefix: string) => `${prefix}-${Date.now()}`;

async function apiStats(page: Page): Promise<{ total: number; completed: number; pending: number }> {
  const res = await page.request.get('/api/Todos/stats');
  expect(res.status()).toBe(200);
  const body = (await res.json()) as { total: number; completed: number; pending: number };
  return { total: Number(body.total), completed: Number(body.completed), pending: Number(body.pending) };
}

async function createViaApi(page: Page, title: string, status: string): Promise<{ id: number }> {
  const res = await page.request.post('/api/Todos', { data: { title, status } });
  expect(res.status()).toBe(201);
  return (await res.json()) as { id: number };
}

async function searchFor(page: Page, title: string): Promise<void> {
  await page.getByPlaceholder('Search title...').fill(title);
  await expect(page.getByRole('cell', { name: title, exact: true })).toBeVisible();
}

function rowFor(page: Page, title: string) {
  return page.getByRole('row').filter({ hasText: title });
}

test.describe('crud: create / update / delete', () => {
  test('create: dialog opens empty with Save disabled, then creates a todo', async ({ page }) => {
    await page.goto('/todos');
    const before = await apiStats(page);
    const title = unique('e2e-create');

    await page.getByRole('button', { name: 'New Todo' }).click();
    const dialog = page.locator('.p-dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText('New Todo', { exact: true })).toBeVisible();

    const titleInput = dialog.getByPlaceholder('Title');
    await expect(titleInput).toHaveValue('');
    await expect(dialog.locator('.p-select')).toContainText('Pending');

    const save = dialog.getByRole('button', { name: 'Save' });
    await expect(save).toBeDisabled();

    await titleInput.fill(title);
    await expect(save).toBeEnabled();
    await save.click();
    await expect(dialog).toBeHidden();

    const after = await apiStats(page);
    expect(after.total).toBe(before.total + 1);

    // the new row is last in natural order; prove DOM presence through the search filter
    await searchFor(page, title);
    const row = rowFor(page, title);
    await expect(row).toHaveCount(1);
    const id = Number((await row.locator('td').first().innerText()).trim());

    await page.request.delete(`/api/Todos/${id}`);
    const final = await apiStats(page);
    expect(final.total).toBe(before.total);
  });

  test('edit: dialog opens prefilled, update title and status persists', async ({ page }) => {
    const title = unique('e2e-edit');
    const dto = await createViaApi(page, title, 'Pending');

    await page.goto('/todos');
    await searchFor(page, title);
    const row = rowFor(page, title);

    await row.locator('button:has(.pi-pencil)').click();
    const dialog = page.locator('.p-dialog');
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText('Edit Todo', { exact: true })).toBeVisible();

    const titleInput = dialog.getByPlaceholder('Title');
    await expect(titleInput).toHaveValue(title);
    await expect(dialog.locator('.p-select')).toContainText('Pending');

    const newTitle = `${title}-edited`;
    await titleInput.fill(newTitle);
    await dialog.locator('.p-select').click();
    await page.getByRole('option', { name: 'Completed', exact: true }).click();

    await dialog.getByRole('button', { name: 'Save' }).click();
    await expect(dialog).toBeHidden();

    // the active search ("contains <title>") still matches "<title>-edited"
    const editedRow = rowFor(page, newTitle);
    await expect(editedRow.getByRole('cell', { name: newTitle, exact: true })).toBeVisible();
    await expect(editedRow.getByRole('cell', { name: 'Completed', exact: true })).toBeVisible();

    await page.request.delete(`/api/Todos/${dto.id}`);
  });

  test('cycle-status button advances Pending to InProgress to Completed', async ({ page }) => {
    const title = unique('e2e-cycle');
    const dto = await createViaApi(page, title, 'Pending');

    await page.goto('/todos');
    await searchFor(page, title);
    const row = rowFor(page, title);

    // Pending -> next icon is pi-play (InProgress)
    await row.locator('button:has(.pi-play)').click();
    await expect(row.getByRole('cell', { name: 'InProgress', exact: true })).toBeVisible();

    // InProgress -> next icon is pi-check (Completed)
    await row.locator('button:has(.pi-check)').click();
    await expect(row.getByRole('cell', { name: 'Completed', exact: true })).toBeVisible();

    const stats = await apiStats(page);
    expect(stats.pending + stats.completed).toBe(stats.total);

    await page.request.delete(`/api/Todos/${dto.id}`);
  });

  test('delete: confirm dialog shows the title, accept removes the todo', async ({ page }) => {
    const title = unique('e2e-delete');
    await createViaApi(page, title, 'Pending');
    const before = await apiStats(page);

    await page.goto('/todos');
    await searchFor(page, title);
    const row = rowFor(page, title);

    await row.locator('button:has(.pi-trash)').click();
    const confirm = page.getByRole('alertdialog', { name: 'Confirm Delete' });
    await expect(confirm).toBeVisible();
    await expect(confirm).toContainText(`Delete "${title}"?`);

    await confirm.getByRole('button', { name: 'Yes' }).click();
    await expect(page.getByRole('cell', { name: title, exact: true })).toBeHidden();

    const after = await apiStats(page);
    expect(after.total).toBe(before.total - 1);
  });
});
