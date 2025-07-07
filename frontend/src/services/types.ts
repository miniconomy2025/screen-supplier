export type OrderSummary = {
  status: "Completed" | "Pending" | "Cancelled";
  totalOrders: number;
};

export type ScreenOrdersReport = {
  startDate: string;
  endDate: string;
  summaries: OrderSummary[];
  totalOrders: number;
};

export type ManufacturingSummary = {
  totalExpenses: number;
  totalIncome: number;
  screensSoldPerDay: number;
  sandConsumption: number;
  dateRange: string;
  totalDays: number;
};


