import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { DragDropModule } from 'primeng/dragdrop';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip';
import { Api } from '../../generated/api';
import { apiTodosWeekGet$Json, apiTodosIdPut$Json, apiTodosIdDelete } from '../../generated/functions';
import type { TodoDto, TodoStatus, TodoUpdateDto, WeekResponseDto } from '../../generated/models';
import { nextStatusOf as sharedNextStatusOf } from '../../core/todo-status';
import { parseIsoDate, todayIso } from '../../core/dates';
import { TodoDialog, TodoSavedPayload } from '../todos/todo-dialog';

interface WeekColumn {
  label: string;
  date: string | null;
  isToday: boolean;
  todos: TodoDto[];
}

@Component({
  selector: 'app-week-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagModule, ButtonModule, DragDropModule, ConfirmDialogModule, TooltipModule, TodoDialog],
  templateUrl: './week-view.html'
})
export class WeekView implements OnInit {
  private readonly api = inject(Api);
  private readonly confirmation = inject(ConfirmationService);

  readonly week = signal<WeekColumn[]>([]);
  readonly dialogVisible = signal(false);
  readonly editing = signal<TodoDto | null>(null);
  dragged: TodoDto | null = null;

  readonly statusIcons: Record<TodoStatus, string> = {
    Pending: 'pi pi-clock',
    InProgress: 'pi pi-play',
    Completed: 'pi pi-check'
  };

  readonly statusSeverity: Record<TodoStatus, 'info' | 'warn' | 'success'> = {
    Pending: 'info',
    InProgress: 'warn',
    Completed: 'success'
  };

  ngOnInit(): void {
    void this.loadWeek();
  }

  async loadWeek(): Promise<void> {
    try {
      const res = await this.api.invoke(apiTodosWeekGet$Json);
      this.week.set(this.toColumns(res));
    } catch (err) {
      console.error('loadWeek failed', err);
      this.week.set([]);
    }
  }

  private toColumns(res: WeekResponseDto): WeekColumn[] {
    const today = todayIso();
    const dayColumns: WeekColumn[] = res.days.map(day => ({
      label: `${parseIsoDate(day.date).toLocaleDateString('en-GB', { weekday: 'short' })} ${parseIsoDate(day.date).getDate()}`,
      date: day.date,
      isToday: day.date === today,
      todos: day.todos
    }));
    return [...dayColumns, { label: 'Undated', date: null, isToday: false, todos: res.undated }];
  }

  nextStatusOf(todo: TodoDto): TodoStatus {
    return sharedNextStatusOf(todo);
  }

  onDrop(col: WeekColumn): void {
    const todo = this.dragged;
    if (!todo) return;
    this.dragged = null;
    const newDate = col.date === null ? undefined : col.date;
    if ((todo.dueDate ?? null) === (col.date ?? null)) return;
    this.week.update(cols => cols.map(c => {
      if (c.date === col.date) {
        return { ...c, todos: [...c.todos.filter(t => t.id !== todo.id), todo] };
      }
      return { ...c, todos: c.todos.filter(t => t.id !== todo.id) };
    }));
    this.api.invoke(apiTodosIdPut$Json, { id: String(todo.id), body: { title: todo.title, status: todo.status, dueDate: newDate } })
      .then(() => this.loadWeek())
      .catch((err) => { console.error('week drop failed', err); this.loadWeek(); });
  }

  openEdit(todo: TodoDto): void {
    this.editing.set(todo);
    this.dialogVisible.set(true);
  }

  async onSaved(payload: TodoSavedPayload): Promise<void> {
    if (payload.id === null) return;
    try {
      const body: TodoUpdateDto = { title: payload.title, status: payload.status, dueDate: payload.dueDate };
      await this.api.invoke(apiTodosIdPut$Json, { id: String(payload.id), body });
      await this.loadWeek();
    } catch (err) {
      console.error('saveTodo failed', err);
    }
  }

  async cycleStatus(todo: TodoDto): Promise<void> {
    if (todo.status === this.nextStatusOf(todo)) return;
    try {
      const body: TodoUpdateDto = { title: todo.title, status: this.nextStatusOf(todo), dueDate: todo.dueDate ?? undefined };
      await this.api.invoke(apiTodosIdPut$Json, { id: String(todo.id), body });
      await this.loadWeek();
    } catch (err) {
      console.error('cycleStatus failed', err);
    }
  }

  confirmDelete(todo: TodoDto): void {
    this.confirmation.confirm({
      message: `Delete "${todo.title}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: async () => {
        try {
          await this.api.invoke(apiTodosIdDelete, { id: String(todo.id) });
          await this.loadWeek();
        } catch (err) {
          console.error('deleteTodo failed', err);
        }
      }
    });
  }
}
