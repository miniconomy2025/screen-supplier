import React, { useEffect, useState } from "react";
import { apiClient, ScreenOrder } from "../../apiClient";
import { useTable, useGlobalFilter, useSortBy, Column, HeaderGroup, Row, Cell } from "react-table";
import styles from './ReportsPage.module.scss';

// GlobalFilter component for searching
function GlobalFilter({ globalFilter, setGlobalFilter }: { globalFilter: string; setGlobalFilter: (filter: string) => void }) {
  return (
    <span className={styles['global-filter']}>
      Search:{' '}
      <input
        value={globalFilter || ''}
        onChange={e => setGlobalFilter(e.target.value)}
        placeholder="Type to filter orders..."
      />
    </span>
  );
}

const STATUS_LABELS: Record<string, string> = {
  waiting_payment: 'Waiting Payment',
  waiting_collection: 'Waiting Collection',
  collected: 'Collected',
  requires_payment_supplier: 'Requires Payment Supplier',
  requires_delivery: 'Requires Delivery',
  requires_payment_delivery: 'Requires Payment Delivery',
  waiting_delivery: 'Waiting Delivery',
  delivered: 'Delivered',
  abandoned: 'Abandoned',
};

// Helper to get status string from order
const getOrderStatus = (order: ScreenOrder): string => {
  const raw = order.orderStatus && (typeof order.orderStatus === 'object')
    ? ('status' in order.orderStatus ? order.orderStatus.status : order.orderStatus.name)
    : order.orderStatus;
  return typeof raw === 'string' ? raw.toLowerCase() : '';
}

// Helper to get capitalized label
function getStatusLabel(status: string): string {
  return STATUS_LABELS[status] || (status.charAt(0).toUpperCase() + status.slice(1));
}

// --- StatusSummaryCards component ---
function StatusSummaryCards({ orders }: { orders: ScreenOrder[] }) {
  const colorClasses = [
    'blue', 'pink', 'green', 'orange', 'purple', 'lime', 'indigo', 'deep-orange', 'teal'
  ];
  return (
    <div className={styles['status-summary-section']}>
      <h2>Orders in the Queue</h2>
      <div className={styles['status-cards']}>
        {Object.entries(STATUS_LABELS).map(([status, label], idx) => {
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

// --- OrdersTable component ---
function OrdersTable({
  headerGroups,
  rows,
  prepareRow,
  getTableProps,
  getTableBodyProps,
}: {
  headerGroups: HeaderGroup<ScreenOrder>[];
  rows: Row<ScreenOrder>[];
  prepareRow: (row: Row<ScreenOrder>) => void;
  getTableProps: () => any;
  getTableBodyProps: () => any;
}) {
  return (
    <div className={styles['orders-table-section']}>
      <table {...getTableProps()}>
        <thead>
          {headerGroups.map((headerGroup) => (
            <tr {...headerGroup.getHeaderGroupProps()}>
              {headerGroup.headers.map((column) => (
                <th
                  {...column.getHeaderProps((column as any).getSortByToggleProps())}
                >
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
                {row.cells.map((cell: Cell<ScreenOrder>) => (
                  <td {...cell.getCellProps()}>{cell.render('Cell') as React.ReactNode}</td>
                ))}
              </tr>
            );
          })}
        </tbody>
      </table>
      {rows.length === 0 && <p className={styles['no-orders']}>No orders found for the last 90 days.</p>}
    </div>
  );
}

interface ReportsPageProps {
  refreshKey?: number;
}

const ReportsPage: React.FC<ReportsPageProps> = ({ refreshKey }) => {
  const [orders, setOrders] = useState<ScreenOrder[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchOrders = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await apiClient.getOrdersPeriod(90);
      setOrders(data);
    } catch (err: any) {
      setError(err.message || "Failed to fetch orders");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, [refreshKey]);

  // Table columns
  const columns: Column<ScreenOrder>[] = React.useMemo(() => [
    { Header: 'Date', accessor: (row: ScreenOrder) => new Date(row.orderDate).toLocaleDateString(), id: 'orderDate' },
    { Header: 'Product', accessor: () => 'Screen', id: 'product' },
    { Header: 'Quantity', accessor: 'quantity' },
    { Header: 'Unit Price', accessor: (row: ScreenOrder) => `Ð${row.unitPrice.toLocaleString()}`, id: 'unitPrice' },
    { Header: 'Amount Paid', accessor: (row: ScreenOrder) => row.amountPaid != null ? `Ð${row.amountPaid.toLocaleString()}` : '-', id: 'amountPaid' },
    { Header: 'Status', accessor: (row: ScreenOrder) => getStatusLabel(getOrderStatus(row)), id: 'status' },
  ], []);

  const tableInstance = useTable<ScreenOrder>({ columns, data: orders }, useGlobalFilter, useSortBy);
  const {
    getTableProps,
    getTableBodyProps,
    headerGroups,
    rows,
    prepareRow,
    state,
  } = tableInstance;
  // Type assertion for setGlobalFilter and globalFilter
  const setGlobalFilter = (tableInstance as any).setGlobalFilter as (filterValue: string) => void;
  const globalFilter = (state as any).globalFilter as string;

  return (
    <div className={styles['reports-container']}>
      <StatusSummaryCards orders={orders} />
      <div className={styles['orders-header']}>
        <h2 className={styles['orders-title']}>Orders</h2>
        
        <GlobalFilter globalFilter={globalFilter} setGlobalFilter={setGlobalFilter} />
        <button
          className={styles['refresh-button']}
          onClick={fetchOrders}
          disabled={isLoading}
        >
          {isLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>
      {isLoading && <p className={styles['loading-message']}>Loading...</p>}
      {error && <p className={styles['error-message']}>{error}</p>}
      {!isLoading && !error && (
        <div className={styles['orders-table-wrapper']}>
          <OrdersTable
            headerGroups={headerGroups as HeaderGroup<ScreenOrder>[]}
            rows={rows as Row<ScreenOrder>[]}
            prepareRow={prepareRow}
            getTableProps={getTableProps}
            getTableBodyProps={getTableBodyProps}
          />
        </div>
      )}
    </div>
  );
};

export default ReportsPage;
