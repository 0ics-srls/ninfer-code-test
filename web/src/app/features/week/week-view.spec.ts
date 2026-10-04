import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ConfirmationService } from 'primeng/api';
import { Api } from '../../generated/api';
import type { TodoDto, TodoUpdateDto, WeekResponseDto } from '../../generated/models';
import { nextStatus, nextStatusOf } from '../../core/todo-status';
import { todayIso } from '../../core/dates';
import { WeekView } from './week-view';

function makeTodo(overrides: Partial<TodoDto>): TodoDto {
  return {
    id: 7,
    title: 'Task',
    status: 'Pending',
    createdAt: '2026-09-01T00:00:00Z',
    dueDate: null,
    ...overrides
  };
}

function fixedWeek(): WeekResponseDto {
  return {
    days: [
      { date: '2026-09-07', todos: [] },
      { date: '2026-09-08', todos: [] },
      { date: '2026-09-09', todos: [] },
      { date: '2026-09-10', todos: [] },
      { date: '2026-09-11', todos: [] },
      { date: '2026-09-12', todos: [] },
      { date: '2026-09-13', todos: [] }
    ],
    undated: []
  };
}

function isoOf(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

describe('WeekView', () => {
  let invoke: ReturnType<typeof vi.fn>;

  async function create(week: WeekResponseDto) {
    invoke = vi.fn().mockResolvedValue(week);
    TestBed.configureTestingModule({
      imports: [WeekView],
      providers: [
        { provide: Api, useValue: { invoke } },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: vi.fn(),
            requireConfirmation$: { subscribe: () => ({ unsubscribe: () => undefined }) }
          }
        }
      ]
    });
    const fixture = TestBed.createComponent(WeekView);
    fixture.detectChanges();
    await new Promise(r => setTimeout(r, 0));
    fixture.detectChanges();
    return { fixture, view: fixture.componentInstance };
  }

  it('renders eight column headers from a week fixture', async () => {
    const { fixture } = await create(fixedWeek());

    const cols = Array.from(fixture.nativeElement.querySelectorAll('.week-col')) as Element[];
    expect(cols).toHaveLength(8);
    const headers = cols.map(c => (c.querySelector('.week-col-header')?.textContent ?? '').trim());
    const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    for (let i = 0; i < 7; i++) {
      expect(headers[i]).toContain(weekdays[i]);
    }
    expect(headers[0]).toContain('7');
    expect(headers[7]).toBe('Undated');
    expect(cols[0].getAttribute('data-date')).toBe('2026-09-07');
    expect(cols[7].getAttribute('data-date')).toBe('');
  });

  it('flags today column', async () => {
    const now = new Date();
    const mondayOffset = (now.getDay() + 6) % 7;
    const monday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - mondayOffset);
    const week: WeekResponseDto = {
      days: Array.from({ length: 7 }, (_, i) => ({
        date: isoOf(new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + i)),
        todos: []
      })),
      undated: []
    };

    const { fixture } = await create(week);

    const flagged = fixture.nativeElement.querySelectorAll('.week-col.today');
    expect(flagged).toHaveLength(1);
    expect(flagged[0].getAttribute('data-date')).toBe(todayIso());
  });

  it('renders one card per todo in its day bucket', async () => {
    const week = fixedWeek();
    week.days[0].todos = [makeTodo({ id: 1, title: 'Monday task', dueDate: '2026-09-07' })];
    week.days[3].todos = [makeTodo({ id: 2, title: 'Thursday task', dueDate: '2026-09-10' })];
    week.undated = [makeTodo({ id: 3, title: 'Undated task', dueDate: null })];

    const { fixture } = await create(week);

    const mon = fixture.nativeElement.querySelector('.week-col[data-date="2026-09-07"]');
    expect(mon).toBeTruthy();
    const monCards = mon!.querySelectorAll('.week-card');
    expect(monCards).toHaveLength(1);
    expect(monCards[0].textContent).toContain('Monday task');

    const thu = fixture.nativeElement.querySelector('.week-col[data-date="2026-09-10"]');
    const thuCards = thu!.querySelectorAll('.week-card');
    expect(thuCards).toHaveLength(1);
    expect(thuCards[0].textContent).toContain('Thursday task');

    const undated = fixture.nativeElement.querySelector('.week-col[data-date=""]');
    expect(undated).toBeTruthy();
    const undatedCards = undated!.querySelectorAll('.week-card');
    expect(undatedCards).toHaveLength(1);
    expect(undatedCards[0].textContent).toContain('Undated task');
  });

  it('cycleStatus invokes update with next status', async () => {
    const todo = makeTodo({ id: 7, title: 'Cycle me', status: 'Pending', dueDate: '2026-09-08' });
    const week = fixedWeek();
    week.days[1].todos = [todo];
    const { view } = await create(week);

    await view.cycleStatus(todo);

    const putCall = invoke.mock.calls.find(([, params]) => (params as { id?: string } | undefined)?.id === '7');
    expect(putCall).toBeDefined();
    const params = putCall![1] as { id: string; body: TodoUpdateDto };
    expect(params.id).toBe('7');
    expect(params.body.status).toBe('InProgress');
    expect(params.body.title).toBe('Cycle me');
  });

  it('nextStatusOf matches the shared map used by the table view', () => {
    expect(nextStatusOf(makeTodo({ status: 'Pending' }))).toBe('InProgress');
    expect(nextStatusOf(makeTodo({ status: 'InProgress' }))).toBe('Completed');
    expect(nextStatusOf(makeTodo({ status: 'Completed' }))).toBe('Pending');
    expect(nextStatus).toEqual({
      Pending: 'InProgress',
      InProgress: 'Completed',
      Completed: 'Pending'
    });
  });

  it('onDrop_OnDayColumn_UpdatesDueDate', async () => {
    const todo = makeTodo({ id: 7, title: 'Drop me', status: 'Pending', dueDate: null });
    const week = fixedWeek();
    week.undated = [todo];
    const { view } = await create(week);

    view.dragged = todo;
    await view.onDrop({ label: 'Mon 7', date: '2026-09-07', isToday: false, todos: [] });
    await new Promise(r => setTimeout(r, 0));

    const putCall = invoke.mock.calls.find(([, params]) => (params as { id?: string } | undefined)?.id === '7');
    expect(putCall).toBeDefined();
    const params = putCall![1] as { id: string; body: TodoUpdateDto };
    expect(params.body.dueDate).toBe('2026-09-07');
  });

  it('onDrop_OnUndatedColumn_ClearsDueDate', async () => {
    const todo = makeTodo({ id: 8, title: 'Clear me', status: 'Pending', dueDate: '2026-09-08' });
    const week = fixedWeek();
    week.days[1].todos = [todo];
    const { view } = await create(week);

    view.dragged = todo;
    await view.onDrop({ label: 'Undated', date: null, isToday: false, todos: [] });
    await new Promise(r => setTimeout(r, 0));

    const putCall = invoke.mock.calls.find(([, params]) => (params as { id?: string } | undefined)?.id === '8');
    expect(putCall).toBeDefined();
    const params = putCall![1] as { id: string; body: TodoUpdateDto };
    expect(params.body.dueDate).toBeUndefined();
  });

  it('onDrop_SameBucket_NoApiCall', async () => {
    const todo = makeTodo({ id: 9, title: 'Stay put', status: 'Pending', dueDate: '2026-09-07' });
    const week = fixedWeek();
    week.days[0].todos = [todo];
    const { view } = await create(week);

    view.dragged = todo;
    await view.onDrop({ label: 'Mon 7', date: '2026-09-07', isToday: false, todos: [] });
    await new Promise(r => setTimeout(r, 0));

    const putCalls = invoke.mock.calls.filter(([, params]) => (params as { id?: string } | undefined)?.id !== undefined);
    expect(putCalls).toHaveLength(0);
  });

  it('onDrop_Failure_LogsAndReloads', async () => {
    const todo = makeTodo({ id: 10, title: 'Fail me', status: 'Pending', dueDate: null });
    const week = fixedWeek();
    week.undated = [todo];
    const { view } = await create(week);

    const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    invoke.mockRejectedValueOnce(new Error('boom'));

    view.dragged = todo;
    await view.onDrop({ label: 'Mon 7', date: '2026-09-07', isToday: false, todos: [] });
    await new Promise(r => setTimeout(r, 0));

    expect(errorSpy).toHaveBeenCalled();
    expect(invoke).toHaveBeenCalledTimes(3);
    errorSpy.mockRestore();
  });
});
