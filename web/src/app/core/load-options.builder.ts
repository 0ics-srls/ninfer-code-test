import type { TodoStatus } from '../generated/models';

export interface TableFilters {
  title: string;
  status: TodoStatus | 'All';
  createdAtRange: (Date | null)[] | null;
}

export type LoadParams = Record<string, string>;

// Structural mirror of primeng TableLazyLoadEvent (lazy load metadata) — keeps this builder dependency-free.
export interface TableLazyLoadEvent {
  first?: number | null;
  rows?: number | null;
  sortField?: string | string[] | null;
  sortOrder?: number | null;
}

function camelCase(s: string): string {
  return s.charAt(0).toLowerCase() + s.slice(1);
}

function endOfDay(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate(), 23, 59, 59, 999);
}

export function buildLoadParams(event: TableLazyLoadEvent, filters: TableFilters): LoadParams {
  const params: LoadParams = {
    skip: String(event.first ?? 0),
    take: String(event.rows ?? 10),
    requireTotalCount: 'true'
  };
  if (typeof event.sortField === 'string') {
    params['sort'] = JSON.stringify([{ selector: camelCase(event.sortField), desc: event.sortOrder === -1 }]);
  }
  const conds: string[][] = [];
  if (filters.title) {
    conds.push(['title', 'contains', filters.title]);
  }
  if (filters.status !== 'All') {
    conds.push(['status', '=', filters.status]);
  }
  const [from, to] = filters.createdAtRange ?? [null, null];
  if (from) {
    conds.push(['createdAt', '>=', from.toISOString()]);
  }
  if (to) {
    conds.push(['createdAt', '<=', endOfDay(to).toISOString()]);
  }
  if (conds.length === 1) {
    params['filter'] = JSON.stringify(conds[0]);
  } else if (conds.length > 1) {
    const joined: unknown[] = [conds[0]];
    for (let i = 1; i < conds.length; i++) {
      joined.push('and', conds[i]);
    }
    params['filter'] = JSON.stringify(joined);
  }
  return params;
}
