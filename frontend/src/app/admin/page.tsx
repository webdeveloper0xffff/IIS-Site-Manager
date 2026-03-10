'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { fetchAdminJobs, fetchAdminNodes, fetchAdminSummary, getStoredAdminToken, type AdminSummary, type ProvisionJob, type ServerNode } from '@/lib/admin-api';

export default function AdminDashboardPage() {
  const router = useRouter();
  const [summary, setSummary] = useState<AdminSummary | null>(null);
  const [nodes, setNodes] = useState<ServerNode[]>([]);
  const [jobs, setJobs] = useState<ProvisionJob[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = getStoredAdminToken();
    if (!token) {
      router.replace('/admin/login');
      return;
    }

    const load = async () => {
      try {
        const [summaryResponse, nodesResponse, jobsResponse] = await Promise.all([
          fetchAdminSummary(token),
          fetchAdminNodes(token),
          fetchAdminJobs(token),
        ]);
        setSummary(summaryResponse);
        setNodes(nodesResponse);
        setJobs(jobsResponse.slice(0, 8));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load admin data');
      }
    };

    void load();
  }, [router]);

  const cards = summary
    ? [
        { label: 'Online Nodes', value: summary.onlineNodeCount, tone: 'text-[var(--accent)]' },
        { label: 'Pending Customers', value: summary.pendingCustomerCount, tone: 'text-sky-300' },
        { label: 'Active Customers', value: summary.activeCustomerCount, tone: 'text-emerald-300' },
        { label: 'Pending Sites', value: summary.pendingSiteCount, tone: 'text-amber-300' },
        { label: 'Failed Jobs', value: summary.failedJobCount, tone: 'text-[var(--danger)]' },
      ]
    : [];

  return (
    <div className="space-y-6">
      {error ? <p className="rounded-2xl border border-[var(--line)] bg-[var(--panel)] p-4 text-sm text-[var(--danger)]">{error}</p> : null}

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        {cards.map((card) => (
          <div key={card.label} className="rounded-[24px] border border-[var(--line)] bg-[var(--panel)] p-5 backdrop-blur">
            <p className="text-sm text-[var(--muted)]">{card.label}</p>
            <p className={`mt-3 text-4xl font-semibold ${card.tone}`}>{card.value}</p>
          </div>
        ))}
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.25fr_0.95fr]">
        <div className="rounded-[28px] border border-[var(--line)] bg-[var(--panel)] p-6 backdrop-blur">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-mono text-xs uppercase tracking-[0.24em] text-[var(--accent)]">Nodes</p>
              <h2 className="mt-2 text-xl font-semibold">Fleet overview</h2>
            </div>
            <Link href="/admin/sites" className="text-sm text-[var(--muted)] hover:text-[var(--foreground)]">
              Provision sites
            </Link>
          </div>
          <div className="mt-5 space-y-3">
            {nodes.map((node) => (
              <div key={node.id} className="rounded-2xl border border-[var(--line)] bg-white/3 p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="font-medium">{node.nodeName}</p>
                    <p className="mt-1 font-mono text-xs text-[var(--muted)]">{node.publicHost}</p>
                  </div>
                  <span className={`rounded-full px-3 py-1 text-xs ${node.isOnline ? 'bg-emerald-400/15 text-emerald-300' : 'bg-rose-400/15 text-rose-300'}`}>
                    {node.isOnline ? 'online' : 'offline'}
                  </span>
                </div>
                <div className="mt-4 grid gap-3 sm:grid-cols-3">
                  <Stat label="CPU" value={`${node.cpuUsagePercent.toFixed(1)}%`} />
                  <Stat label="Memory" value={`${node.memoryUsagePercent.toFixed(1)}%`} />
                  <Stat label="Sites" value={`${node.reportedIisSiteCount}`} />
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="rounded-[28px] border border-[var(--line)] bg-[var(--panel)] p-6 backdrop-blur">
          <div className="flex items-center justify-between">
            <div>
              <p className="font-mono text-xs uppercase tracking-[0.24em] text-[var(--accent)]">Jobs</p>
              <h2 className="mt-2 text-xl font-semibold">Recent provisioning activity</h2>
            </div>
            <Link href="/admin/sites" className="text-sm text-[var(--muted)] hover:text-[var(--foreground)]">
              All jobs
            </Link>
          </div>
          <div className="mt-5 space-y-3">
            {jobs.map((job) => (
              <div key={job.id} className="rounded-2xl border border-[var(--line)] bg-white/3 p-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-medium">{job.type}</p>
                  <span className="rounded-full bg-white/6 px-3 py-1 font-mono text-xs uppercase tracking-[0.2em] text-[var(--muted)]">
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

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3">
      <p className="text-xs uppercase tracking-[0.2em] text-[var(--muted)]">{label}</p>
      <p className="mt-2 text-xl font-semibold">{value}</p>
    </div>
  );
}
