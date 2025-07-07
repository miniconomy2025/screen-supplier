import { useEffect, useState } from "react";
import styles from "./styles/styles";
import {
  DollarSign,
  Package,
  TrendingUp,
  Activity,
  Filter,
  ShoppingCart,
  CheckCircle,
  BarChart3,
  FileText,
  XCircle,
  Clock,
} from "lucide-react";
import { apiService } from "./services/reporting";
import { OrderSummary } from "./services/types";

export default function ReportingDashboard() {
  const [activeMainTab, setActiveMainTab] = useState("dashboard");
  const [activeReportTab, setActiveReportTab] = useState("manufacturing");
  const [startDate, setStartDate] = useState("2024-01-01");
  const [endDate, setEndDate] = useState("2024-01-31");
  const [viewMode, setViewMode] = useState("table");
  const [isDashboardLoading, setIsDashboardLoading] = useState(false);
  const [dashboardData, setDashboardData] = useState<any>(null);
  const [ordersData, setOrdersData] = useState<any>(null);
  const [manufacturingData, setManufacturingData] = useState<any>(null);

  const [isLoading, setIsLoading] = useState(false);

  const handleDashboardDateRangeChange = async (days: number) => {
    console.log(days);
    setIsDashboardLoading(true);
    const dashboardData = await apiService.getLastPeriodReport(days);
    setDashboardData(dashboardData);
    setIsDashboardLoading(false);
  };

  const loadReports = async (ordersApiFn: Function) => {
    setIsLoading(true);
    setIsDashboardLoading(true);
    const [manufacturingData, ordersData, lastDaysPeriodsReport] =
      await Promise.all([
        apiService.getManufacturingReportData(startDate, endDate),
        ordersApiFn(startDate, endDate),
        apiService.getLastPeriodReport(3),
      ]);

    setManufacturingData(manufacturingData);
    setDashboardData(lastDaysPeriodsReport);
    setOrdersData(ordersData);
    setIsLoading(false);
    setIsDashboardLoading(false);
  };

  const fetchReports = async () => {
    await loadReports(apiService.getOrdersReport);
  };

  const handleFilterChange = async () => {
    await loadReports(apiService.getOrdersReport);
  };

  useEffect(() => {
    setIsDashboardLoading(true);
    fetchReports();
  }, []);

  const currentData =
    activeReportTab === "manufacturing" ? manufacturingData : ordersData;

  return (
    <div style={styles.container}>
      <div style={styles.sidebar}>
        <div style={styles.sidebarHeader}>
          <h1 style={styles.sidebarTitle}>Screens</h1>
        </div>
        <nav style={styles.sidebarNav}>
          <button
            style={{
              ...styles.navItem,
              ...(activeMainTab === "dashboard" ? styles.navItemActive : {}),
            }}
            onClick={() => setActiveMainTab("dashboard")}
          >
            <BarChart3 size={18} />
            Dashboard
          </button>
          <button
            style={{
              ...styles.navItem,
              ...(activeMainTab === "reports" ? styles.navItemActive : {}),
            }}
            onClick={() => setActiveMainTab("reports")}
          >
            <FileText size={18} />
            Reports
          </button>
        </nav>
      </div>

      <div style={styles.mainContent}>
        <div style={styles.maxWidth}>
          {activeMainTab === "dashboard" && (
            <>
              <div style={styles.header}>
                <h1 style={styles.title}>Dashboard</h1>
                <p style={styles.subtitle}>Operations overview</p>
                <div style={{ marginTop: "16px", marginBottom: "16px" }}>
                  <div style={styles.filterGrid}>
                    <div style={styles.filterGroup}>
                      <label
                        style={styles.label}
                        htmlFor="dashboard-date-range"
                      >
                        Time Period
                      </label>
                      <select
                        id="dashboard-date-range"
                        // value={dashboardDateRange}
                        onChange={(e) =>
                          handleDashboardDateRangeChange(Number(e.target.value))
                        }
                        style={styles.select}
                      >
                        <option value="3">Last 3 days</option>
                        <option value="7">Last 7 Days</option>
                        <option value="30">Last 30 Days</option>
                        <option value="90">Last 90 Days</option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>

              <div style={styles.cardsGrid}>
                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Screens Produced</h3>
                    <Package size={16} color="#007bff" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : dashboardData.screensProduced}
                  </div>
                  <p style={styles.cardDescription}>Daily production output</p>
                  <p style={{ ...styles.cardChange, color: "#007bff" }}>
                    {/* {dashboardData.workingMachines} machines working */}
                  </p>
                </div>

                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Screens Sold Today</h3>
                    <ShoppingCart size={16} color="#28a745" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : dashboardData.screensSold}
                  </div>
                  <p style={styles.cardDescription}>Units sold today</p>
                </div>

                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Daily Revenue</h3>
                    <DollarSign size={16} color="#28a745" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : `R${dashboardData.revenue}`}
                  </div>
                  <p style={styles.cardDescription}>
                    Revenue from screen sales
                  </p>
                  <p style={{ ...styles.cardChange, color: "#28a745" }}></p>
                </div>

                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Working Machines</h3>
                    <Activity size={16} color="#007bff" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : dashboardData.workingMachines}
                  </div>
                  <p style={styles.cardDescription}>
                    Active production machines
                  </p>
                </div>
              </div>

              <div style={styles.cardsGrid}>
                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Sand Stock</h3>
                    <Package size={16} color="#ff8c00" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : `${dashboardData.sandStock} kg`}
                  </div>
                  <p style={styles.cardDescription}>Current sand inventory</p>
                  <p
                    style={{
                      ...styles.cardChange,
                      color:
                        dashboardData?.sandStock > 1000 ? "#28a745" : "#dc3545",
                    }}
                  >
                    {dashboardData?.sandConsumed} kg consumed today
                  </p>
                </div>

                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Copper Stock</h3>
                    <Package size={16} color="#6f42c1" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : `${dashboardData.copperStock} kg`}
                  </div>
                  <p style={styles.cardDescription}>Current copper inventory</p>
                  <p
                    style={{
                      ...styles.cardChange,
                      color:
                        dashboardData?.copperStock > 300
                          ? "#28a745"
                          : "#dc3545",
                    }}
                  >
                    {dashboardData?.copperConsumed} kg consumed today
                  </p>
                </div>

                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Sand Purchased</h3>
                    <TrendingUp size={16} color="#28a745" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : `${dashboardData.sandPurchased} kg`}
                  </div>
                  <p style={styles.cardDescription}>Sand purchased today</p>
                  {/* <p style={{ ...styles.cardChange, color: "#666" }}>
                    Net:{" "}
                    {dashboardData.sandPurchased - dashboardData.sandConsumed}{" "}
                    kg
                  </p> */}
                </div>

                <div
                  style={{
                    ...styles.card,
                    ...(isDashboardLoading ? styles.cardLoading : {}),
                  }}
                >
                  <div style={styles.cardHeader}>
                    <h3 style={styles.cardTitle}>Copper Purchased</h3>
                    <TrendingUp size={16} color="#28a745" />
                  </div>
                  <div style={styles.cardValue}>
                    {isDashboardLoading || !dashboardData
                      ? "Loading..."
                      : `${dashboardData.copperPurchased} kg`}
                  </div>
                  <p style={styles.cardDescription}>Copper purchased today</p>
                </div>
              </div>
            </>
          )}

          {activeMainTab === "reports" && (
            <div style={styles.header}>
              <h1 style={styles.title}>Reports</h1>

              <div style={styles.tabs}>
                <button
                  style={{
                    ...styles.tab,
                    ...(activeReportTab === "manufacturing"
                      ? styles.activeTab
                      : {}),
                  }}
                  onClick={() => setActiveReportTab("manufacturing")}
                >
                  Manufacturing Reports
                </button>
                <button
                  style={{
                    ...styles.tab,
                    ...(activeReportTab === "orders" ? styles.activeTab : {}),
                  }}
                  onClick={() => setActiveReportTab("orders")}
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

              {}
              <div style={styles.filterSummary}>
                <strong>Current Filter:</strong>{" "}
                {activeReportTab === "manufacturing" ? (
                  <>
                    {currentData.dateRange} ({currentData.totalDays} days)
                  </>
                ) : (
                  <>
                    {currentData.startDate} to {currentData.endDate}
                  </>
                )}
              </div>
            </div>
          )}

          {}
          {activeReportTab === "manufacturing" &&
            activeMainTab === "reports" && (
              <>
                {}
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
                      Purchase orders and operational costs
                    </p>
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
                    <p style={styles.cardDescription}>
                      Average daily production
                    </p>
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
                    <p style={styles.cardDescription}>
                      Daily raw material usage
                    </p>
                  </div>
                </div>
              </>
            )}

          {activeReportTab === "orders" && ordersData && (
            <>
              {}
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
                </div>

                {ordersData.summaries.map((summary: OrderSummary) => {
                  const iconProps = {
                    Completed: { icon: CheckCircle, color: "#28a745" },
                    Pending: { icon: Clock, color: "#ff8c00" },
                    Cancelled: { icon: XCircle, color: "#dc3545" },
                  }[summary.status];

                  const Icon = iconProps?.icon;
                  const color = iconProps?.color;

                  return (
                    <div
                      key={summary.status}
                      style={{
                        ...styles.card,
                        ...(isLoading ? styles.cardLoading : {}),
                      }}
                    >
                      <div style={styles.cardHeader}>
                        <h3 style={styles.cardTitle}>
                          {summary.status} Orders
                        </h3>
                        {Icon && <Icon size={16} color={color} />}
                      </div>
                      <div style={styles.cardValue}>
                        {isLoading
                          ? "Loading..."
                          : summary.totalOrders.toLocaleString()}
                      </div>
                      <p style={styles.cardDescription}>
                        {summary.status} orders in selected period
                      </p>
                      {!isLoading && ordersData.totalOrders > 0 && (
                        <p style={{ ...styles.cardChange, color }}>
                          {Math.round(
                            (summary.totalOrders / ordersData.totalOrders) * 100
                          )}
                          % of total orders
                        </p>
                      )}
                    </div>
                  );
                })}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
