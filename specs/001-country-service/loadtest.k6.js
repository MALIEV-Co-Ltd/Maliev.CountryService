/**
 * K6 Load Test Script for Maliev Country Service
 *
 * Purpose: Validate performance targets for read operations
 * Targets:
 *   - Cached reads: p95 < 50ms
 *   - Uncached reads: p95 < 200ms
 *   - List operations: p95 < 100ms
 *
 * Run: k6 run loadtest.k6.js
 * Run with custom VUs: k6 run --vus 100 --duration 30s loadtest.k6.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');
const cachedReadLatency = new Trend('cached_read_latency');
const uncachedReadLatency = new Trend('uncached_read_latency');
const listLatency = new Trend('list_latency');
const searchLatency = new Trend('search_latency');

// Configuration
export const options = {
  stages: [
    { duration: '30s', target: 100 },   // Ramp up to 100 users
    { duration: '1m', target: 500 },    // Ramp up to 500 users
    { duration: '2m', target: 1000 },   // Ramp up to 1,000 users
    { duration: '2m', target: 1000 },   // Stay at 1,000 users
    { duration: '30s', target: 0 },     // Ramp down to 0 users
  ],
  thresholds: {
    'http_req_duration{type:cached_read}': ['p(95)<50'],     // 95% of cached reads < 50ms
    'http_req_duration{type:uncached_read}': ['p(95)<200'],  // 95% of uncached reads < 200ms
    'http_req_duration{type:list}': ['p(95)<100'],           // 95% of list ops < 100ms
    'http_req_duration{type:search}': ['p(95)<150'],         // 95% of search ops < 150ms
    'errors': ['rate<0.01'],                                 // Error rate < 1%
    'http_req_failed': ['rate<0.01'],                        // Failed requests < 1%
  },
};

// Base URL - modify as needed
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const API_BASE = `${BASE_URL}/countries/v1`;

// Sample ISO2 codes for testing (common countries)
const ISO2_CODES = ['US', 'GB', 'FR', 'DE', 'JP', 'CN', 'IN', 'BR', 'CA', 'AU'];

// Sample country IDs (assuming these exist)
const COUNTRY_IDS = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

export default function () {
  const scenario = Math.random();

  if (scenario < 0.4) {
    // 40% - Cached read by ISO2 (most common operation)
    testCachedReadByISO2();
  } else if (scenario < 0.6) {
    // 20% - Read by ID
    testReadById();
  } else if (scenario < 0.75) {
    // 15% - List countries with pagination
    testListCountries();
  } else if (scenario < 0.9) {
    // 15% - Search countries
    testSearchCountries();
  } else {
    // 10% - Read by ISO3
    testReadByISO3();
  }

  // Think time between requests
  sleep(Math.random() * 2);
}

function testCachedReadByISO2() {
  const iso2 = ISO2_CODES[Math.floor(Math.random() * ISO2_CODES.length)];
  const url = `${API_BASE}/countries/iso2/${iso2}`;

  const res = http.get(url, {
    tags: { type: 'cached_read', endpoint: 'iso2' },
  });

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'has country data': (r) => r.json('iso2') !== undefined,
    'has ETag header': (r) => r.headers['Etag'] !== undefined,
    'response time OK': (r) => r.timings.duration < 100,
  });

  cachedReadLatency.add(res.timings.duration);
  errorRate.add(!success);
}

function testReadById() {
  const id = COUNTRY_IDS[Math.floor(Math.random() * COUNTRY_IDS.length)];
  const url = `${API_BASE}/countries/${id}`;

  const res = http.get(url, {
    tags: { type: 'uncached_read', endpoint: 'id' },
  });

  const success = check(res, {
    'status is 200 or 404': (r) => r.status === 200 || r.status === 404,
    'response time OK': (r) => r.timings.duration < 200,
  });

  if (res.status === 200) {
    uncachedReadLatency.add(res.timings.duration);
  }

  errorRate.add(!success);
}

function testReadByISO3() {
  const iso3Codes = ['USA', 'GBR', 'FRA', 'DEU', 'JPN', 'CHN', 'IND', 'BRA', 'CAN', 'AUS'];
  const iso3 = iso3Codes[Math.floor(Math.random() * iso3Codes.length)];
  const url = `${API_BASE}/countries/iso3/${iso3}`;

  const res = http.get(url, {
    tags: { type: 'cached_read', endpoint: 'iso3' },
  });

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'has country data': (r) => r.json('iso3') !== undefined,
    'response time OK': (r) => r.timings.duration < 100,
  });

  cachedReadLatency.add(res.timings.duration);
  errorRate.add(!success);
}

function testListCountries() {
  const page = Math.floor(Math.random() * 10) + 1;
  const pageSize = [10, 20, 50][Math.floor(Math.random() * 3)];
  const sortBy = ['name', 'iso2', 'population'][Math.floor(Math.random() * 3)];
  const sortOrder = Math.random() > 0.5 ? 'asc' : 'desc';

  const url = `${API_BASE}/countries?page=${page}&pageSize=${pageSize}&sortBy=${sortBy}&sortOrder=${sortOrder}`;

  const res = http.get(url, {
    tags: { type: 'list', endpoint: 'list' },
  });

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'has data array': (r) => Array.isArray(r.json('data')),
    'has pagination metadata': (r) => r.json('page') !== undefined,
    'has X-Total-Count header': (r) => r.headers['X-Total-Count'] !== undefined,
    'response time OK': (r) => r.timings.duration < 150,
  });

  listLatency.add(res.timings.duration);
  errorRate.add(!success);
}

function testSearchCountries() {
  const queries = ['United', 'Republic', 'Kingdom', 'Island', 'States'];
  const query = queries[Math.floor(Math.random() * queries.length)];
  const page = Math.floor(Math.random() * 5) + 1;

  const url = `${API_BASE}/countries/search?q=${query}&page=${page}&pageSize=10`;

  const res = http.get(url, {
    tags: { type: 'search', endpoint: 'search' },
  });

  const success = check(res, {
    'status is 200': (r) => r.status === 200,
    'has data array': (r) => Array.isArray(r.json('data')),
    'response time OK': (r) => r.timings.duration < 200,
  });

  searchLatency.add(res.timings.duration);
  errorRate.add(!success);
}

/**
 * Setup function - runs once before test starts
 */
