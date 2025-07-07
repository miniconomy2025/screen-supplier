"use client";

import { useEffect, useState } from "react";
import {
  DollarSign,
  Package,
  TrendingUp,
  Activity,
  Filter,
  ShoppingCart,
  CheckCircle,
  XCircle,
  Clock,
} from "lucide-react";
import { apiService } from "@/services/reporting";

// Function to calculate days between two dates
const getDaysBetween = (startDate: string, endDate: string) => {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const diffTime = Math.abs(end.getTime() - start.getTime());
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24)) + 1;
};

// Generate detailed breakdown data for manufacturing table
const generateManufacturingData = (
  startDate: string,
  endDate: string,
  period: string
) => {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const data = [];

  if (period === "daily") {
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      const dateStr = d.toISOString().split("T")[0];
      const screens = 40 + Math.floor(Math.random() * 25);
      const revenue = screens * (5100 + Math.random() * 300);
      const expenses = screens * (2550 + Math.random() * 300);
      const sandUsed = screens * (24 + Math.random() * 4);
      const copperUsed = screens * (8 + Math.random() * 2);

      data.push({
        period: dateStr,
        screens_sold: screens,
        revenue: Math.round(revenue),
        expenses: Math.round(expenses),
        profit: Math.round(revenue - expenses),
        sand_kg: Math.round(sandUsed),
        copper_kg: Math.round(copperUsed),
        profit_margin: Math.round(((revenue - expenses) / revenue) * 100),
      });
    }
  } else if (period === "monthly") {
    const months = [
      "Jan",
      "Feb",
      "Mar",
      "Apr",
      "May",
      "Jun",
      "Jul",
      "Aug",
      "Sep",
      "Oct",
      "Nov",
      "Dec",
    ];
    const currentDate = new Date(start);

    while (currentDate <= end) {
      const year = currentDate.getFullYear();
      const month = currentDate.getMonth();
      const monthName = months[month];

      const monthStart = new Date(
        Math.max(currentDate.getTime(), start.getTime())
      );
      const monthEnd = new Date(year, month + 1, 0);
      if (monthEnd > end) monthEnd.setTime(end.getTime());

      const days = getDaysBetween(
        monthStart.toISOString().split("T")[0],
        monthEnd.toISOString().split("T")[0]
      );
      const screens = Math.round((45 + Math.random() * 15) * days);
      const revenue = screens * (5100 + Math.random() * 300);
      const expenses = screens * (2550 + Math.random() * 300);
      const sandUsed = screens * (24 + Math.random() * 4);
      const copperUsed = screens * (8 + Math.random() * 2);

      data.push({
        period: `${monthName} ${year}`,
        screens_sold: screens,
        revenue: Math.round(revenue),
        expenses: Math.round(expenses),
        profit: Math.round(revenue - expenses),
        sand_kg: Math.round(sandUsed),
        copper_kg: Math.round(copperUsed),
        profit_margin: Math.round(((revenue - expenses) / revenue) * 100),
      });

      currentDate.setMonth(currentDate.getMonth() + 1);
      currentDate.setDate(1);
    }
  }

  return data;
};

