import DashboardCharts from "./components/dashboard/DashboardCharts";
import DashboardTimeSelector from "./components/dashboard/DashboardTimeSelector";
import { useState } from "react";
import RefreshButton from "./components/RefreshButton";
import { usePeriodReport } from "./hooks/queries";
import { useDayChangeEffect } from "./hooks/useSimulation";

export default function GraphsTab() {
  const [days, setDays] = useState(30);

  const {
    data,
    isFetching: isLoading,
    refetch,
  } = usePeriodReport(days);

  // Refetch data when simulation day changes
  useDayChangeEffect(() => {
    console.log("GraphsTab: Day changed, refetching data");
    refetch();
  });

  // Manual refresh handler
  const handleRefresh = () => {
    refetch();
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

  const chartData = Array.isArray(data) ? [...data].sort((a, b) => a.date.localeCompare(b.date)) : [];

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
