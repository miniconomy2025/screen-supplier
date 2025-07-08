import React from "react";

interface DashboardTimeSelectorProps {
  value: number;
  onChange: (days: number) => void;
}

const DashboardTimeSelector: React.FC<DashboardTimeSelectorProps> = ({ value, onChange }) => (
  <div style={{ marginBottom: 24, display: 'flex', alignItems: 'center', gap: 16 }}>
    <label style={{ fontWeight: 500 }} htmlFor="dashboard-date-range">
      Time Period
    </label>
    <select
      id="dashboard-date-range"
      value={value}
      onChange={e => onChange(Number(e.target.value))}
      style={{ padding: 8, borderRadius: 4 }}
    >
      <option value="3">Last 3 days</option>
      <option value="7">Last 7 Days</option>
      <option value="30">Last 30 Days</option>
      <option value="90">Last 90 Days</option>
    </select>
  </div>
);

export default DashboardTimeSelector;
