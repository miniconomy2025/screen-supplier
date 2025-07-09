import React from "react";
import { usePurchases } from "./hooks/queries";
import { Column } from "react-table";
import styles from "./components/reports/ReportsPage.module.scss";
import { StatusSummaryCards, DataTable } from "./components/SharedTableComponents";
import { useDayChangeEffect } from "./hooks/useSimulation";

const STATUS_LABELS: Record<string, string> = {
  waiting_payment: "Waiting Payment",
  waiting_collection: "Waiting Collection",
  collected: "Collected",
  requires_payment_supplier: "Requires Payment Supplier",
  requires_delivery: "Requires Delivery",
  requires_payment_delivery: "Requires Payment Delivery",
  waiting_delivery: "Waiting Delivery",
  delivered: "Delivered",
  abandoned: "Abandoned",
};

function getOrderStatus(order: any): string {
  const raw = order.orderStatus && typeof order.orderStatus === "object"
    ? ("status" in order.orderStatus ? order.orderStatus.status : order.orderStatus.name)
    : order.orderStatus;
  return typeof raw === "string" ? raw.toLowerCase() : "";
}

function getStatusLabel(status: string): string {
  return STATUS_LABELS[status] || (status.charAt(0).toUpperCase() + status.slice(1));
}

const PurchaseOrdersTab: React.FC = () => {
  const {
    data: orders = [],
    isFetching: loading,
    refetch,
  } = usePurchases();

  // Refetch data when simulation day changes
  useDayChangeEffect(() => {
    console.log("PurchaseOrdersTab: Day changed, refetching data");
    refetch();
  });

  // Manual refresh handler
  const handleRefresh = () => {
    refetch();
  };

  // Table columns
  const columns: Column<any>[] = React.useMemo(() => [
    { Header: "Order ID", accessor: "orderID" },
    { Header: "Shipment ID", accessor: "shipmentID" },
    { Header: "Quantity", accessor: "quantity" },
    { Header: "Delivered", accessor: "quantityDelivered" },
    { Header: "Order Date", accessor: (row: any) => {
        if (!row.orderDate) return "-";
        const date = new Date(row.orderDate);
        return `${date.getMonth() + 1}/${date.getDate()}/${date.getFullYear()}`;
      }
    },
    { Header: "Unit Price", accessor: (row: any) => `Ð${row.unitPrice?.toLocaleString?.() ?? row.unitPrice}` },
    { Header: "Origin", accessor: "origin" },
    { Header: "Order Shipping Price", accessor: (row: any) => `Ð${row.orderShippingPrice?.toLocaleString?.() ?? row.orderShippingPrice}` },
    { Header: "Status", accessor: (row: any) => getStatusLabel(getOrderStatus(row)) },
    { Header: "Raw Material", accessor: (row: any) => row.rawMaterial ? row.rawMaterial.name : (row.equipmentOrder ? "Equipment" : "-") },
  ], []);

  return (
    <div className={styles["reports-container"]}>
      <StatusSummaryCards 
        title="Purchases in the Queue"
        orders={orders}
        statusLabels={STATUS_LABELS}
        getOrderStatus={getOrderStatus}
      />
      <DataTable
        data={orders}
        columns={columns}
        isLoading={loading}
        onRefresh={handleRefresh}
        title="Purchase Orders"
        noDataMessage="No purchases found."
        searchPlaceholder="Type to filter purchases..."
        useOrdersHeader={false}
      />
    </div>
  );
};

export default PurchaseOrdersTab;