export function setup() {
  console.log('========================================');
  console.log('Maliev Country Service Load Test');
  console.log('========================================');
  console.log(`Target URL: ${BASE_URL}`);
  console.log('Test Duration: 6 minutes');
  console.log('Peak VUs: 1,000');
  console.log('Performance Targets:');
  console.log('  - Cached reads: p95 < 50ms');
  console.log('  - Uncached reads: p95 < 200ms');
  console.log('  - List operations: p95 < 100ms');
  console.log('========================================\n');

  // Verify service is accessible
  const healthCheck = http.get(`${BASE_URL}/countries/v1/liveness`);
  if (healthCheck.status !== 200) {
    throw new Error(`Health check failed. Status: ${healthCheck.status}`);
  }
  console.log('✓ Service health check passed\n');
}

/**
 * Teardown function - runs once after test completes
 */
export function teardown(data) {
  console.log('\n========================================');
  console.log('Load Test Completed');
  console.log('========================================');
  console.log('Review the metrics above to verify:');
  console.log('  1. All thresholds passed (✓)');
  console.log('  2. Error rate < 1%');
  console.log('  3. p95 latencies meet targets');
  console.log('========================================\n');
}

/**
 * Handle summary - custom test summary
 */
export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: '  ', enableColors: true }),
    'loadtest-results.json': JSON.stringify(data),
  };
}

// Helper function for text summary (built-in k6 function)
function textSummary(data, options) {
  const indent = options?.indent || '';
  const colors = options?.enableColors ?? true;

  let summary = `\n${indent}Test Summary:\n`;
  summary += `${indent}=============\n\n`;

  // HTTP metrics
  summary += `${indent}HTTP Requests:\n`;
  summary += `${indent}  Total: ${data.metrics.http_reqs.values.count}\n`;
  summary += `${indent}  Rate: ${data.metrics.http_reqs.values.rate.toFixed(2)}/s\n`;
  summary += `${indent}  Failed: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%\n\n`;

  // Duration metrics
  summary += `${indent}Request Duration:\n`;
  summary += `${indent}  p50: ${data.metrics.http_req_duration.values['p(50)'].toFixed(2)}ms\n`;
  summary += `${indent}  p95: ${data.metrics.http_req_duration.values['p(95)'].toFixed(2)}ms\n`;
  summary += `${indent}  p99: ${data.metrics.http_req_duration.values['p(99)'].toFixed(2)}ms\n`;
  summary += `${indent}  max: ${data.metrics.http_req_duration.values.max.toFixed(2)}ms\n\n`;

  // Virtual users
  summary += `${indent}Virtual Users:\n`;
  summary += `${indent}  Max: ${data.metrics.vus_max.values.max}\n\n`;

  return summary;
}
