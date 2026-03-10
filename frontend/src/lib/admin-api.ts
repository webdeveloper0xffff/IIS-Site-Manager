export interface AdminLoginResponse {
  success: boolean;
  message: string;
  token?: string | null;
}

export interface AdminSummary {
  onlineNodeCount: number;
  pendingCustomerCount: number;
  activeCustomerCount: number;
  pendingSiteCount: number;
  failedJobCount: number;
}

export interface ServerNode {
  id: string;
  nodeName: string;
  publicHost: string;
  enabled: boolean;
  isOnline: boolean;
  reportedIisSiteCount: number;
  lastHeartbeatUtc: string;
  cpuUsagePercent: number;
  memoryUsagePercent: number;
  bytesTotalPerSec: number;
}

export interface AdminCustomer {
  id: string;
  email: string;
  status: string;
  createdUtc: string;
  approvedUtc?: string | null;
  assignedServerNodeId?: string | null;
  assignedNodeName?: string | null;
}

export interface PublishCredentials {
  ftpHost: string;
  ftpUser: string;
  ftpPassword: string;
  webDeployEndpoint: string;
  deployUser: string;
  deployPassword: string;
}

export interface AdminSite {
  id: string;
  customerId: string;
  customerEmail: string;
  serverNodeId: string;
  nodeName: string;
  siteName: string;
  domain: string;
  physicalPath: string;
  appPoolName: string;
  port: number;
  provisioningStatus: string;
  lastProvisionError?: string | null;
  publish: PublishCredentials;
  createdUtc: string;
}

export interface ProvisionJob {
  id: string;
  nodeId: string;
  customerId: string;
  hostedSiteId: string;
  type: string;
  status: string;
  payloadJson: string;
  error?: string | null;
  createdUtc: string;
  startedUtc?: string | null;
  completedUtc?: string | null;
  leaseUntilUtc?: string | null;
}

export interface AdminCreateSiteRequest {
  customerId: string;
  siteName: string;
  domain: string;
  physicalPath: string;
  appPoolName: string;
  port: number;
}

function getApiBase(): string {
  const envUrl = process.env.NEXT_PUBLIC_API_URL?.trim();
  if (envUrl) return envUrl;
  if (typeof window !== 'undefined') {
    if (window.location.port === '3000') return 'http://localhost:5032';
    return `${window.location.protocol}//${window.location.hostname}:5032`;
  }
  return 'http://localhost:5032';
}

const API_BASE = getApiBase();

async function apiFetch<T>(path: string, token: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed: ${response.status}`);
  }

  return response.json();
}

export async function adminLogin(username: string, password: string): Promise<AdminLoginResponse> {
  const response = await fetch(`${API_BASE}/api/admin/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });
  return response.json();
}

export function getStoredAdminToken(): string | null {
  if (typeof window === 'undefined') return null;
  return window.localStorage.getItem('admin_token');
}

export function setStoredAdminToken(token: string): void {
  if (typeof window === 'undefined') return;
  window.localStorage.setItem('admin_token', token);
}

export function clearStoredAdminToken(): void {
  if (typeof window === 'undefined') return;
  window.localStorage.removeItem('admin_token');
}

export function fetchAdminSummary(token: string): Promise<AdminSummary> {
  return apiFetch('/api/admin/summary', token);
}

export function fetchAdminNodes(token: string): Promise<ServerNode[]> {
  return apiFetch('/api/admin/nodes', token);
}

export function fetchAdminCustomers(token: string): Promise<AdminCustomer[]> {
  return apiFetch('/api/admin/customers', token);
}

export async function approveCustomer(token: string, customerId: string): Promise<{ success: boolean; message: string }> {
  return apiFetch(`/api/admin/customers/${customerId}/approve`, token, { method: 'POST' });
}

export function fetchAdminSites(token: string): Promise<AdminSite[]> {
  return apiFetch('/api/admin/sites', token);
}

export async function createAdminSite(token: string, payload: AdminCreateSiteRequest): Promise<{ success: boolean; message: string }> {
  return apiFetch('/api/admin/sites', token, {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function fetchAdminJobs(token: string): Promise<ProvisionJob[]> {
  return apiFetch('/api/admin/jobs', token);
}
