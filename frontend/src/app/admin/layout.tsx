import Link from 'next/link';

const nav = [
  { href: '/admin', label: 'Dashboard' },
  { href: '/admin/customers', label: 'Customers' },
  { href: '/admin/sites', label: 'Sites & Jobs' },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-transparent text-[var(--foreground)]">
      <div className="mx-auto flex min-h-screen max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8">
        <header className="mb-6 rounded-[28px] border border-[var(--line)] bg-[var(--panel)] px-6 py-5 backdrop-blur">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="font-mono text-xs uppercase tracking-[0.28em] text-[var(--accent)]">IIS Hosting Admin</p>
              <h1 className="mt-2 text-3xl font-semibold tracking-tight">Shared hosting operations console</h1>
              <p className="mt-2 max-w-2xl text-sm text-[var(--muted)]">
                Approve customers, allocate node capacity, trigger remote provisioning, and monitor delivery state.
              </p>
            </div>
            <nav className="flex flex-wrap gap-2">
              {nav.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="rounded-full border border-[var(--line)] bg-white/4 px-4 py-2 text-sm text-[var(--muted)] transition hover:border-[var(--accent)] hover:text-[var(--foreground)]"
                >
                  {item.label}
                </Link>
              ))}
              <Link
                href="/admin/login"
                className="rounded-full bg-[var(--accent)] px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-[var(--accent-strong)]"
              >
                Switch Admin
              </Link>
            </nav>
          </div>
        </header>
        <main className="flex-1">{children}</main>
      </div>
    </div>
  );
}
