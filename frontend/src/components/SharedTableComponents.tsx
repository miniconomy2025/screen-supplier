import React from "react";
import { useTable, useGlobalFilter, useSortBy, Column, HeaderGroup, Row, Cell } from "react-table";
import styles from './reports/ReportsPage.module.scss';
import RefreshButton from "./RefreshButton";

// GlobalFilter component for searching
function GlobalFilter({ globalFilter, setGlobalFilter, placeholder }: { 
  globalFilter: string; 
  setGlobalFilter: (filter: string) => void;
  placeholder?: string;
}) {
  return (
    <span className={styles['global-filter']}>
      Search:{' '}
      <input
        value={globalFilter || ''}
        onChange={e => setGlobalFilter(e.target.value)}
        placeholder={placeholder || "Type to filter..."}
      />
    </span>
  );
}

// StatusSummaryCards component
interface StatusSummaryCardsProps {
  title: string;
  orders: any[];
  statusLabels: Record<string, string>;
  getOrderStatus: (order: any) => string;
}

function StatusSummaryCards({ title, orders, statusLabels, getOrderStatus }: StatusSummaryCardsProps) {
  const colorClasses = [
    'blue', 'pink', 'green', 'orange', 'purple', 'lime', 'indigo', 'deep-orange', 'teal'
  ];
  
  return (
    <div className={styles['status-summary-section']}>
      <h2>{title}</h2>
      <div className={styles['status-cards']}>
        {Object.entries(statusLabels).map(([status, label], idx) => {
          const count = orders.filter(o => getOrderStatus(o) === status).length;
          if (count === 0) return null;
          const colorClass = styles[colorClasses[idx % colorClasses.length]] || '';
          return (
            <div key={status} className={`${styles['status-card']} ${colorClass}`}>
              <span className={styles['label']}>{label}</span>
              <span className={styles['count']}>{count}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// DataTable component
interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  isLoading: boolean;
  onRefresh: () => void;
  title: string;
  noDataMessage?: string;
  searchPlaceholder?: string;
  useOrdersHeader?: boolean;
}

function DataTable<T extends Record<string, any>>({
  data,
  columns,
  isLoading,
  onRefresh,
  title,
  noDataMessage = "No data found",
  searchPlaceholder = "Type to filter...",
  useOrdersHeader = false
}: DataTableProps<T>) {
  const tableInstance = useTable<T>({ columns, data }, useGlobalFilter, useSortBy);
  const {
    getTableProps,
    getTableBodyProps,
    headerGroups,
    rows,
    prepareRow,
    state,
  } = tableInstance;
  
  const setGlobalFilter = (tableInstance as any).setGlobalFilter as (filterValue: string) => void;
  const globalFilter = (state as any).globalFilter as string;

  if (useOrdersHeader) {
    return (
      <div className={styles['orders-table-wrapper']}>
        <div className={styles['orders-header']}>
          <h2 className={styles['orders-title']}>{title}</h2>
          <GlobalFilter 
            globalFilter={globalFilter} 
            setGlobalFilter={setGlobalFilter}
            placeholder={searchPlaceholder}
          />
          <RefreshButton onClick={onRefresh} disabled={isLoading} />
        </div>
        <div className={styles['orders-table-section']}>
          <table {...getTableProps()}>
            <thead>
              {headerGroups.map((headerGroup) => (
                <tr {...headerGroup.getHeaderGroupProps()}>
                  {headerGroup.headers.map((column) => (
                    <th {...column.getHeaderProps((column as any).getSortByToggleProps())}>
                      {column.render('Header') as React.ReactNode}
                      {(column as any).isSorted ? ((column as any).isSortedDesc ? ' ▼' : ' ▲') : ''}
                    </th>
                  ))}
                </tr>
              ))}
            </thead>
            <tbody {...getTableBodyProps()}>
              {rows.map((row) => {
                prepareRow(row);
                return (
                  <tr {...row.getRowProps()}>
                    {row.cells.map((cell: Cell<T>) => (
                      <td {...cell.getCellProps()}>{cell.render('Cell') as React.ReactNode}</td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
          {rows.length === 0 && <p className={styles['no-orders']}>{noDataMessage}</p>}
        </div>
      </div>
    );
  }

  return (
    <div className={styles['orders-table-wrapper']}>
      <section className={styles['orders-table-section']}>
        <div className={styles['tableTitle']}>{title}</div>
        <div className={styles['topControls']}>
          <GlobalFilter 
            globalFilter={globalFilter} 
            setGlobalFilter={setGlobalFilter}
            placeholder={searchPlaceholder}
          />
          <RefreshButton onClick={onRefresh} disabled={isLoading} />
        </div>
        <table {...getTableProps()} className={styles['table']}>
          <thead>
            {headerGroups.map((headerGroup: any) => (
              <tr {...headerGroup.getHeaderGroupProps()}>
                {headerGroup.headers.map((column: any) => (
                  <th {...column.getHeaderProps(column.getSortByToggleProps())} className={styles['th']}>
                    {column.render('Header')}
                    <span>
                      {column.isSorted ? (column.isSortedDesc ? ' 🔽' : ' 🔼') : ''}
                    </span>
                  </th>
                ))}
              </tr>
            ))}
          </thead>
          <tbody {...getTableBodyProps()}>
            {rows.map((row: any) => {
              prepareRow(row);
              return (
                <tr {...row.getRowProps()}>
                  {row.cells.map((cell: any) => (
                    <td {...cell.getCellProps()} className={styles['td']}>
                      {cell.render('Cell')}
                    </td>
                  ))}
                </tr>
              );
            })}
          </tbody>
        </table>
      </section>
    </div>
  );
}

export { StatusSummaryCards, DataTable, GlobalFilter };
