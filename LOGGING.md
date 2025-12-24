# Logging and Observability Setup

This guide explains how to set up centralized logging, metrics, and tracing using OpenTelemetry, Grafana, Loki, Prometheus, and Tempo.

## Architecture

```
Application (API)
    ↓ (OTLP)
OpenTelemetry Collector
    ↓
    ├─→ Loki (Logs)
    ├─→ Prometheus (Metrics)
    └─→ Tempo (Traces)
        ↓
    Grafana (Visualization)
```

## Quick Start

### Start All Services

```bash
docker-compose up -d
```

This will start:
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Loki**: http://localhost:3100
- **Tempo**: http://localhost:3200
- **OTEL Collector**: Ports 4317 (gRPC), 4318 (HTTP)

### Access Grafana

1. Open http://localhost:3000
2. Login with `admin` / `admin`
3. Data sources are pre-configured:
   - Loki (logs)
   - Prometheus (metrics)
   - Tempo (traces)

## Configuration

### API Configuration

The API automatically exports telemetry to the OpenTelemetry Collector. Configure via environment variables or `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://otel-collector:4317",
    "OtlpHeaders": ""
  }
}
```

Or via environment variables:
```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

### OpenTelemetry Collector

The collector configuration is in `otel-collector-config.yaml`. It:
- Receives OTLP data from applications (gRPC on 4317, HTTP on 4318)
- Processes and enriches telemetry data
- Exports to:
  - **Loki**: Logs
  - **Prometheus**: Metrics (exposed on port 8889)
  - **Tempo**: Traces

## Querying Logs in Grafana

### LogQL Examples

**All logs from API:**
```logql
{job="otel-collector"}
```

**Error logs:**
```logql
{job="otel-collector"} |= "error"
```

**Logs by tenant:**
```logql
{job="otel-collector"} | json | tenant_id="tenant-123"
```

**Time range queries:**
```logql
{job="otel-collector"} | json | severity="Error"
```

## Querying Traces in Grafana (Tempo)

### How to View Traces

1. **Open Grafana Explore:**
   - Click the **Explore** icon (compass) in the left menu
   - Select **Tempo** as the datasource

2. **Search for Traces:**
   - **By Service Name:** Select `service.name = Configuration.Api` from the dropdown
   - **By Operation:** Select `name = GET api/Configuration` or other operations
   - **By Trace ID:** If you have a trace ID, paste it directly
   - **Time Range:** Make sure the time range includes when traces were generated (last 15 minutes, last hour, etc.)

3. **Example Queries:**
   - Search by service: `service.name="Configuration.Api"`
   - Search by operation: `name="GET api/Configuration"`
   - Search by HTTP method: `http.method="GET"`
   - Search by status code: `http.status_code=200`

4. **View Trace Details:**
   - Click on any trace in the results
   - You'll see the full trace timeline with all spans
   - Click on individual spans to see details, tags, and logs

### Troubleshooting Tempo

**If you don't see traces:**
- Check the time range - traces might be older than your selected range
- Make sure the API is generating traces (make some API calls)
- Verify Tempo is receiving data: `curl http://localhost:3200/api/search?limit=10`
- Check OTEL Collector logs: `docker-compose logs otel-collector | grep -i trace`

**Common Issues:**
- **No traces found:** Expand the time range or make new API requests
- **Wrong service name:** Use `service.name="Configuration.Api"` (case-sensitive)
- **Traces too old:** Tempo has limited retention (1 hour by default in this setup)

## Querying Metrics in Grafana

### PromQL Examples

**Request rate:**
```promql
rate(http_server_request_duration_seconds_count[5m])
```

**Error rate:**
```promql
rate(http_server_request_duration_seconds_count{status_code=~"5.."}[5m])
```

**Response time (p95):**
```promql
histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))
```

## Querying Traces in Grafana

1. Go to **Explore** → Select **Tempo** datasource
2. Search by:
   - Service name: `Configuration.Api`
   - Operation: `GET /api/configuration`
   - Tags: `tenant_id=tenant-123`
   - Trace ID (if you have one)

## Pre-built Dashboards

Grafana comes with pre-configured dashboards for:
- **Loki**: Log exploration
- **Prometheus**: Metrics overview
- **Tempo**: Trace analysis

You can also import additional dashboards from [Grafana Dashboards](https://grafana.com/grafana/dashboards/).

## Multi-Tenancy Support

The logging infrastructure automatically enriches logs with tenant context:
- `tenant_id`: Current tenant identifier
- `tenant_name`: Tenant name (if available)

Query tenant-specific logs:
```logql
{service_name="Configuration.Api", tenant_id="tenant-123"}
```

## Production Considerations

### 1. Authentication

Update Grafana credentials:
```yaml
environment:
  - GF_SECURITY_ADMIN_PASSWORD=your-secure-password
```

### 2. Data Retention

Configure retention in `prometheus/prometheus.yml`:
```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    retention: 30d  # Adjust as needed
```

### 3. Resource Limits

Add resource limits to docker-compose.yml:
```yaml
services:
  loki:
    deploy:
      resources:
        limits:
          memory: 2G
        reservations:
          memory: 1G
```

### 4. High Availability

For production, consider:
- Loki in distributed mode
- Prometheus with remote write to long-term storage
- Tempo with object storage backend

### 5. Security

- Use TLS for OTLP endpoints
- Secure Grafana with reverse proxy
- Use API keys for service-to-service communication

## Alternative: Grafana Cloud

For a managed solution, use Grafana Cloud:

1. Sign up at https://grafana.com/auth/sign-up/create-user
2. Get your OTLP endpoint and API key
3. Update API configuration:

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "https://otlp-gateway-prod-us-central-0.grafana.net:443",
    "OtlpHeaders": "Authorization=Basic <base64-encoded-api-key>"
  }
}
```

## Troubleshooting

### No Logs Appearing

1. Check OTEL Collector logs:
   ```bash
   docker-compose logs otel-collector
   ```

2. Verify API is sending data:
   ```bash
   docker-compose logs api | grep -i otlp
   ```

3. Check Loki is receiving data:
   ```bash
   curl http://localhost:3100/ready
   ```

### Metrics Not Showing

1. Verify Prometheus is scraping:
   ```bash
   curl http://localhost:9090/api/v1/targets
   ```

2. Check OTEL Collector metrics endpoint:
   ```bash
   curl http://localhost:8889/metrics
   ```

### Traces Not Appearing

1. Check Tempo is running:
   ```bash
   curl http://localhost:3200/ready
   ```

2. Verify trace export in OTEL Collector logs

## Useful Commands

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f otel-collector

# Restart observability stack
docker-compose restart otel-collector loki prometheus tempo grafana

# Clean up and restart
docker-compose down -v
docker-compose up -d
```

## Additional Resources

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Grafana Loki Documentation](https://grafana.com/docs/loki/latest/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Tempo Documentation](https://grafana.com/docs/tempo/latest/)
- [LogQL Query Language](https://grafana.com/docs/loki/latest/logql/)

