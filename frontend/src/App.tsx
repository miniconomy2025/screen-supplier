import React, { useState, useRef } from "react";
import styles from "./styles/styles";
import Dashboard from "./Dashboard";
import GraphsTab from "./GraphsTab";
import SimulationStatus from "./SimulationStatus";
import appStyles from "./styles/App.module.scss";
import { FiMenu } from "react-icons/fi";
import PurchaseOrdersTab from "./PurchaseOrdersTab";
import OrdersTab from "./OrdersTab";

export default function ReportingDashboard() {
  // Persist tab in localStorage
  const [activeMainTab, setActiveMainTab] = useState(() => {
    return localStorage.getItem("activeMainTab") || "dashboard";
  });

  const [sidebarOpen, setSidebarOpen] = useState(true);
  const toggleSidebar = () => setSidebarOpen((open) => !open);

  // For mobile: sidebar overlay logic
  const [isMobile, setIsMobile] = useState(false);
  React.useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth <= 700);
    checkMobile();
    window.addEventListener("resize", checkMobile);
    return () => window.removeEventListener("resize", checkMobile);
  }, []);

  // Helper to handle tab change and close sidebar on mobile
  const handleTabClick = (tab: string) => {
    setActiveMainTab(tab);
    localStorage.setItem("activeMainTab", tab);
    if (isMobile) setSidebarOpen(false);
  };

  const [lastStatusRefresh, setLastStatusRefresh] = useState<number>(Date.now());
  const [simulationStatus, setSimulationStatus] = useState<any>(null);

  // Handler for simulation status refresh
  const handleStatusRefresh = (status: any) => {
    setLastStatusRefresh(Date.now());
    setSimulationStatus(status);
  };

  return (
    <div style={styles.container}>
      {/* Mobile header with menu icon */}
      {isMobile && !sidebarOpen && (
        <header
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            width: "100vw",
            height: 56,
            background: "#fff",
            zIndex: 101,
            display: "flex",
            alignItems: "center",
            boxShadow: "0 1px 4px #0001",
            padding: "0 16px",
          }}
        >
          <button
            aria-label="Open sidebar"
            onClick={() => setSidebarOpen(true)}
            style={{
              background: "none",
              border: "none",
              fontSize: 28,
              cursor: "pointer",
              marginRight: 16,
              color: "#333",
              display: "flex",
              alignItems: "center",
            }}
          >
            <FiMenu />
          </button>
          <span style={{ fontWeight: 600, fontSize: 18, color: "#333" }}>
            Screens
          </span>
        </header>
      )}
      <div
        className={[
          appStyles.sidebar,
          sidebarOpen ? appStyles.sidebarExpanded : appStyles.sidebarCollapsed,
          isMobile && sidebarOpen ? appStyles.sidebarMobileFull + " open" : isMobile ? "" : "",
        ].join(" ")}
        style={isMobile && !sidebarOpen ? { pointerEvents: "none", left: "-100vw" } : {}}
      >
        <button
          aria-label={sidebarOpen ? "Collapse sidebar" : "Expand sidebar"}
          onClick={isMobile ? () => setSidebarOpen(false) : toggleSidebar}
          className={appStyles.sidebarToggle}
          style={
            isMobile
              ? { left: 16, right: "auto", top: 16, zIndex: 201 }
              : {}
          }
        >
          {sidebarOpen ? "<" : ">"}
        </button>
        <div style={{ display: sidebarOpen ? "block" : "none" }}>
          <div className={appStyles.sidebarHeader}>
            <h1 className={appStyles.sidebarTitle}>Screens</h1>
          </div>
          <nav className={appStyles.sidebarNav}>
            <button
              className={[
                appStyles.navItem,
                activeMainTab === "dashboard" ? appStyles.navItemActive : "",
              ].join(" ")}
              onClick={() => handleTabClick("dashboard")}
            >
              Dashboard
            </button>
            <button
              className={[
                appStyles.navItem,
                activeMainTab === "graphs" ? appStyles.navItemActive : "",
              ].join(" ")}
              onClick={() => handleTabClick("graphs")}
            >
              Graphs
            </button>
            <button
              className={[
                appStyles.navItem,
                activeMainTab === "orders" ? appStyles.navItemActive : "",
              ].join(" ")}
              onClick={() => handleTabClick("orders")}
            >
              Orders
            </button>
            <button
              className={[
                appStyles.navItem,
                activeMainTab === "purchases" ? appStyles.navItemActive : "",
              ].join(" ")}
              onClick={() => handleTabClick("purchases")}
            >
              Purchases
            </button>
          </nav>
          <SimulationStatus onStatusRefresh={handleStatusRefresh} />
        </div>
      </div>
      <div
        className={[
          appStyles.mainContent,
          !sidebarOpen ? appStyles.mainContentCollapsed : "",
        ].join(" ")}
        style={isMobile && !sidebarOpen ? { paddingTop: 56 } : {}}
      >
        <div style={styles.maxWidth}>
          {activeMainTab === "dashboard" && <Dashboard refreshKey={lastStatusRefresh} simulationStatus={simulationStatus} />}
          {activeMainTab === "graphs" && <GraphsTab refreshKey={lastStatusRefresh} />}
          {activeMainTab === "orders" && <OrdersTab refreshKey={lastStatusRefresh} />}
          {activeMainTab === "purchases" && <PurchaseOrdersTab refreshKey={lastStatusRefresh} />}
        </div>
      </div>
      {/* Mobile overlay: clicking outside sidebar closes it */}
      {isMobile && sidebarOpen && (
        <div
          onClick={() => setSidebarOpen(false)}
          style={{
            position: "fixed",
            top: 0,
            left: 0,
            width: "100vw",
            height: "100vh",
            background: "rgba(0,0,0,0.2)",
            zIndex: 199,
          }}
        />
      )}
    </div>
  );
}
