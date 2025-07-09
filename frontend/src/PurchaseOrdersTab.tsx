import React, { useEffect, useState } from "react";
import { apiClient } from "../src/apiClient";
import { Column } from "react-table";
import styles from "./components/reports/ReportsPage.module.scss";
import { StatusSummaryCards, DataTable } from "./components/SharedTableComponents";

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

const PurchaseOrdersTab: React.FC<{ refreshKey?: number }> = ({ refreshKey }) => {
  const [orders, setOrders] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchOrders = async () => {
    setLoading(true);
    try {
      const data = await apiClient.getPurchases();
      setOrders(data);
    } catch (e) {
      // ...existing error handling...
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, [refreshKey]);

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
        onRefresh={fetchOrders}
        title="Purchase Orders"
        noDataMessage="No purchases found."
        searchPlaceholder="Type to filter purchases..."
        useOrdersHeader={false}
      />
    </div>
  );
};

export default PurchaseOrdersTab;
