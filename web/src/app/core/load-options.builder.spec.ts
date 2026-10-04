import { buildLoadParams, type TableFilters, type TableLazyLoadEvent } from './load-options.builder';

const emptyFilters: TableFilters = { title: '', status: 'All', createdAtRange: null };
const emptyEvent = { first: 0, rows: 10 } as TableLazyLoadEvent;

describe('buildLoadParams', () => {
  it('buildLoadParams_MapsPaging', () => {
    const params = buildLoadParams({ first: 20, rows: 10 } as TableLazyLoadEvent, emptyFilters);

    expect(params).toEqual({ skip: '20', take: '10', requireTotalCount: 'true' });
  });

  it('buildLoadParams_MapsSortAscAndDesc', () => {
    const asc = buildLoadParams({ first: 0, rows: 10, sortField: 'title', sortOrder: 1 } as TableLazyLoadEvent, emptyFilters);
    const desc = buildLoadParams({ first: 0, rows: 10, sortField: 'title', sortOrder: -1 } as TableLazyLoadEvent, emptyFilters);

    expect(asc['sort']).toBe('[{"selector":"title","desc":false}]');
    expect(desc['sort']).toBe('[{"selector":"title","desc":true}]');
  });

  it('buildLoadParams_OmitsSort_WhenNoSortField', () => {
    const params = buildLoadParams(emptyEvent, emptyFilters);

    expect(params).not.toHaveProperty('sort');
  });

  it('buildLoadParams_MapsTitleContainsFilter', () => {
    const params = buildLoadParams(emptyEvent, { title: 'abc', status: 'All', createdAtRange: null });

    expect(params['filter']).toBe('["title","contains","abc"]');
  });

  it('buildLoadParams_MapsStatusFilter_SkipsAll', () => {
    const params = buildLoadParams(emptyEvent, { title: '', status: 'Pending', createdAtRange: null });
    const all = buildLoadParams(emptyEvent, emptyFilters);

    expect(params['filter']).toContain('["status","=","Pending"]');
    expect(all).not.toHaveProperty('filter');
  });

  it('buildLoadParams_MapsDateRange_AndCombinesWithAnd', () => {
    const from = new Date(2026, 0, 1);
    const to = new Date(2026, 11, 31);
    const endOfDay = new Date(to.getFullYear(), to.getMonth(), to.getDate(), 23, 59, 59, 999);
    const params = buildLoadParams(emptyEvent, { title: 'x', status: 'All', createdAtRange: [from, to] });

    const expected = JSON.stringify([
      ['title', 'contains', 'x'],
      'and',
      ['createdAt', '>=', from.toISOString()],
      'and',
      ['createdAt', '<=', endOfDay.toISOString()]
    ]);
    expect(params['filter']).toBe(expected);
  });

  it('buildLoadParams_NoFilters_OmitsFilterKey', () => {
    const params = buildLoadParams(emptyEvent, emptyFilters);

    expect(params).not.toHaveProperty('filter');
  });
});
