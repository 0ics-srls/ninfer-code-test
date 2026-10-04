import { ChangeDetectionStrategy, Component, computed, effect, input, model, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TodoDto, TodoStatus } from '../../generated/models';

export interface TodoSavedPayload {
  id: number | null;
  title: string;
  status: TodoStatus;
  dueDate: string | undefined;
}

@Component({
  selector: 'app-todo-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DialogModule, InputTextModule, SelectModule, DatePickerModule, ButtonModule, ReactiveFormsModule],
  templateUrl: './todo-dialog.html'
})
export class TodoDialog {
  readonly visible = model(false);
  readonly editing = input<TodoDto | null>(null);
  readonly saved = output<TodoSavedPayload>();

  readonly title = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(200)] });
  readonly status = new FormControl<TodoStatus>('Pending', { nonNullable: true });
  readonly dueDate = new FormControl<string | null | undefined>(undefined);
  readonly form = new FormGroup({ title: this.title, status: this.status, dueDate: this.dueDate });

  readonly statusOptions: { label: string; value: TodoStatus }[] = [
    { label: 'Pending', value: 'Pending' },
    { label: 'InProgress', value: 'InProgress' },
    { label: 'Completed', value: 'Completed' }
  ];

  readonly header = computed(() => (this.editing() !== null ? 'Edit Todo' : 'New Todo'));

  private prefilledKey: string | null = null;

  constructor() {
    // Prefill only when the (visible, todo) state actually changes. The `editing`
    // input re-notifies with the same reference on parent CD cycles, so an unguarded
    // effect would re-run while the dialog is open and stomp the user's edits.
    effect(() => {
      const todo = this.editing();
      const key = this.visible() ? `todo:${todo?.id ?? 'new'}` : 'closed';
      if (key === this.prefilledKey) {
        return;
      }
      this.prefilledKey = key;
      if (!this.visible()) {
        return;
      }
      this.title.setValue(todo?.title ?? '', { emitEvent: false });
      this.status.setValue(todo?.status ?? 'Pending', { emitEvent: false });
      this.dueDate.setValue(todo?.dueDate ?? undefined, { emitEvent: false });
    });
  }

  save(): void {
    if (this.title.invalid) return;
    const editing = this.editing();
    this.saved.emit({
      id: editing !== null ? Number(editing.id) : null,
      title: this.title.value,
      status: this.status.value,
      dueDate: this.dueDate.value ?? undefined
    });
    this.visible.set(false);
  }
}
