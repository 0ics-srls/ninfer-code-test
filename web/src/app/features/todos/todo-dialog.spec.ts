import { TestBed } from '@angular/core/testing';
import { TodoDialog } from './todo-dialog';
import type { TodoDto, TodoStatus } from '../../generated/models';

function makeTodo(overrides: Partial<TodoDto>): TodoDto {
  return {
    id: 7,
    title: 'Test title',
    status: 'InProgress',
    createdAt: '2026-09-01T00:00:00Z',
    dueDate: '2026-09-07',
    ...overrides
  };
}

describe('TodoDialog', () => {
  function create(editing: TodoDto | null, visible = true) {
    const fixture = TestBed.createComponent(TodoDialog);
    fixture.componentRef.setInput('editing', editing);
    fixture.componentRef.setInput('visible', visible);
    fixture.detectChanges();
    return { fixture, dialog: fixture.componentInstance };
  }

  function inputValues(fixture: { nativeElement: HTMLElement }): string[] {
    return Array.from(fixture.nativeElement.querySelectorAll('input')).map(i => i.value);
  }

  function clickSave(fixture: { nativeElement: HTMLElement; detectChanges(): void }): void {
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement | null;
    expect(button).toBeTruthy();
    button?.click();
    fixture.detectChanges();
  }

  it('renders title, status and date fields', async () => {
    const { fixture, dialog } = create(makeTodo({ id: 7, title: 'Test title', status: 'InProgress', dueDate: '2026-09-07' }));
    await fixture.whenStable();
    fixture.detectChanges();

    const values = inputValues(fixture);
    expect(values).toContain('Test title');
    expect(values).toContain('2026-09-07');
    expect(dialog.title.value).toBe('Test title');
    expect(dialog.status.value).toBe('InProgress');
    expect(dialog.dueDate.value).toBe('2026-09-07');
  });

  it('save emits payload with iso dueDate', async () => {
    const { fixture, dialog } = create(makeTodo({ title: 'Dated task', status: 'Pending', dueDate: '2026-09-07' }));
    await fixture.whenStable();
    fixture.detectChanges();

    let emitted: { id: number | null; title: string; status: TodoStatus; dueDate: string | undefined } | undefined;
    dialog.saved.subscribe(p => (emitted = p));
    dialog.dueDate.setValue('2026-09-07');
    clickSave(fixture);

    expect(emitted).toBeDefined();
    expect(emitted?.id).toBe(7);
    expect(emitted?.title).toBe('Dated task');
    expect(emitted?.status).toBe('Pending');
    expect(emitted?.dueDate).toBe('2026-09-07');
  });

  it('cleared date emits undefined', async () => {
    const { fixture, dialog } = create(makeTodo({ title: 'Dated task', status: 'Pending', dueDate: '2026-09-07' }));
    await fixture.whenStable();
    fixture.detectChanges();

    let emitted: { id: number | null; title: string; status: TodoStatus; dueDate: string | undefined } | undefined;
    dialog.saved.subscribe(p => (emitted = p));
    dialog.dueDate.setValue(null);
    clickSave(fixture);

    expect(emitted).toBeDefined();
    expect(emitted?.id).toBe(7);
    expect(emitted?.dueDate).toBeUndefined();
  });

  it('create mode emits null id and empty date', async () => {
    const { fixture, dialog } = create(null);
    await fixture.whenStable();
    fixture.detectChanges();

    let emitted: { id: number | null; title: string; status: TodoStatus; dueDate: string | undefined } | undefined;
    dialog.saved.subscribe(p => (emitted = p));
    dialog.title.setValue('Brand new');
    clickSave(fixture);

    expect(emitted).toBeDefined();
    expect(emitted?.id).toBeNull();
    expect(emitted?.title).toBe('Brand new');
    expect(emitted?.dueDate).toBeUndefined();
  });
});
