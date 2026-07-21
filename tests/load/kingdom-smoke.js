/* global __ENV */
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    bootstrap: {
      executor: 'constant-vus',
      vus: 50,
      duration: '2m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<250'],
  },
};

const baseUrl = __ENV.API_BASE_URL ?? 'http://localhost:3000';

export function setup() {
  const installationId = '0198a7c9-5e62-7f54-a22d-c40f2f82ed48';
  const response = http.post(
    `${baseUrl}/v1/auth/guest`,
    JSON.stringify({ installation_id: installationId, display_name: 'LoadTester' }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(response, { 'guest auth succeeds': (result) => result.status === 201 });
  return { token: response.json('data.access_token') };
}

export default function (data) {
  const response = http.get(`${baseUrl}/v1/bootstrap`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });
  check(response, { 'bootstrap succeeds': (result) => result.status === 200 });
  sleep(1);
}
