import React, { useEffect, useState } from "react";
import { apiClient } from "../src/apiClient";
import styles from "./components/reports/ReportsPage.module.scss";
import { useTable, useSortBy, useGlobalFilter } from "react-table";

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

function StatusSummaryCards({ orders }: { orders: any[] }) {
  const colorClasses = [
    "blue", "pink", "green", "orange", "purple", "lime", "indigo", "deep-orange", "teal"
  ];
  return (
    <div className={styles["status-summary-section"]}>
      <h2>Purchases in the Queue</h2>
      <div className={styles["status-cards"]}>
        {Object.entries(STATUS_LABELS).map(([status, label], idx) => {
          const count = orders.filter(o => getOrderStatus(o) === status).length;
          if (count === 0) return null;
          const colorClass = styles[colorClasses[idx % colorClasses.length]] || "";
          return (
            <div key={status} className={`${styles["status-card"]} ${colorClass}`}>
              <span className={styles["label"]}>{label}</span>
              <span className={styles["count"]}>{count}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

const PurchaseOrdersTab: React.FC = () => {
  const [orders, setOrders] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [globalFilter, setGlobalFilter] = useState("");

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
  }, []);

  // Table columns
  const columns = React.useMemo(() => [
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

  // react-table setup
  const {
    getTableProps,
    getTableBodyProps,
    headerGroups,
    rows,
    prepareRow,
    setGlobalFilter: setTableGlobalFilter,
    state: tableState
  } = useTable({ columns, data: orders }, useGlobalFilter, useSortBy);

  useEffect(() => {
    setTableGlobalFilter(globalFilter);
  }, [globalFilter, setTableGlobalFilter]);

  return (
    <div className={styles["reports-container"]}>
      <StatusSummaryCards orders={orders} />
      <div className={styles["orders-table-wrapper"]}>
        <section className={styles["orders-table-section"]}>
          <div className={styles["tableTitle"]}>Purchase Orders</div>
          <div className={styles["topControls"]}>
            <span className={styles["global-filter"]}>
              Search: <input value={globalFilter} onChange={e => setGlobalFilter(e.target.value)} placeholder="Type to filter purchases..." />
            </span>
            <button className={styles["refreshButton"]} onClick={fetchOrders} disabled={loading}>
              Refresh
            </button>
          </div>
          <table {...getTableProps()} className={styles["table"]}>
            <thead>
              {headerGroups.map((headerGroup: any) => (
                <tr {...headerGroup.getHeaderGroupProps()}>
                  {headerGroup.headers.map((column: any) => (
                    <th {...column.getHeaderProps(column.getSortByToggleProps())} className={styles["th"]}>
                      {column.render("Header")}
                      <span>
                        {column.isSorted ? (column.isSortedDesc ? " 🔽" : " 🔼") : ""}
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
                      <td {...cell.getCellProps()} className={styles["td"]}>
                        {cell.render("Cell")}
                      </td>
                    ))}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </section>
      </div>
    </div>
  );
};

export default PurchaseOrdersTab;
