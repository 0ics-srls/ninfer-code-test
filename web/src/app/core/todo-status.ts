import type { TodoDto, TodoStatus } from '../generated/models';

export const nextStatus: Record<TodoStatus, TodoStatus> = {
  Pending: 'InProgress',
  InProgress: 'Completed',
  Completed: 'Pending'
};

export function nextStatusOf(todo: TodoDto): TodoStatus {
  return nextStatus[todo.status];
}
