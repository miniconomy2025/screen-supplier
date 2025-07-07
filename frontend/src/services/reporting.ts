export type DataResponse<T> = {
  status: "success" | "error";
  data: T;
};

interface ApiResponse<T = any> {
  data?: T;
  message?: string;
  error?: string;
  success?: boolean;
  count?: number;
}

async function handleResponse<T>(response: Response): Promise<T> {
  const contentType = response.headers.get("content-type");
  let data: any;

  if (contentType && contentType.includes("application/json")) {
    try {
      data = await response.json();
    } catch {
      data = {};
    }
  } else {
    const text = await response.text();
    data = { message: text || "No response data" };
  }

  if (!response.ok) {
    const errorMessage =
      data.message || response.statusText || "Something went wrong";
    const error = new Error(errorMessage) as any;
    error.response = {
      status: response.status,
      data: data,
    };
    if (response.status === 401) {
      sessionStorage.removeItem("token");
      sessionStorage.removeItem("user");
      window.location.href = "/login";
    }
    throw error;
  }
  return data as T;
}

class ApiService {
  private baseURL: string;

  constructor() {
    this.baseURL = "/api";
  }

  private async fetchWrapper<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `http://localhost:5205${endpoint}`;

    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      ...(options.headers as Record<string, string>),
    };

    const response = await fetch(url, {
      ...options,
      headers,
    });
    return handleResponse<T>(response);
  }

  // Example: User teams API
  async getReportData(
    uuid: string
  ): Promise<ApiResponse<{ id: number; name: string; is_lead: boolean }[]>> {
    return this.fetchWrapper(`/users/${uuid}/teams`, { method: "GET" });
  }

  async getManufacturingReport(
 
  ): Promise<ApiResponse<any>> {
    // const query = new URLSearchParams({ startDate, endDate, period }).toString();
    return this.fetchWrapper(`/screens`, { method: "GET" });
  }

  async getOrdersReport(
    startDate: string,
    endDate: string,
    period: string
  ): Promise<ApiResponse<any>> {
    const query = new URLSearchParams({ startDate, endDate, period }).toString();
    return this.fetchWrapper(`/screens`, { method: "GET" });
  }
}

export const apiService = new ApiService();
