import React from "react";
import { DollarSign, TrendingUp, TrendingDown } from "lucide-react";
import DashboardStatCard from "./DashboardStatCard";

interface SummaryCardsProps {
  totalRevenue: number;
  totalCosts: number;
  totalProfit: number;
  isLoading: boolean;
}

const SummaryCards: React.FC<SummaryCardsProps> = React.memo(({ totalRevenue, totalCosts, totalProfit, isLoading }) => {
  return (
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))',
        gap: 24,
        width: '100%',
        marginBottom: 32,
      }}
    >
      <DashboardStatCard
        title="Total Revenue"
        value={isLoading ? 0 : `Ð${totalRevenue.toLocaleString()}`}
        icon={<DollarSign size={16} color="#28a745" />}
        description="Total revenue from all sales"
      />
      <DashboardStatCard
        title="Total Costs"
        value={isLoading ? 0 : `Ð${totalCosts.toLocaleString()}`}
        icon={<TrendingDown size={16} color="#dc3545" />}
        description="Total costs for materials"
      />
      <DashboardStatCard
        title="Total Profit"
        value={isLoading ? 0 : `Ð${totalProfit.toLocaleString()}`}
        icon={<TrendingUp size={16} color={totalProfit >= 0 ? "#28a745" : "#dc3545"} />}
        description="Total profit/loss"
      />
    </div>
  );
});

export default SummaryCards;
