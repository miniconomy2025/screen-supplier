import { ManufacturingSummary, ScreenOrdersReport } from "./types";

class ApiService {
  private baseURL: string;

  constructor() {
    this.baseURL = "/http://localhost:5205";
  }
  async getLastPeriodReport(pastDaysToInclude: number): Promise<any> {
    return {
      date: "2025-07-06T15:13:29.244Z",
      sandStock: 0,
      copperStock: 0,
      sandPurchased: 0,
      copperPurchased: 0,
      sandConsumed: 0,
      copperConsumed: 0,
      screensProduced: 0,
      workingMachines: 0,
      screensSold: 0,
      revenue: 0,
    };
  }



  async getOrdersReport(
    startDate: string,
    endDate: string
  ): Promise<ScreenOrdersReport> {
    const query = new URLSearchParams({
      startDate,
      endDate,
    }).toString();
    return {
      startDate: "2025-07-01",
      endDate: "2025-07-31",
      summaries: [
        {
          status: "Completed",
          totalOrders: 15,
        },
        {
          status: "Pending",
          totalOrders: 7,
        },
        {
          status: "Cancelled",
          totalOrders: 3,
        },
      ],
      totalOrders: 25,
    };
  }

  async getManufacturingReportData(
    startDate: string,
    endDate: string
  ): Promise<ManufacturingSummary> {
    const query = new URLSearchParams({
      startDate,
      endDate,
    }).toString();
    return {
      totalExpenses: 3766559,
      totalIncome: 7812000,
      screensSoldPerDay: 50,
      sandConsumption: 36,
      dateRange: `${startDate} to ${endDate}`,
      totalDays: 31,
    };
  }
}

export const apiService = new ApiService();
