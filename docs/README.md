# Engineering context

## About this repository

This repository sends periodic endpoint heartbeats to ServiceControl.

- `src/NServiceBus.Heartbeat/` — plugin implementation
- `src/NServiceBus.Heartbeat.Tests/` and `src/NServiceBus.Heartbeat.AcceptanceTests/` — unit and acceptance tests
- `.github/workflows/` — CI pipelines

## Start here

- [Component documentation](https://docs.particular.net/monitoring/heartbeats/) — understand how ServiceControl detects missing endpoint heartbeats
- [Install the Heartbeat plugin](https://docs.particular.net/monitoring/heartbeats/install-plugin) — configure the destination and heartbeat interval
- [Repository README](../README.md) — package overview and support links
- [Contributing](https://docs.particular.net/platform/contributing) — contribution process

## Decisions and rationale

- [Architecture and design decisions](decisions/)

Keep this index current when a canonical source is added, replaced, or retired; link, do not copy.
