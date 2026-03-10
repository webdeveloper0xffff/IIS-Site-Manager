'use client';

import { FormEvent, useState } from 'react';
import { useRouter } from 'next/navigation';
import { adminLogin, setStoredAdminToken } from '@/lib/admin-api';

export default function AdminLoginPage() {
  const router = useRouter();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoading(true);
    setError(null);

    try {
      const response = await adminLogin(username, password);
      if (!response.success || !response.token) {
        setError(response.message || 'Login failed');
        return;
      }

      setStoredAdminToken(response.token);
      router.push('/admin');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center px-4 py-10">
      <div className="w-full max-w-md rounded-[32px] border border-[var(--line)] bg-[var(--panel)] p-8 shadow-[0_24px_80px_rgba(0,0,0,0.35)] backdrop-blur">
        <p className="font-mono text-xs uppercase tracking-[0.28em] text-[var(--accent)]">Admin Access</p>
        <h1 className="mt-3 text-3xl font-semibold tracking-tight">Sign in to operate the fleet</h1>
        <p className="mt-3 text-sm text-[var(--muted)]">
          Use the administrator account configured on the control-plane. Credentials are no longer prefilled.
        </p>

        <form className="mt-8 space-y-4" onSubmit={handleSubmit}>
          <div>
            <label className="mb-2 block text-sm text-[var(--muted)]">Username</label>
            <input
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3 outline-none transition focus:border-[var(--accent)]"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
            />
          </div>

          <div>
            <label className="mb-2 block text-sm text-[var(--muted)]">Password</label>
            <input
              type="password"
              className="w-full rounded-2xl border border-[var(--line)] bg-[var(--panel-strong)] px-4 py-3 outline-none transition focus:border-[var(--accent)]"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          </div>

          {error ? <p className="text-sm text-[var(--danger)]">{error}</p> : null}

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded-2xl bg-[var(--accent)] px-4 py-3 font-semibold text-slate-950 transition hover:bg-[var(--accent-strong)] disabled:opacity-60"
          >
            {loading ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </div>
    </div>
  );
}