// Generate orders data
const generateOrdersData = (
  startDate: string,
  endDate: string,
  period: string
) => {
  const start = new Date(startDate);
  const end = new Date(endDate);
  const data = [];

  if (period === "daily") {
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      const dateStr = d.toISOString().split("T")[0];
      const totalOrders = 15 + Math.floor(Math.random() * 10); // 15-25 orders per day
      const completed = Math.floor(totalOrders * (0.7 + Math.random() * 0.2)); // 70-90% completed
      const pending = Math.floor(
        (totalOrders - completed) * (0.6 + Math.random() * 0.3)
      ); // 60-90% of remaining
      const cancelled = totalOrders - completed - pending;
      const totalValue = totalOrders * (15000 + Math.random() * 10000); // R15k-25k per order

      data.push({
        period: dateStr,
        total_orders: totalOrders,
        completed_orders: completed,
        pending_orders: pending,
        cancelled_orders: cancelled,
        total_value: Math.round(totalValue),
        avg_order_value: Math.round(totalValue / totalOrders),
        completion_rate: Math.round((completed / totalOrders) * 100),
      });
    }
  } else if (period === "monthly") {
    const months = [
      "Jan",
      "Feb",
      "Mar",
      "Apr",
      "May",
      "Jun",
      "Jul",
      "Aug",
      "Sep",
      "Oct",
      "Nov",
      "Dec",
    ];
    const currentDate = new Date(start);

    while (currentDate <= end) {
      const year = currentDate.getFullYear();
      const month = currentDate.getMonth();
      const monthName = months[month];

      const monthStart = new Date(
        Math.max(currentDate.getTime(), start.getTime())
      );
      const monthEnd = new Date(year, month + 1, 0);
      if (monthEnd > end) monthEnd.setTime(end.getTime());

      const days = getDaysBetween(
        monthStart.toISOString().split("T")[0],
        monthEnd.toISOString().split("T")[0]
      );
      const totalOrders = Math.round((18 + Math.random() * 8) * days);
      const completed = Math.floor(totalOrders * (0.75 + Math.random() * 0.15));
      const pending = Math.floor(
        (totalOrders - completed) * (0.65 + Math.random() * 0.25)
      );
      const cancelled = totalOrders - completed - pending;
      const totalValue = totalOrders * (18000 + Math.random() * 8000);

      data.push({
        period: `${monthName} ${year}`,
        total_orders: totalOrders,
        completed_orders: completed,
        pending_orders: pending,
        cancelled_orders: cancelled,
        total_value: Math.round(totalValue),
        avg_order_value: Math.round(totalValue / totalOrders),
        completion_rate: Math.round((completed / totalOrders) * 100),
      });

      currentDate.setMonth(currentDate.getMonth() + 1);
      currentDate.setDate(1);
    }
  }

  return data;
};

// Generate summary data for manufacturing cards
const generateManufacturingReportData = (
  startDate: string,
  endDate: string,
  period: string
) => {
  const days = getDaysBetween(startDate, endDate);
  const dailyScreens = 45 + Math.floor(Math.random() * 20);
  const dailyRevenue = dailyScreens * 5250;
  const dailyExpenses = dailyScreens * 2700;
  const dailySandConsumption = dailyScreens * 25;

  const totalRevenue = dailyRevenue * days;
  const totalExpenses = dailyExpenses * days;

  return {
    totalExpenses: Math.round(totalExpenses),
    totalIncome: Math.round(totalRevenue),
    screensSoldPerDay: dailyScreens,
    sandConsumption: Math.round(dailySandConsumption / days),
    dateRange: `${startDate} to ${endDate}`,
    totalDays: days,
    period: period,
  };
};

// Generate summary data for orders cards
const generateOrdersReportData = (
  startDate: string,
  endDate: string,
  period: string
) => {
  const days = getDaysBetween(startDate, endDate);
  const dailyOrders = 18 + Math.floor(Math.random() * 8);
  const totalOrders = dailyOrders * days;
  const completedOrders = Math.floor(
    totalOrders * (0.75 + Math.random() * 0.15)
  );
  const pendingOrders = Math.floor(
    (totalOrders - completedOrders) * (0.65 + Math.random() * 0.25)
  );
  const cancelledOrders = totalOrders - completedOrders - pendingOrders;
  const avgOrderValue = 18000 + Math.random() * 8000;

  return {
    totalOrders: totalOrders,
    completedOrders: completedOrders,
    pendingOrders: pendingOrders,
    cancelledOrders: cancelledOrders,
    avgOrderValue: Math.round(avgOrderValue),
    completionRate: Math.round((completedOrders / totalOrders) * 100),
    dateRange: `${startDate} to ${endDate}`,
    totalDays: days,
    period: period,
  };
};

