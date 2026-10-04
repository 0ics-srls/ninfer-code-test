import { test, expect, type Page } from '@playwright/test';

// Coverage: 06-weekly-view week page — structure, bucketing, drag persistence, card actions.
// State-invariant: every test creates its own todo(s) with unique e2e-<ts> titles via the API
// and deletes them afterwards; never asserts global seeded counts.

const unique = (prefix: string) => `${prefix}-${Date.now()}`;

// Local date parts, zero-padded — same rule as web/src/app/core/dates.ts, never toISOString().
function isoOf(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

async function createViaApi(page: Page, title: string, status: string, dueDate?: string): Promise<{ id: number }> {
  const res = await page.request.post('/api/Todos', {
    data: dueDate ? { title, status, dueDate } : { title, status }
  });
  expect(res.status()).toBe(201);
  return (await res.json()) as { id: number };
}

function cardFor(page: Page, title: string) {
  return page.locator('.week-card').filter({ hasText: title });
}

function columnFor(page: Page, date: string) {
  return page.locator(`.week-col[data-date="${date}"]`);
}

test.describe('week view: columns, bucketing, drag and drop, card actions', () => {
  test('week page shows eight columns', async ({ page }) => {
    const now = new Date();
    const mondayOffset = (now.getDay() + 6) % 7;
    const monday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - mondayOffset);
    const labels = Array.from({ length: 7 }, (_, i) => {
      const d = new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + i);
      return `${d.toLocaleDateString('en-GB', { weekday: 'short' })} ${d.getDate()}`;
    });

    await page.goto('/week');

    const cols = page.locator('.week-col');
    await expect(cols).toHaveCount(8);
    for (let i = 0; i < 7; i++) {
      await expect(cols.nth(i).locator('.week-col-header')).toHaveText(labels[i]);
    }
    await expect(cols.nth(7).locator('.week-col-header')).toHaveText('Undated');
    await expect(cols.nth(7)).toHaveAttribute('data-date', '');

    await expect(page.locator('.week-col.today')).toHaveCount(1);
    await expect(page.locator('.week-col.today')).toHaveAttribute('data-date', isoOf(now));
  });

  test('dated todos land in correct columns', async ({ page }) => {
    const now = new Date();
    const tToday = unique('e2e-week-today');
    const tPlus2 = unique('e2e-week-plus2');
    const dueToday = isoOf(now);
    const duePlus2 = isoOf(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 2));

    const a = await createViaApi(page, tToday, 'Pending', dueToday);
    const b = await createViaApi(page, tPlus2, 'Pending', duePlus2);

    try {
      await page.goto('/week');

      const colDates: string[] = [];
      for (let i = 0; i < 8; i++) {
        colDates.push((await page.locator('.week-col').nth(i).getAttribute('data-date')) ?? '');
      }

      await expect(cardFor(page, tToday)).toHaveCount(1);
      await expect(columnFor(page, dueToday).locator('.week-card').filter({ hasText: tToday })).toHaveCount(1);

      if (colDates.includes(duePlus2)) {
        await expect(columnFor(page, duePlus2).locator('.week-card').filter({ hasText: tPlus2 })).toHaveCount(1);
        await expect(cardFor(page, tPlus2)).toHaveCount(1);
      } else {
        // today+2 falls in the NEXT week on Sat/Sun runs — then the card appears nowhere
        await expect(cardFor(page, tPlus2)).toHaveCount(0);
      }
    } finally {
      await page.request.delete(`/api/Todos/${a.id}`);
      await page.request.delete(`/api/Todos/${b.id}`);
    }
  });

  test('undated todo appears in Undated column', async ({ page }) => {
    const title = unique('e2e-week-undated');
    const dto = await createViaApi(page, title, 'Pending');

    try {
      await page.goto('/week');
      await expect(columnFor(page, '').locator('.week-card').filter({ hasText: title })).toHaveCount(1);
    } finally {
      await page.request.delete(`/api/Todos/${dto.id}`);
    }
  });

  test('drag card to another day persists', async ({ page }) => {
    const title = unique('e2e-week-drag');
    const dto = await createViaApi(page, title, 'Pending');
    const dueToday = isoOf(new Date());

    try {
      await page.goto('/week');
      const card = cardFor(page, title);
      await expect(card).toHaveCount(1);

      const put = page.waitForResponse(
        r => r.request().method() === 'PUT' && r.url().includes('/api/Todos/'),
        { timeout: 15_000 }
      );
      await card.dragTo(columnFor(page, dueToday));
      expect((await put).status()).toBe(200);

      await page.reload();
      await expect(columnFor(page, dueToday).locator('.week-card').filter({ hasText: title })).toHaveCount(1);
      await expect(columnFor(page, '').locator('.week-card').filter({ hasText: title })).toHaveCount(0);
    } finally {
      await page.request.delete(`/api/Todos/${dto.id}`);
    }
  });

  test('cycle status from card updates chip', async ({ page }) => {
    const title = unique('e2e-week-cycle');
    const dto = await createViaApi(page, title, 'Pending');

    try {
      await page.goto('/week');
      const card = cardFor(page, title);
      await expect(card).toHaveCount(1);

      const chip = card.locator('p-tag');
      await expect(chip).toContainText('Pending');
      await expect(chip).toHaveClass(/p-tag-info/);

      // Pending -> the cycle button shows pi-play (points at InProgress)
      await card.locator('button:has(.pi-play)').click();

      await expect(chip).toContainText('InProgress');
      await expect(chip).toHaveClass(/p-tag-warn/);
    } finally {
      await page.request.delete(`/api/Todos/${dto.id}`);
    }
  });

  test('delete from card removes it', async ({ page }) => {
    const title = unique('e2e-week-delete');
    await createViaApi(page, title, 'Pending');

    await page.goto('/week');
    const card = cardFor(page, title);
    await expect(card).toHaveCount(1);

    await card.locator('button:has(.pi-trash)').click();
    const confirm = page.getByRole('alertdialog', { name: 'Confirm Delete' });
    await expect(confirm).toBeVisible();
    await expect(confirm).toContainText(`Delete "${title}"?`);

    await confirm.getByRole('button', { name: 'Yes' }).click();
    await expect(cardFor(page, title)).toHaveCount(0);
  });
});
