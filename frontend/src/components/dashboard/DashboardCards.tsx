import React from "react";
import { DollarSign, Package, TrendingUp, Activity, ShoppingCart } from "lucide-react";
import { PeriodReport } from "../../apiClient";
import DashboardStatCard from "./DashboardStatCard";

interface DashboardCardsProps {
  data: PeriodReport | null;
  isLoading: boolean;
  currentDay?: number;
}

const DashboardCards: React.FC<DashboardCardsProps> = ({ data, isLoading, currentDay }) => (
  <section style={{ marginBottom: 32 }}>
    <h2 style={{ fontSize: 20, fontWeight: 600, margin: '0 0 16px 0' }}>
      Day{typeof currentDay === 'number' ? `: ${currentDay}` : ''}
    </h2>
    <div
      style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
        gap: 24,
        width: '100%',
      }}
    >
      <DashboardStatCard
        title="Screens Produced"
        value={isLoading || !data ? "Loading..." : data.screensProduced}
        icon={<Package size={16} color="#007bff" />}
        description="Daily production output"
      />
      <DashboardStatCard
        title="Screens Sold Today"
        value={isLoading || !data ? "Loading..." : data.screensSold}
        icon={<ShoppingCart size={16} color="#28a745" />}
        description="Units sold today"
      />
      <DashboardStatCard
        title="Daily Revenue"
        value={isLoading || !data ? "Loading..." : `R${data.revenue}`}
        icon={<DollarSign size={16} color="#28a745" />}
        description="Revenue from screen sales"
      />
      <DashboardStatCard
        title="Working Machines"
        value={isLoading || !data ? "Loading..." : data.workingMachines}
        icon={<Activity size={16} color="#007bff" />}
        description="Active production machines"
      />
      <DashboardStatCard
        title="Sand Stock"
        value={isLoading || !data ? "Loading..." : `${data.sandStock} kg`}
        icon={<Package size={16} color="#ff8c00" />}
        description="Current sand inventory"
      />
      <DashboardStatCard
        title="Copper Stock"
        value={isLoading || !data ? "Loading..." : `${data.copperStock} kg`}
        icon={<Package size={16} color="#6f42c1" />}
        description="Current copper inventory"
      />
      <DashboardStatCard
        title="Sand Purchased"
        value={isLoading || !data ? "Loading..." : `${data.sandPurchased} kg`}
        icon={<TrendingUp size={16} color="#28a745" />}
        description="Sand purchased today"
      />
      <DashboardStatCard
        title="Copper Purchased"
        value={isLoading || !data ? "Loading..." : `${data.copperPurchased} kg`}
        icon={<TrendingUp size={16} color="#28a745" />}
        description="Copper purchased today"
      />
    </div>
  </section>
);

export default DashboardCards;
