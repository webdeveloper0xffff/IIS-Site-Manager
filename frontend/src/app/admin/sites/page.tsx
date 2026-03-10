'use client';

import { FormEvent, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  createAdminSite,
  fetchAdminCustomers,
  fetchAdminJobs,
  fetchAdminSites,
  getStoredAdminToken,
  type AdminCreateSiteRequest,
  type AdminCustomer,
  type AdminSite,
  type ProvisionJob,
} from '@/lib/admin-api';

export default function AdminSitesPage() {
  const router = useRouter();
  const [customers, setCustomers] = useState<AdminCustomer[]>([]);
  const [sites, setSites] = useState<AdminSite[]>([]);
  const [jobs, setJobs] = useState<ProvisionJob[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<AdminCreateSiteRequest>({
    customerId: '',
    siteName: '',
    domain: '',
    physicalPath: '',
    appPoolName: 'DefaultAppPool',
    port: 80,
  });

  const load = async (token: string) => {
    const [customersResponse, sitesResponse, jobsResponse] = await Promise.all([
      fetchAdminCustomers(token),
      fetchAdminSites(token),
      fetchAdminJobs(token),
    ]);

    const activeCustomers = customersResponse.filter((customer) => customer.status === 'active');
    setCustomers(activeCustomers);
    setSites(sitesResponse);
    setJobs(jobsResponse);
    setForm((current) => ({
      ...current,
      customerId: current.customerId || activeCustomers[0]?.id || '',
    }));
  };

  useEffect(() => {
    const token = getStoredAdminToken();
    if (!token) {
      router.replace('/admin/login');
      return;
    }

    load(token).catch((err) => setError(err instanceof Error ? err.message : 'Failed to load sites'));
  }, [router]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const token = getStoredAdminToken();
    if (!token) {
      router.replace('/admin/login');
      return;
    }

    setSubmitting(true);
    setError(null);
    setStatus(null);

    try {
      const response = await createAdminSite(token, form);
      setStatus(response.message);
      setForm((current) => ({ ...current, siteName: '', domain: '', physicalPath: '', port: 80 }));
      await load(token);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create site');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <section className="rounded-[28px] border border-[var(--line)] bg-[var(--panel)] p-6 backdrop-blur">
        <p className="font-mono text-xs uppercase tracking-[0.24em] text-[var(--accent)]">Provisioning</p>
        <h2 className="mt-2 text-2xl font-semibold">Queue a new hosting site</h2>
        <p className="mt-2 text-sm text-[var(--muted)]">
          Admin requests create provisioning jobs. The assigned node agent polls and executes the IIS site creation locally.
        </p>

        <form className="mt-6 grid gap-4 md:grid-cols-2" onSubmit={handleSubmit}>
          <Field label="Customer">
            <select
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3"
              value={form.customerId}
              onChange={(event) => setForm((current) => ({ ...current, customerId: event.target.value }))}
            >
              {customers.map((customer) => (
                <option key={customer.id} value={customer.id}>
                  {customer.email}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Site Name">
            <input
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3"
              value={form.siteName}
              onChange={(event) => setForm((current) => ({ ...current, siteName: event.target.value }))}
            />
          </Field>
          <Field label="Domain">
            <input
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3"
              value={form.domain}
              onChange={(event) => setForm((current) => ({ ...current, domain: event.target.value }))}
            />
          </Field>
          <Field label="Physical Path">
            <input
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3"
              value={form.physicalPath}
              onChange={(event) => setForm((current) => ({ ...current, physicalPath: event.target.value }))}
            />
          </Field>
          <Field label="App Pool">
            <input
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3"
              value={form.appPoolName}
              onChange={(event) => setForm((current) => ({ ...current, appPoolName: event.target.value }))}
            />
          </Field>
          <Field label="Port">
            <input
              type="number"
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3"
              value={form.port}
              onChange={(event) => setForm((current) => ({ ...current, port: Number(event.target.value) || 80 }))}
            />
          </Field>

          <div className="md:col-span-2">
            {error ? <p className="mb-3 text-sm text-[var(--danger)]">{error}</p> : null}
            {status ? <p className="mb-3 text-sm text-[var(--accent)]">{status}</p> : null}
            <button
              type="submit"
              disabled={submitting || !form.customerId}
              className="rounded-full bg-[var(--accent)] px-5 py-3 font-semibold text-slate-950 transition hover:bg-[var(--accent-strong)] disabled:opacity-60"
            >
              {submitting ? 'Queueing job...' : 'Queue provisioning job'}
            </button>
          </div>
        </form>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <div className="rounded-[28px] border border-[var(--line)] bg-[var(--panel)] p-6 backdrop-blur">
          <h3 className="text-xl font-semibold">Sites</h3>
          <div className="mt-5 space-y-3">
            {sites.map((site) => (
              <div key={site.id} className="rounded-2xl border border-[var(--line)] bg-white/3 p-4">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-medium">{site.siteName}</p>
                    <p className="mt-1 text-sm text-[var(--muted)]">
                      {site.domain} • {site.customerEmail} • {site.nodeName}
                    </p>
                  </div>
                  <span className="rounded-full bg-white/6 px-3 py-1 text-xs uppercase tracking-[0.18em] text-[var(--muted)]">
                    {site.provisioningStatus}
                  </span>
                </div>
                <p className="mt-3 font-mono text-xs text-[var(--muted)]">{site.publish.webDeployEndpoint}</p>
                {site.lastProvisionError ? <p className="mt-3 text-sm text-[var(--danger)]">{site.lastProvisionError}</p> : null}
              </div>
            ))}
          </div>
        </div>

        <div className="rounded-[28px] border border-[var(--line)] bg-[var(--panel)] p-6 backdrop-blur">
          <h3 className="text-xl font-semibold">Jobs</h3>
          <div className="mt-5 space-y-3">
            {jobs.map((job) => (
              <div key={job.id} className="rounded-2xl border border-[var(--line)] bg-white/3 p-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-medium">{job.type}</p>
                  <span className="rounded-full bg-white/6 px-3 py-1 text-xs uppercase tracking-[0.18em] text-[var(--muted)]">
                    {job.status}
                  </span>
                </div>
                <p className="mt-2 font-mono text-xs text-[var(--muted)]">{job.id}</p>
                {job.error ? <p className="mt-3 text-sm text-[var(--danger)]">{job.error}</p> : null}
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="mb-2 block text-sm text-[var(--muted)]">{label}</span>
      {children}
    </label>
  );
}
