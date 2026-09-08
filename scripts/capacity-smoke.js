import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = (__ENV.BASE_URL || '').replace(/\/$/, '');
const configurations = {
  baseline: { vus: 5, duration: '30s', p95: 500, errorRate: 0.01 },
  '2x': { vus: 10, duration: '60s', p95: 750, errorRate: 0.01 },
  '5x': { vus: 25, duration: '120s', p95: 1000, errorRate: 0.02 },
};
const scenario = configurations[__ENV.SCENARIO || 'baseline'];

if (!baseUrl || !scenario) {
  throw new Error('BASE_URL and a valid SCENARIO (baseline, 2x, or 5x) are required.');
}

export const options = {
  scenarios: {
    capacity: {
      executor: 'constant-vus',
      vus: scenario.vus,
      duration: scenario.duration,
    },
  },
  thresholds: {
    http_req_failed: [`rate<${scenario.errorRate}`],
    http_req_duration: [`p(95)<${scenario.p95}`],
  },
};

export default function () {
  for (const path of ['/health/live', '/health/ready']) {
    const response = http.get(`${baseUrl}${path}`, { tags: { endpoint: path } });
    check(response, {
      [`${path} returns 2xx`]: (result) => result.status >= 200 && result.status < 300,
    });
  }
  sleep(0.1);
}
