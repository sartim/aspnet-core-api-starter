const apiUrl = process.env.API_URL ?? 'http://localhost:5070';
const email = process.env.ADMIN_EMAIL ?? 'admin@example.com';
const password = process.env.ADMIN_PASSWORD ?? 'change-this-password';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, init);
  if (!response.ok) throw new Error(`${response.status} ${await response.text()}`);
  return response.json() as Promise<T>;
}

const login = await request<{ token: string }>('/api/v1/auth/generate-jwt', {
  method: 'POST',
  headers: { 'content-type': 'application/json' },
  body: JSON.stringify({ email, password }),
});

const users = await request<{ items: unknown[]; totalCount: number }>(
  '/api/v1/users?page=1&pageSize=25',
  { headers: { authorization: `Bearer ${login.token}` } },
);

console.log(`Loaded ${users.items.length} of ${users.totalCount} users.`);
