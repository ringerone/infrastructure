# Configuration Management UI

Angular application for managing configurations and feature flags.

## Setup

1. Install dependencies:
```bash
npm install
```

2. Update the API URL in `src/app/configuration.service.ts` if needed.

3. Start the development server:
```bash
npm start
```

The application will be available at `http://localhost:4200`.

## Features

- View and manage configurations with hierarchical scoping
- View and manage feature flags
- Real-time updates via SignalR WebSocket connection
- Support for multiple scopes: Global, Environment, Region, Tenant, User

