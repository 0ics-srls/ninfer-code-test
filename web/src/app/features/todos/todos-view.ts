import { Component, ChangeDetectionStrategy, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import type { SelectChangeEvent } from 'primeng/types/select';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { Api } from '../../generated/api';
import { ApiTodosGet$Json$Params, apiTodosGet$Json, apiTodosPost$Json, apiTodosStatsGet$Json, apiTodosIdPut$Json, apiTodosIdDelete } from '../../generated/functions';
import { TodoCreateDto, TodoDto, TodoStatus, TodoUpdateDto } from '../../generated/models';
import { buildLoadParams, TableFilters, TableLazyLoadEvent } from '../../core/load-options.builder';
import { nextStatusOf as sharedNextStatusOf } from '../../core/todo-status';
import { TodoDialog, TodoSavedPayload } from './todo-dialog';

@Component({
  selector: 'app-todos-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TableModule, CardModule, ConfirmDialogModule, SelectModule, DatePickerModule, InputTextModule, ButtonModule, TooltipModule, FormsModule, TodoDialog],
  templateUrl: './todos-view.html'
})
export class TodosView implements OnInit, OnDestroy {
  private readonly api = inject(Api);
  private readonly confirmation = inject(ConfirmationService);

  readonly todos = signal<TodoDto[]>([]);
  readonly totalRecords = signal(0);

  readonly statsTotal = signal(0);
  readonly statsCompleted = signal(0);
  readonly statsPending = signal(0);

  readonly filters = signal<TableFilters>({ title: '', status: 'All', createdAtRange: null });

  readonly searchTerm = signal('');
  filterStatus: TodoStatus | 'All' = 'All';
  filterRange: (Date | null)[] | null = null;

  private lastLazyEvent: TableLazyLoadEvent = { first: 0, rows: 10 };

  private readonly searchSubject = new Subject<void>();
  private searchSub?: Subscription;

  readonly dialogVisible = signal(false);
  readonly editing = signal<TodoDto | null>(null);

  readonly statusOptions: { label: string; value: TodoStatus }[] = [
    { label: 'Pending', value: 'Pending' },
    { label: 'InProgress', value: 'InProgress' },
    { label: 'Completed', value: 'Completed' }
  ];

  readonly filterStatusOptions: { label: string; value: TodoStatus | 'All' }[] = [
    { label: 'All', value: 'All' },
    ...this.statusOptions
  ];

  readonly statusIcons: Record<TodoStatus, string> = {
    Pending: 'pi pi-clock',
    InProgress: 'pi pi-play',
    Completed: 'pi pi-check'
  };

  async ngOnInit(): Promise<void> {
    this.searchSub = this.searchSubject.pipe(debounceTime(400)).subscribe(() => this.applyFilters());
    await this.loadStats();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    this.searchSubject.next();
  }

  onStatusFilterChange(event: SelectChangeEvent): void {
    this.filterStatus = event.value as TodoStatus | 'All';
    this.applyFilters();
  }

  onRangeSelect(): void {
    // PrimeNG emits the clicked Date, but the ngModel-bound filterRange already holds [start, end] (or [start, null] while picking).
    this.applyFilters();
  }

  onRangeClear(): void {
    this.filterRange = null;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.filterStatus = 'All';
    this.filterRange = null;
    this.applyFilters();
  }

  applyFilters(): void {
    this.filters.set({ title: this.searchTerm(), status: this.filterStatus, createdAtRange: this.filterRange });
    this.lastLazyEvent = { ...this.lastLazyEvent, first: 0 };
    void this.reload();
  }

  async loadTodos(event: TableLazyLoadEvent): Promise<void> {
    this.lastLazyEvent = event;
    try {
      const p = buildLoadParams(event, this.filters());
      const params: ApiTodosGet$Json$Params = {
        RequireTotalCount: p['requireTotalCount'] === 'true',
        Skip: p['skip'],
        Take: p['take'],
        Sort: (p['sort'] ?? undefined) as unknown as ApiTodosGet$Json$Params['Sort'],
        Filter: (p['filter'] ?? undefined) as unknown as ApiTodosGet$Json$Params['Filter']
      };
      const res = await this.api.invoke(apiTodosGet$Json, params);
      this.todos.set((res.data ?? []) as TodoDto[]);
      this.totalRecords.set(Number(res.totalCount ?? 0));
    } catch (err) {
      console.error('loadTodos failed', err);
      this.todos.set([]);
      this.totalRecords.set(0);
    }
  }

  private async loadStats(): Promise<void> {
    try {
      const stats = await this.api.invoke(apiTodosStatsGet$Json);
      this.statsTotal.set(Number(stats.total ?? 0));
      this.statsCompleted.set(Number(stats.completed ?? 0));
      this.statsPending.set(Number(stats.pending ?? 0));
    } catch (err) {
      console.error('loadStats failed', err);
    }
  }

  private async reload(): Promise<void> {
    await Promise.all([this.loadTodos(this.lastLazyEvent), this.loadStats()]);
  }

  openCreate(): void {
    this.editing.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(todo: TodoDto): void {
    this.editing.set(todo);
    this.dialogVisible.set(true);
  }

  async onSaved(payload: TodoSavedPayload): Promise<void> {
    try {
      if (payload.id === null) {
        const body: TodoCreateDto = { title: payload.title, status: payload.status, dueDate: payload.dueDate };
        await this.api.invoke(apiTodosPost$Json, { body });
      } else {
        const body: TodoUpdateDto = { title: payload.title, status: payload.status, dueDate: payload.dueDate };
        await this.api.invoke(apiTodosIdPut$Json, { id: String(payload.id), body });
      }
      await this.reload();
    } catch (err) {
      console.error('saveTodo failed', err);
    }
  }

  nextStatusOf(todo: TodoDto): TodoStatus {
    return sharedNextStatusOf(todo);
  }

  statusIconForNext(todo: TodoDto): string {
    return this.statusIcons[sharedNextStatusOf(todo)];
  }

  cycleStatus(todo: TodoDto): void {
    this.setStatus(todo, this.nextStatusOf(todo));
  }

  async setStatus(todo: TodoDto, status: TodoStatus): Promise<void> {
    if (todo.status === status) return;
    try {
      const body: TodoUpdateDto = { title: todo.title, status };
      await this.api.invoke(apiTodosIdPut$Json, { id: String(todo.id), body });
      await this.reload();
    } catch (err) {
      console.error('setStatus failed', err);
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
          await this.reload();
        } catch (err) {
          console.error('deleteTodo failed', err);
        }
      }
    });
  }
}
