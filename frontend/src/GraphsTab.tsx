import DashboardCharts from "./components/dashboard/DashboardCharts";
import DashboardTimeSelector from "./components/dashboard/DashboardTimeSelector";
import { useEffect, useState } from "react";
import { apiClient, PeriodReport } from "./apiClient";

interface GraphsTabProps {
  refreshKey?: number;
}

export default function GraphsTab({ refreshKey }: GraphsTabProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [history, setHistory] = useState<PeriodReport[]>([]);
  const [days, setDays] = useState(30);
  const [screensHistory, setScreensHistory] = useState<{ quantity: number; price: number; date?: string }[]>([]);

  const metrics = [
    { key: "screensProduced", label: "Screens Produced", color: "#007bff" },
    { key: "screensSold", label: "Screens Sold", color: "#28a745" },
    { key: "revenue", label: "Revenue", color: "#ffc107" },
    { key: "workingMachines", label: "Working Machines", color: "#17a2b8" },
    { key: "sandStock", label: "Sand Stock", color: "#ff7043" },
    { key: "copperStock", label: "Copper Stock", color: "#8e24aa" },
    { key: "sandPurchased", label: "Sand Purchased", color: "#00bcd4" },
    { key: "copperPurchased", label: "Copper Purchased", color: "#cddc39" },
  ];

  const handleDateRangeChange = async (newDays: number) => {
    setDays(newDays);
    setIsLoading(true);
    try {
      const reports = await apiClient.getPeriodReport(newDays);
      setHistory(reports);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const loadData = async () => {
    setIsLoading(true);
    try {
      const reports = await apiClient.getPeriodReport(days);
      setHistory(reports);
      // Fetch screens history for extra graphs
      const screens = await apiClient.getScreensHistory();
      // Use the real date for charting
      const screensWithDate = screens.map((item) => ({
        ...item.screens,
        date: item.date,
      }));
      // Sort by date ascending
      screensWithDate.sort((a, b) => (a.date || '').localeCompare(b.date || ''));
      setScreensHistory(screensWithDate);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [refreshKey, days]);

  const chartData = [...history].sort((a, b) => a.date.localeCompare(b.date));

  return (
    <div>
      <h2 style={{ fontSize: 20, fontWeight: 600, margin: '0 0 16px 0' }}>Performance over time</h2>
      <DashboardTimeSelector value={days} onChange={handleDateRangeChange} />
      <DashboardCharts chartData={chartData} metrics={metrics} />
      {/* New graphs for screens quantity and price */}
      {screensHistory.length > 0 && (
        <div style={{ marginTop: 40 }}>
          <h2 style={{ fontSize: 18, fontWeight: 600, margin: '0 0 16px 0' }}>Screen Inventory & Price</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: 24 }}>
            <div style={{ background: '#fff', borderRadius: 8, padding: 16, boxShadow: '0 1px 4px #0001' }}>
              <h3 style={{ marginBottom: 8 }}>Screen Stock</h3>
              <DashboardCharts
                chartData={screensHistory}
                metrics={[{ key: "quantity", label: "", color: "#1976d2" }]}
              />
            </div>
            <div style={{ background: '#fff', borderRadius: 8, padding: 16, boxShadow: '0 1px 4px #0001' }}>
              <h3 style={{ marginBottom: 8 }}>Screen Price</h3>
              <DashboardCharts
                chartData={screensHistory}
                metrics={[{ key: "price", label: "", color: "#ff7043" }]}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
