import { parseIsoDate, todayIso } from './dates';

describe('dates', () => {
  it('todayIso and parseIsoDate use local date parts', () => {
    const d = parseIsoDate('2026-09-07');
    expect(d.getFullYear()).toBe(2026);
    expect(d.getMonth()).toBe(8);
    expect(d.getDate()).toBe(7);

    const iso = todayIso();
    expect(iso).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    const now = new Date();
    const expected = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
    expect(iso).toBe(expected);
  });
});
