'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { approveCustomer, fetchAdminCustomers, type AdminCustomer, getStoredAdminToken } from '@/lib/admin-api';

export default function AdminCustomersPage() {
  const router = useRouter();
  const [customers, setCustomers] = useState<AdminCustomer[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [pendingAction, setPendingAction] = useState<string | null>(null);

  const load = async (token: string) => {
    try {
      setCustomers(await fetchAdminCustomers(token));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load customers');
    }
  };

  useEffect(() => {
    const token = getStoredAdminToken();
    if (!token) {
      router.replace('/admin/login');
      return;
    }

    void load(token);
  }, [router]);

  const handleApprove = async (customerId: string) => {
    const token = getStoredAdminToken();
    if (!token) {
      router.replace('/admin/login');
      return;
    }

    setPendingAction(customerId);
    setError(null);
    try {
      await approveCustomer(token, customerId);
      await load(token);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Approval failed');
    } finally {
      setPendingAction(null);
    }
  };

  return (
    <div className="rounded-[28px] border border-[var(--line)] bg-[var(--panel)] p-6 backdrop-blur">
      <div className="mb-6">
        <p className="font-mono text-xs uppercase tracking-[0.24em] text-[var(--accent)]">Customers</p>
        <h2 className="mt-2 text-2xl font-semibold">Review registrations and activate hosting accounts</h2>
      </div>

      {error ? <p className="mb-4 rounded-2xl border border-[var(--line)] bg-white/4 p-4 text-sm text-[var(--danger)]">{error}</p> : null}

      <div className="overflow-hidden rounded-[24px] border border-[var(--line)]">
        <table className="w-full">
          <thead className="bg-white/5">
            <tr className="text-left text-xs uppercase tracking-[0.2em] text-[var(--muted)]">
              <th className="px-4 py-3">Email</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Assigned Node</th>
              <th className="px-4 py-3">Created</th>
              <th className="px-4 py-3 text-right">Action</th>
            </tr>
          </thead>
          <tbody>
            {customers.map((customer) => (
              <tr key={customer.id} className="border-t border-[var(--line)] align-top">
                <td className="px-4 py-4 font-medium">{customer.email}</td>
                <td className="px-4 py-4">
                  <span className="rounded-full bg-white/6 px-3 py-1 text-xs uppercase tracking-[0.18em] text-[var(--muted)]">
                    {customer.status}
                  </span>
                </td>
                <td className="px-4 py-4 text-sm text-[var(--muted)]">{customer.assignedNodeName ?? '-'}</td>
                <td className="px-4 py-4 text-sm text-[var(--muted)]">{new Date(customer.createdUtc).toLocaleString()}</td>
                <td className="px-4 py-4 text-right">
                  {customer.status === 'pending' ? (
                    <button
                      onClick={() => void handleApprove(customer.id)}
                      disabled={pendingAction === customer.id}
                      className="rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-[var(--accent-strong)] disabled:opacity-60"
                    >
                      {pendingAction === customer.id ? 'Approving...' : 'Approve'}
                    </button>
                  ) : (
                    <span className="text-sm text-[var(--muted)]">No action</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
