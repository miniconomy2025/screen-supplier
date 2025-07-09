import React from "react";
import { ScreenOrder } from "./apiClient";
import { Column } from "react-table";
import styles from './components/reports/ReportsPage.module.scss';
import { StatusSummaryCards, DataTable } from "./components/SharedTableComponents";
import { useOrdersPeriod } from "./hooks/queries";
import { useDayChangeEffect } from "./hooks/useSimulation";

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

const OrdersTab: React.FC = () => {
  const {
    data: orders = [],
    isFetching: isLoading,
    refetch,
    error,
  } = useOrdersPeriod();

  // Refetch data when simulation day changes
  useDayChangeEffect(() => {
    console.log("OrdersTab: Day changed, refetching data");
    refetch();
  });

  // Manual refresh handler
  const handleRefresh = () => {
    refetch();
  };

  // Table columns
  const columns: Column<ScreenOrder>[] = React.useMemo(() => [
    { Header: 'Date', accessor: (row: ScreenOrder) => new Date(row.orderDate).toLocaleDateString(), id: 'orderDate' },
    { Header: 'Product', accessor: () => 'Screen', id: 'product' },
    { Header: 'Quantity', accessor: 'quantity' },
    { Header: 'Unit Price', accessor: (row: ScreenOrder) => `Ð${row.unitPrice.toLocaleString()}`, id: 'unitPrice' },
    { Header: 'Amount Paid', accessor: (row: ScreenOrder) => row.amountPaid != null ? `Ð${row.amountPaid.toLocaleString()}` : '-', id: 'amountPaid' },
    { Header: 'Status', accessor: (row: ScreenOrder) => getStatusLabel(getOrderStatus(row)), id: 'status' },
  ], []);

  return (
    <div className={styles['reports-container']}>
      <StatusSummaryCards 
        title="Orders in the Queue"
        orders={orders}
        statusLabels={STATUS_LABELS}
        getOrderStatus={getOrderStatus}
      />
      {isLoading && <p className={styles['loading-message']}>Loading...</p>}
      {error && <p className={styles['error-message']}>{(error as Error).message}</p>}
      {!isLoading && !error && (
        <DataTable
          data={orders}
          columns={columns}
          isLoading={isLoading}
          onRefresh={handleRefresh}
          title="Orders"
          noDataMessage="No orders found for the last 90 days."
          searchPlaceholder="Type to filter orders..."
          useOrdersHeader={true}
        />
      )}
    </div>
  );
};

export default OrdersTab;
