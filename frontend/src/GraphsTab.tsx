import DashboardCharts from "./components/dashboard/DashboardCharts";
import DashboardTimeSelector from "./components/dashboard/DashboardTimeSelector";
import { useState, useMemo } from "react";
import RefreshButton from "./components/RefreshButton";
import { usePeriodReport, useOrdersPeriod, usePurchases } from "./hooks/queries";
import { useDayChangeEffect } from "./hooks/useSimulation";

export default function GraphsTab() {
  const [days, setDays] = useState(30);

  const {
    data,
    isFetching: isLoading,
    refetch,
  } = usePeriodReport(days);

  const {
    data: ordersData,
    refetch: refetchOrders,
  } = useOrdersPeriod();

  const {
    data: purchasesData,
    refetch: refetchPurchases,
  } = usePurchases();

  // Refetch data when simulation day changes
  useDayChangeEffect(() => {
    console.log("GraphsTab: Day changed, refetching data");
    refetch();
    refetchOrders();
    refetchPurchases();
  });

  // Manual refresh handler
  const handleRefresh = () => {
    refetch();
    refetchOrders();
    refetchPurchases();
  };

  const handleDateRangeChange = (newDays: number) => {
    setDays(newDays);
  };

  const metrics = [
    { key: "screensProduced", label: "Screens Produced", color: "#007bff" },
    { key: "screensSold", label: "Screens Sold", color: "#28a745" },
    { key: "revenue", label: "Revenue", color: "#ffc107" },
    { key: "workingEquipment", label: "Working Equipment", color: "#00bcd4" },
    { key: "sandStock", label: "Sand Stock", color: "#ff7043" },
    { key: "copperStock", label: "Copper Stock", color: "#8e24aa" },
    { key: "sandPurchased", label: "Sand Purchased", color: "#00bcd4" },
    { key: "copperPurchased", label: "Copper Purchased", color: "#cddc39" },
    { key: "screenStock", label: "Screen Stock", color: "#1976d2" },
    { key: "screenPrice", label: "Screen Price", color: "#ff7043" },
  ];

  // Calculate daily totals from orders data
  const ordersDailyTotals = useMemo(() => {
    if (!ordersData || !Array.isArray(ordersData)) return {};
    
    const dailyTotals: Record<string, { screensSold: number; revenue: number }> = {};
    
    ordersData.forEach(order => {
      // Only count orders that have been paid (amountPaid exists and > 0)
      if (order.amountPaid && order.amountPaid > 0) {
        const orderDate = new Date(order.orderDate).toISOString().split('T')[0];
        
        if (!dailyTotals[orderDate]) {
          dailyTotals[orderDate] = { screensSold: 0, revenue: 0 };
        }
        
        dailyTotals[orderDate].screensSold += order.quantity;
        dailyTotals[orderDate].revenue += order.amountPaid;
      }
    });
    
    return dailyTotals;
  }, [ordersData]);

  // Calculate daily totals from purchases data
  const purchasesDailyTotals = useMemo(() => {
    if (!purchasesData || !Array.isArray(purchasesData)) return {};
    
    const dailyTotals: Record<string, { sandPurchased: number; copperPurchased: number }> = {};
    
    purchasesData.forEach((purchase) => {
      // Count all purchases with raw materials (regardless of delivery status)
      if (purchase.rawMaterial) {
        const orderDate = new Date(purchase.orderDate).toISOString().split('T')[0];
        
        if (!dailyTotals[orderDate]) {
          dailyTotals[orderDate] = { sandPurchased: 0, copperPurchased: 0 };
        }
        
        const materialName = purchase.rawMaterial.name?.toLowerCase();
        
        if (materialName === 'sand') {
          dailyTotals[orderDate].sandPurchased += purchase.quantity;
        } else if (materialName === 'copper') {
          dailyTotals[orderDate].copperPurchased += purchase.quantity;
        }
      }
    });
    
    return dailyTotals;
  }, [purchasesData]);

  // Merge report data with orders and purchases data
  const chartData = useMemo(() => {
    if (!Array.isArray(data)) return [];
    
    return [...data]
      .sort((a, b) => a.date.localeCompare(b.date))
      .map(report => {
        // Normalize report date to match purchase date format (YYYY-MM-DD)
        const reportDateNormalized = new Date(report.date).toISOString().split('T')[0];
        
        const orderTotals = ordersDailyTotals[reportDateNormalized];
        const purchaseTotals = purchasesDailyTotals[reportDateNormalized];
        
        return {
          ...report,
          // Override revenue and screensSold with orders data if available
          revenue: orderTotals?.revenue ?? report.revenue,
          screensSold: orderTotals?.screensSold ?? report.screensSold,
          // Override sand and copper purchased with purchases data if available
          sandPurchased: purchaseTotals?.sandPurchased ?? report.sandPurchased,
          copperPurchased: purchaseTotals?.copperPurchased ?? report.copperPurchased,
        };
      });
  }, [data, ordersDailyTotals, purchasesDailyTotals]);

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-start', marginBottom: 16 }}>
        <RefreshButton onClick={handleRefresh} disabled={isLoading} />
      </div>
      <h2 style={{ fontSize: 20, fontWeight: 600, margin: '0 0 16px 0' }}>Performance over time</h2>
      <DashboardTimeSelector value={days} onChange={handleDateRangeChange} />
      <DashboardCharts chartData={chartData} metrics={metrics} />
    </div>
  );
}
