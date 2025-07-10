import { useEffect, useState, useMemo, useCallback } from "react";
import DashboardCards from "./components/dashboard/DashboardCards";
import SummaryCards from "./components/dashboard/SummaryCards";
import ProfitLossChart from "./components/dashboard/ProfitLossChart";
import { usePeriodReport, useOrdersPeriod, usePurchases } from "./hooks/queries";
import RefreshButton from "./components/RefreshButton";
import { useDayChangeEffect, useSimulationStatus } from "./hooks/useSimulation";

export default function Dashboard() {
  const [days] = useState(90);
  const [error, setError] = useState<string | null>(null);
  const simulationStatus = useSimulationStatus();

  const {
    data,
    isFetching: isLoading,
    refetch,
    error: queryError,
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
    console.log("Dashboard: Day changed, refetching data");
    refetch();
    refetchOrders();
    refetchPurchases();
  });

  useEffect(() => {
    if (queryError) setError('Error fetching dashboard data. Please try again.');
  }, [queryError]);

  // Manual refresh handler - memoized to prevent unnecessary re-renders
  const handleRefresh = useCallback(() => {
    refetch();
    refetchOrders();
    refetchPurchases();
  }, [refetch, refetchOrders, refetchPurchases]);

  // Calculate daily totals from orders data
  const ordersDailyTotals = useMemo(() => {
    if (!ordersData || !Array.isArray(ordersData)) return {};
    
    const dailyTotals: Record<string, { screensSold: number; revenue: number }> = {};
    const processedOrderIds = new Set<number>();
    
    ordersData.forEach(order => {
      // Skip duplicate orders (same ID)
      if (processedOrderIds.has(order.id)) {
        return;
      }
      processedOrderIds.add(order.id);
      
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

  // Calculate daily totals from purchases data using actual costs
  const purchasesDailyTotals = useMemo(() => {
    if (!purchasesData || !Array.isArray(purchasesData)) return {};
    
    const dailyTotals: Record<string, { sandPurchased: number; copperPurchased: number; totalCost: number }> = {};
    const processedPurchaseIds = new Set<number>();
    
    purchasesData.forEach((purchase) => {
      // Skip duplicate purchases (same ID)
      if (processedPurchaseIds.has(purchase.id)) {
        return;
      }
      processedPurchaseIds.add(purchase.id);
      
      // Count all purchases with raw materials (regardless of delivery status)
      if (purchase.rawMaterial) {
        const orderDate = new Date(purchase.orderDate).toISOString().split('T')[0];
        
        if (!dailyTotals[orderDate]) {
          dailyTotals[orderDate] = { sandPurchased: 0, copperPurchased: 0, totalCost: 0 };
        }
        
        const materialName = purchase.rawMaterial.name?.toLowerCase();
        const quantity = purchase.quantity || 0;
        
        // Calculate actual cost from purchase order data
        const unitPrice = purchase.unitPrice || 0;
        const shippingPrice = purchase.orderShippingPrice || 0;
        const totalOrderCost = (unitPrice * quantity) + shippingPrice;
        
        if (materialName === 'sand') {
          dailyTotals[orderDate].sandPurchased += quantity;
          dailyTotals[orderDate].totalCost += totalOrderCost;
        } else if (materialName === 'copper') {
          dailyTotals[orderDate].copperPurchased += quantity;
          dailyTotals[orderDate].totalCost += totalOrderCost;
        }
      }
    });
    
    return dailyTotals;
  }, [purchasesData]);

  // Calculate profit/loss data for chart
  const profitLossData = useMemo(() => {
    if (!Array.isArray(data)) return [];
    
    const result = [...data]
      .sort((a, b) => a.date.localeCompare(b.date))
      .map(report => {
        // Normalize report date to match purchase date format (YYYY-MM-DD)
        const reportDateNormalized = new Date(report.date).toISOString().split('T')[0];
        
        const orderTotals = ordersDailyTotals[reportDateNormalized];
        const purchaseTotals = purchasesDailyTotals[reportDateNormalized];
        
        const revenue = orderTotals?.revenue ?? 0;
        const costs = purchaseTotals?.totalCost ?? 0;
        const profit = revenue - costs;
        
        return {
          date: report.date,
          revenue,
          costs,
          profit,
        };
      });

    // Debug logging for a few sample days
    console.log('Sample profit/loss data:', result.slice(0, 5).map(d => ({
      date: d.date,
      revenue: d.revenue,
      costs: d.costs,
      profit: d.profit
    })));

    return result;
  }, [data, ordersDailyTotals, purchasesDailyTotals]);

  // Calculate summary totals
  const summaryTotals = useMemo(() => {
    const totalRevenue = profitLossData.reduce((sum, day) => sum + day.revenue, 0);
    const totalCosts = profitLossData.reduce((sum, day) => sum + day.costs, 0);
    const totalProfit = totalRevenue - totalCosts;
    
    return { totalRevenue, totalCosts, totalProfit };
  }, [profitLossData]);

  const latest = Array.isArray(data) && data.length > 0 ? data[data.length - 1] : null;

  return (
    <div>
      {error && (
        <div style={{ background: '#ffe0e0', color: '#a00', padding: '10px', marginBottom: '16px', borderRadius: '4px', textAlign: 'center' }}>
          {error}
        </div>
      )}
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '40px 0' }}>
          <div className="loader" style={{ margin: '0 auto', width: '40px', height: '40px', border: '4px solid #ccc', borderTop: '4px solid #333', borderRadius: '50%', animation: 'spin 1s linear infinite' }} />
          <style>{`@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }`}</style>
        </div>
      ) : (
        <>
          {/* Summary Cards */}
          <SummaryCards
            totalRevenue={summaryTotals.totalRevenue}
            totalCosts={summaryTotals.totalCosts}
            totalProfit={summaryTotals.totalProfit}
            isLoading={isLoading}
          />
          
          {/* Profit/Loss Chart */}
          <ProfitLossChart data={profitLossData} />
          
          {/* Current Day Cards */}
          <DashboardCards
            data={latest}
            isLoading={isLoading}
            currentDay={simulationStatus?.currentDay}
            onRefresh={handleRefresh}
            RefreshButtonComponent={RefreshButton}
          />
        </>
      )}
    </div>
  );
}
