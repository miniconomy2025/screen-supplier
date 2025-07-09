import React from "react";
import styles from "../components/reports/ReportsPage.module.scss";

interface RefreshButtonProps {
  onClick: () => void;
  disabled?: boolean;
  children?: React.ReactNode;
}

const RefreshButton: React.FC<RefreshButtonProps> = ({ onClick, disabled, children }) => (
  <button
    className={styles["refresh-button"]}
    onClick={onClick}
    disabled={disabled}
    type="button"
  >
    {children || "Refresh"}
  </button>
);

export default RefreshButton;
