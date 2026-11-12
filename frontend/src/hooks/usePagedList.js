import { useCallback, useEffect, useMemo, useState } from 'react';
import { getApiError } from '../services/api';
import useDebounce from './useDebounce';

// Shared logic for every list screen: search, sort, paging and loading the data.
// fetcher(params) must return { items, page, pageSize, totalCount, totalPages, ... }
export default function usePagedList(fetcher, { filters = {}, defaultSort, pageSize = 10 }) {
  const [searchText, setSearchText] = useState('');
  const search = useDebounce(searchText, 400);
  const [sort, setSort] = useState(defaultSort);
  const [page, setPage] = useState(1);
  const [reloadCount, setReloadCount] = useState(0);

  // What the server answered, remembered together with the question it answered
  const [result, setResult] = useState(null);

  // The filters object is new on every render, so we compare it by its content
  const filterKey = JSON.stringify(filters);

  const params = useMemo(
    () => ({
      ...JSON.parse(filterKey),
      page,
      pageSize,
      search: search.trim() || undefined,
      sortBy: sort.by,
      descending: sort.descending,
    }),
    [filterKey, page, pageSize, search, sort]
  );

  useEffect(() => {
    let cancelled = false;
    fetcher(params)
      .then((data) => {
        if (!cancelled) setResult({ params, reloadCount, data, error: null });
      })
      .catch((error) => {
        if (!cancelled) setResult({ params, reloadCount, data: null, error: getApiError(error).message });
      });
    return () => {
      cancelled = true;
    };
  }, [fetcher, params, reloadCount]);

  const loading = !result || result.params !== params || result.reloadCount !== reloadCount;
  const data = result?.data ?? null;
  const error = loading ? null : result.error;

  const reload = useCallback(() => setReloadCount((count) => count + 1), []);

  const changeSearch = useCallback((value) => {
    setSearchText(value);
    setPage(1);
  }, []);

  const toggleSort = useCallback((column) => {
    setSort((current) =>
      current.by === column
        ? { by: column, descending: !current.descending }
        : { by: column, descending: false }
    );
    setPage(1);
  }, []);

  const goToFirstPage = useCallback(() => setPage(1), []);

  // Call after deleting a row: step back a page if that page is now empty
  const afterRemove = () => {
    if (data && data.items.length === 1 && page > 1) setPage(page - 1);
    else reload();
  };

  return {
    data,
    loading,
    error,
    searchText,
    hasSearch: Boolean(search.trim()),
    sort,
    page,
    setPage,
    changeSearch,
    toggleSort,
    goToFirstPage,
    reload,
    afterRemove,
  };
}