export default function ReportingDashboard() {
  const [activeTab, setActiveTab] = useState("manufacturing");
  const [startDate, setStartDate] = useState("2024-01-01");
  const [endDate, setEndDate] = useState("2024-01-31");
  const [reportPeriod, setReportPeriod] = useState("monthly");
  const [viewMode, setViewMode] = useState("table");
  const [manufacturingData, setManufacturingData] = useState(
    generateManufacturingReportData("2024-01-01", "2024-01-31", "monthly")
  );
  const [ordersData, setOrdersData] = useState(
    generateOrdersReportData("2024-01-01", "2024-01-31", "monthly")
  );
  const [manufacturingDetailedData, setManufacturingDetailedData] = useState(
    generateManufacturingData("2024-01-01", "2024-01-31", "monthly")
  );
  const [ordersDetailedData, setOrdersDetailedData] = useState(
    generateOrdersData("2024-01-01", "2024-01-31", "monthly")
  );
  const [isLoading, setIsLoading] = useState(false);

  const fetchReports = async () => {
    setIsLoading(true);
    try {
      const [manufacturingRes, ordersRes] = await Promise.all([
        apiService.getManufacturingReport(),
        apiService.getOrdersReport(startDate, endDate, reportPeriod),
      ]);
      console.log(manufacturingRes);
      if (manufacturingRes.success && ordersRes.success) {
        setManufacturingData(manufacturingRes.data);
        setOrdersData(ordersRes.data);
      } else {
        console.error("Failed to fetch reports");
      }
    } catch (error) {
      console.error("API error:", error);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchReports();
  }, []);

  const handleFilterChange = () => {
    setIsLoading(true);

    setTimeout(() => {
      const newManufacturingData = generateManufacturingReportData(
        startDate,
        endDate,
        reportPeriod
      );
      const newOrdersData = generateOrdersReportData(
        startDate,
        endDate,
        reportPeriod
      );
      const newManufacturingDetailedData = generateManufacturingData(
        startDate,
        endDate,
        reportPeriod
      );
      const newOrdersDetailedData = generateOrdersData(
        startDate,
        endDate,
        reportPeriod
      );

      setManufacturingData(newManufacturingData);
      setOrdersData(newOrdersData);
      setManufacturingDetailedData(newManufacturingDetailedData);
      setOrdersDetailedData(newOrdersDetailedData);
      setIsLoading(false);
    }, 500);
  };

  const getPercentageChange = () => {
    return -5 + Math.random() * 20; // Random percentage change
  };

  const styles = {
    container: {
      minHeight: "100vh",
      backgroundColor: "#f5f5f5",
      padding: "24px",
      fontFamily: "Arial, sans-serif",
    },
    maxWidth: {
      maxWidth: "1400px",
      margin: "0 auto",
    },
    header: {
      backgroundColor: "white",
      borderRadius: "8px",
      boxShadow: "0 2px 4px rgba(0,0,0,0.1)",
      padding: "24px",
      marginBottom: "24px",
    },
    title: {
      fontSize: "32px",
      fontWeight: "bold",
      color: "#333",
      marginBottom: "24px",
      margin: 0,
    },
    tabs: {
      display: "flex",
      borderBottom: "2px solid #e0e0e0",
      marginBottom: "24px",
    },
    tab: {
      padding: "12px 24px",
      backgroundColor: "transparent",
      border: "none",
      fontSize: "16px",
      fontWeight: "500",
      cursor: "pointer",
      borderBottom: "2px solid transparent",
      color: "#666",
    },
    activeTab: {
      color: "#007bff",
      borderBottomColor: "#007bff",
    },
    topControls: {
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: "24px",
      flexWrap: "wrap" as const,
      gap: "16px",
    },
    viewModeGroup: {
      display: "flex",
      flexDirection: "column" as const,
    },
    filterGrid: {
      display: "grid",
      gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))",
      gap: "16px",
      alignItems: "end",
    },
    filterGroup: {
      display: "flex",
      flexDirection: "column" as const,
    },
    label: {
      fontSize: "14px",
      fontWeight: "500",
      color: "#555",
      marginBottom: "4px",
    },
    input: {
      padding: "8px 12px",
      border: "1px solid #ddd",
      borderRadius: "4px",
      fontSize: "14px",
    },
    select: {
      padding: "8px 12px",
      border: "1px solid #ddd",
      borderRadius: "4px",
      fontSize: "14px",
      backgroundColor: "white",
    },
    button: {
      padding: "8px 16px",
      backgroundColor: "#007bff",
      color: "white",
      border: "none",
      borderRadius: "4px",
      fontSize: "14px",
      cursor: "pointer",
      display: "flex",
      alignItems: "center",
      gap: "8px",
    },
    buttonDisabled: {
      backgroundColor: "#ccc",
      cursor: "not-allowed",
    },
    filterSummary: {
      marginTop: "16px",
      padding: "12px",
      backgroundColor: "#e3f2fd",
      borderRadius: "4px",
      fontSize: "14px",
      color: "#1565c0",
    },
    cardsGrid: {
      display: "grid",
      gridTemplateColumns: "repeat(auto-fit, minmax(250px, 1fr))",
      gap: "24px",
      marginBottom: "24px",
    },
    card: {
      backgroundColor: "white",
      borderRadius: "8px",
      boxShadow: "0 2px 4px rgba(0,0,0,0.1)",
      padding: "20px",
      opacity: 1,
    },
    cardLoading: {
      opacity: 0.5,
    },
    cardHeader: {
      display: "flex",
      justifyContent: "space-between",
      alignItems: "center",
      marginBottom: "12px",
    },
    cardTitle: {
      fontSize: "14px",
      fontWeight: "500",
      color: "#666",
      margin: 0,
    },
    cardValue: {
      fontSize: "28px",
      fontWeight: "bold",
      color: "#333",
      marginBottom: "4px",
    },
    cardDescription: {
      fontSize: "12px",
      color: "#888",
      marginBottom: "8px",
    },
    cardChange: {
      fontSize: "12px",
      marginTop: "4px",
    },
    tableContainer: {
      backgroundColor: "white",
      borderRadius: "8px",
      boxShadow: "0 2px 4px rgba(0,0,0,0.1)",
      padding: "24px",
      overflowX: "auto" as const,
    },
    tableTitle: {
      fontSize: "20px",
      fontWeight: "bold",
      color: "#333",
      marginBottom: "16px",
    },
    table: {
      width: "100%",
      borderCollapse: "collapse" as const,
      fontSize: "14px",
    },
    th: {
      backgroundColor: "#f8f9fa",
      padding: "12px",
      textAlign: "left" as const,
      fontWeight: "600",
      color: "#555",
      borderBottom: "2px solid #dee2e6",
    },
    td: {
      padding: "12px",
      borderBottom: "1px solid #dee2e6",
    },
    trHover: {
      backgroundColor: "#f8f9fa",
    },
  };

  const currentData =
    activeTab === "manufacturing" ? manufacturingData : ordersData;
  const currentDetailedData =
    activeTab === "manufacturing"
      ? manufacturingDetailedData
      : ordersDetailedData;

  return (
    <div style={styles.container}>
      <div style={styles.maxWidth}>
        {/* Header */}
        <div style={styles.header}>
          <h1 style={styles.title}>Reports</h1>

          {/* Tabs */}
          <div style={styles.tabs}>
            <button
              style={{
                ...styles.tab,
                ...(activeTab === "manufacturing" ? styles.activeTab : {}),
              }}
              onClick={() => setActiveTab("manufacturing")}
            >
              Manufacturing Reports
            </button>
            <button
              style={{
                ...styles.tab,
                ...(activeTab === "orders" ? styles.activeTab : {}),
              }}
              onClick={() => setActiveTab("orders")}
            >
              Orders Reports
            </button>
          </div>

          <div style={styles.topControls}>
            <div></div>
            <div style={styles.viewModeGroup}>
              <label style={styles.label} htmlFor="view-mode">
                View Mode
              </label>
              <select
                id="view-mode"
                value={viewMode}
                onChange={(e) => setViewMode(e.target.value)}
                style={styles.select}
              >
                <option value="table">Table View</option>
                <option value="charts">Charts View (Coming Soon)</option>
              </select>
            </div>
          </div>

          {/* Filters */}
          <div style={styles.filterGrid}>
            <div style={styles.filterGroup}>
              <label style={styles.label} htmlFor="start-date">
                Start Date
              </label>
              <input
                id="start-date"
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                disabled={isLoading}
                style={styles.input}
              />
            </div>
            <div style={styles.filterGroup}>
              <label style={styles.label} htmlFor="end-date">
                End Date
              </label>
              <input
                id="end-date"
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                disabled={isLoading}
                style={styles.input}
              />
            </div>
            <div style={styles.filterGroup}>
              <label style={styles.label} htmlFor="period">
                Report Period
              </label>
              <select
                value={reportPeriod}
                onChange={(e) => setReportPeriod(e.target.value)}
                disabled={isLoading}
                style={styles.select}
              >
                <option value="daily">Daily</option>
                <option value="weekly">Weekly</option>
                <option value="monthly">Monthly</option>
                <option value="quarterly">Quarterly</option>
              </select>
            </div>
            <button
              onClick={handleFilterChange}
              disabled={isLoading}
              style={{
                ...styles.button,
                ...(isLoading ? styles.buttonDisabled : {}),
              }}
            >
              <Filter size={16} />
              {isLoading ? "Loading..." : "Apply Filters"}
            </button>
          </div>

          {/* Filter Summary */}
          <div style={styles.filterSummary}>
            <strong>Current Filter:</strong> {currentData.dateRange} (
            {currentData.totalDays} days) -{" "}
            {currentData.period.charAt(0).toUpperCase() +
              currentData.period.slice(1)}{" "}
            view
          </div>
        </div>

        {/* Manufacturing Tab Content */}
        {activeTab === "manufacturing" && (
          <>
            {/* Manufacturing KPI Cards */}
            <div style={styles.cardsGrid}>
              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Total Expenses</h3>
                  <TrendingUp size={16} color="#dc3545" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : `R${manufacturingData.totalExpenses.toLocaleString()}`}
                </div>
                <p style={styles.cardDescription}>
                  Purchase orders + operational costs
                </p>
                {!isLoading && (
                  <p
                    style={{
                      ...styles.cardChange,
                      color: getPercentageChange() >= 0 ? "#dc3545" : "#28a745",
                    }}
                  >
                    {getPercentageChange() >= 0 ? "+" : ""}
                    {getPercentageChange().toFixed(1)}% vs previous period
                  </p>
                )}
              </div>

              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Total Income</h3>
                  <DollarSign size={16} color="#28a745" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : `R${manufacturingData.totalIncome.toLocaleString()}`}
                </div>
                <p style={styles.cardDescription}>Screen orders revenue</p>
                {!isLoading && (
                  <p
                    style={{
                      ...styles.cardChange,
                      color: getPercentageChange() >= 0 ? "#28a745" : "#dc3545",
                    }}
                  >
                    {getPercentageChange() >= 0 ? "+" : ""}
                    {getPercentageChange().toFixed(1)}% vs previous period
                  </p>
                )}
              </div>

              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Screens sold/day</h3>
                  <Activity size={16} color="#007bff" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : manufacturingData.screensSoldPerDay}
                </div>
                <p style={styles.cardDescription}>Average daily production</p>
                {!isLoading && (
                  <p
                    style={{
                      ...styles.cardChange,
                      color: getPercentageChange() >= 0 ? "#28a745" : "#dc3545",
                    }}
                  >
                    {getPercentageChange() >= 0 ? "+" : ""}
                    {getPercentageChange().toFixed(1)}% vs previous period
                  </p>
                )}
              </div>

              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Sand consumption</h3>
                  <Package size={16} color="#ff8c00" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : `${manufacturingData.sandConsumption} kg`}
                </div>
                <p style={styles.cardDescription}>Daily raw material usage</p>
                {!isLoading && (
                  <p
                    style={{
                      ...styles.cardChange,
                      color: getPercentageChange() >= 0 ? "#dc3545" : "#28a745",
                    }}
                  >
                    {getPercentageChange() >= 0 ? "+" : ""}
                    {getPercentageChange().toFixed(1)}% vs previous period
                  </p>
                )}
              </div>
            </div>
          </>
        )}

        {activeTab === "orders" && (
          <>
            {/* Orders KPI Cards */}
            <div style={styles.cardsGrid}>
              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Total Orders</h3>
                  <ShoppingCart size={16} color="#007bff" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : ordersData.totalOrders.toLocaleString()}
                </div>
                <p style={styles.cardDescription}>
                  All orders in selected period
                </p>
                {!isLoading && (
                  <p
                    style={{
                      ...styles.cardChange,
                      color: getPercentageChange() >= 0 ? "#28a745" : "#dc3545",
                    }}
                  >
                    {getPercentageChange() >= 0 ? "+" : ""}
                    {getPercentageChange().toFixed(1)}% vs previous period
                  </p>
                )}
              </div>

              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Completed Orders</h3>
                  <CheckCircle size={16} color="#28a745" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : ordersData.completedOrders.toLocaleString()}
                </div>
                <p style={styles.cardDescription}>
                  Successfully completed orders
                </p>
                {!isLoading && (
                  <p style={{ ...styles.cardChange, color: "#28a745" }}>
                    {ordersData.completionRate}% completion rate
                  </p>
                )}
              </div>

              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Pending Orders</h3>
                  <Clock size={16} color="#ff8c00" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : ordersData.pendingOrders.toLocaleString()}
                </div>
                <p style={styles.cardDescription}>Orders awaiting processing</p>
                {!isLoading && (
                  <p style={{ ...styles.cardChange, color: "#ff8c00" }}>
                    {Math.round(
                      (ordersData.pendingOrders / ordersData.totalOrders) * 100
                    )}
                    % of total orders
                  </p>
                )}
              </div>

              <div
                style={{
                  ...styles.card,
                  ...(isLoading ? styles.cardLoading : {}),
                }}
              >
                <div style={styles.cardHeader}>
                  <h3 style={styles.cardTitle}>Cancelled Orders</h3>
                  <XCircle size={16} color="#dc3545" />
                </div>
                <div style={styles.cardValue}>
                  {isLoading
                    ? "Loading..."
                    : ordersData.cancelledOrders.toLocaleString()}
                </div>
                <p style={styles.cardDescription}>Orders that were cancelled</p>
                {!isLoading && (
                  <p style={{ ...styles.cardChange, color: "#dc3545" }}>
                    {Math.round(
                      (ordersData.cancelledOrders / ordersData.totalOrders) *
                        100
                    )}
                    % cancellation rate
                  </p>
                )}
              </div>
            </div>
          </>
        )}

        {viewMode === "charts" && (
          <div style={styles.tableContainer}>
            <h2 style={styles.tableTitle}>Charts View</h2>
            <div
              style={{ textAlign: "center", padding: "60px", color: "#666" }}
            >
              <p style={{ fontSize: "18px", marginBottom: "8px" }}>
                Charts view coming soon!
              </p>
              <p>Switch to Table View to see detailed breakdown data.</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
