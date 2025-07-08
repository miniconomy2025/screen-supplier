import React, { useEffect, useState } from "react";
import { apiClient } from "../../apiClient";
import styles from "../reports/ReportsPage.module.scss";

interface PurchaseOrder {
  id: number;
  orderID: number;
  shipmentID?: number;
  quantity: number;
  quantityDelivered: number;
  orderDate: string;
  unitPrice: number;
  bankAccountNumber: string;
  origin: string;
  orderShippingPrice: number;
  shipperBankAccout?: string;
  orderStatusId: number;
  orderStatus: { id: number; name: string };
  rawMaterialId?: number;
  rawMaterial?: { id: number; name: string };
  equipmentOrder?: boolean;
}

const columns = [
  { Header: "Order ID", accessor: "orderID" },
  { Header: "Order Date", accessor: (row: PurchaseOrder) => new Date(row.orderDate).toLocaleDateString(), id: "orderDate" },
  { Header: "Material", accessor: (row: PurchaseOrder) => row.rawMaterial?.name || "-", id: "material" },
  { Header: "Quantity", accessor: "quantity" },
  { Header: "Delivered", accessor: "quantityDelivered" },
  { Header: "Unit Price", accessor: (row: PurchaseOrder) => `Ð${row.unitPrice.toLocaleString()}`, id: "unitPrice" },
  { Header: "Shipping Price", accessor: (row: PurchaseOrder) => `Ð${row.orderShippingPrice.toLocaleString()}`, id: "orderShippingPrice" },
  { Header: "Origin", accessor: "origin" },
  { Header: "Seller Bank", accessor: "bankAccountNumber" },
  { Header: "Shipper Bank", accessor: (row: PurchaseOrder) => row.shipperBankAccout || "-", id: "shipperBankAccout" },
  { Header: "Status", accessor: (row: PurchaseOrder) => row.orderStatus?.name || "-", id: "status" },
  { Header: "Equipment Order", accessor: (row: PurchaseOrder) => row.equipmentOrder ? "Yes" : "No", id: "equipmentOrder" },
];

const PurchaseOrdersTab: React.FC = () => {
  const [orders, setOrders] = useState<PurchaseOrder[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchOrders = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await apiClient.getPurchaseOrders();
      setOrders(data);
    } catch (err: any) {
      setError(err.message || "Failed to fetch purchase orders");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchOrders();
  }, []);

  return (
    <div className={styles['reports-container']}>
      <div className={styles['orders-header']}>
        <h2 className={styles['orders-title']}>Purchase Orders</h2>
        <button className={styles['refresh-button']} onClick={fetchOrders} disabled={isLoading}>
          {isLoading ? 'Refreshing...' : 'Refresh'}
        </button>
      </div>
      {isLoading && <p className={styles['loading-message']}>Loading...</p>}
      {error && <p className={styles['error-message']}>{error}</p>}
      <div className={styles['orders-table-wrapper']}>
        <div className={styles['orders-table-section']}>
          <table>
            <thead>
              <tr>
                {columns.map(col => (
                  <th key={col.id || col.accessor as string}>{col.Header}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {orders.map(order => (
                <tr key={order.id}>
                  {columns.map(col => (
                    <td key={col.id || col.accessor as string}>
                      {typeof col.accessor === 'string' ? (order as any)[col.accessor] : col.accessor(order)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
          {orders.length === 0 && !isLoading && !error && (
            <p className={styles['no-orders']}>No purchase orders found.</p>
          )}
        </div>
      </div>
    </div>
  );
};

export default PurchaseOrdersTab;
