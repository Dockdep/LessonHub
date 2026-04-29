# 02 — Infrastructure (Terraform)

Per-resource inventory of [terraform/](../terraform/). All GCP resources are defined here; nothing is created by-hand in the GCP console.

> **Source files**: [terraform/](../terraform/) (apis.tf, cloud_sql.tf, secrets.tf, service_accounts.tf, wif.tf, document_storage.tf, artifact_registry.tf, main.tf, variables.tf, outputs.tf).

## Resource graph

```mermaid
flowchart TD
  classDef internal fill:#e3f2fd,color:#1a1a1a
  classDef data fill:#f3e5f5,color:#1a1a1a
  classDef iam fill:#fff3e0,color:#1a1a1a

  apis[google_project_service<br/>x8 enabled APIs]:::internal

  subgraph storage[Storage]
    sql_inst[google_sql_database_instance<br/>POSTGRES_17]:::data
    db1[(LessonsHub DB)]:::data
    db2[(LessonsAi DB)]:::data
    bucket[("documents bucket")]:::data
    ar[Artifact Registry]:::data
  end

  subgraph secrets_block[Secret Manager]
    s1[db-url-lessonshub]:::internal
    s2[db-url-lessonsai]:::internal
    s3[jwt-secret]:::internal
    s4[google-oauth-client-id]:::internal
  end

  subgraph identities[Service Accounts]
    sa_api[sa-lessonshub]:::iam
    sa_ui[sa-lessonshub-ui]:::iam
    sa_ai[sa-lessons-ai-api]:::iam
    sa_gh[sa-github-deploy]:::iam
  end

  subgraph wif_block[WIF]
    pool[github-pool OIDC]:::iam
    provider[github-provider<br/>repo-locked]:::iam
  end

  apis --> sql_inst
  sql_inst --> db1
  sql_inst --> db2
  sql_inst --> s1
  sql_inst --> s2
  sa_api -- objectAdmin --> bucket
  sa_ai -- objectViewer --> bucket
  sa_api -- cloudsql.client --> sql_inst
  sa_ui -- cloudsql.client --> sql_inst
  sa_ai -- cloudsql.client --> sql_inst
  pool --> provider
  provider -- workloadIdentityUser --> sa_gh
```

## Cloud SQL

One `POSTGRES_17` instance with `deletion_protection = true`, daily backup, and query insights enabled. Two databases share a single `app` user with a 64-char `random_password` regenerated on apply:

- **`LessonsHub`** — .NET app data (entities, plans, lessons, exercises, shares, jobs). Schema migrated by EF Core on app startup.
- **`LessonsAi`** — Python AI data: `DocumentationCache`, `DocumentChunks` (pgvector). Schema bootstrapped by `init_schema()` calls.

Connection strings are composed in [terraform/secrets.tf](../terraform/secrets.tf) and stored in Secret Manager. Cloud Run reaches the instance via the Cloud SQL Auth Proxy (Unix socket); `ipv4_enabled = true` is required by Cloud SQL but no `authorized_networks` are set, so the public IP accepts zero connections.

## Service accounts and IAM

Four service accounts with role separation:

| SA | Roles |
| --- | --- |
| `sa-lessonshub` | `cloudsql.client`, `secretAccessor`, GCS `objectAdmin`, plus `run.invoker` on the AI service (bound by deploy workflow, not Terraform) |
| `sa-lessonshub-ui` | `cloudsql.client`, `secretAccessor` |
| `sa-lessons-ai-api` | `cloudsql.client`, `secretAccessor`, GCS `objectViewer` |
| `sa-github-deploy` | `run.admin`, `artifactregistry.writer`, `iam.serviceAccountUser`, `cloudsql.client` |

GCS access is asymmetric: the .NET service writes (uploads/deletes) and the Python service only reads (chunk + embed at ingest).

## Secret Manager

| Secret | Source | Consumer |
| --- | --- | --- |
| `db-url-lessonshub` | composed from SQL instance + user | .NET API (Npgsql) |
| `db-url-lessonsai` | composed from SQL instance + user | Python AI (asyncpg) |
| `jwt-secret` | `random_password` (64 chars) | .NET API (signs JWTs) |
| `google-oauth-client-id` | tfvars input | UI (One Tap) + .NET (validation) |

Secrets are injected into Cloud Run via `--set-secrets` at deploy time — the running container sees plain env vars, no SDK fetches at runtime.

## Workload Identity Federation

```mermaid
sequenceDiagram
  autonumber
  participant GHA as GitHub Actions
  participant OIDC as token.actions.githubusercontent.com
  participant Provider as github-provider
  participant STS as Google STS
  participant SA as sa-github-deploy

  GHA->>OIDC: request OIDC token
  OIDC-->>GHA: signed JWT (sub, repository, ...)
  GHA->>STS: exchange JWT
  STS->>Provider: verify + check attribute_condition
  alt repo matches assertion.repository
    STS-->>GHA: federated access token
    GHA->>SA: impersonate (workloadIdentityUser)
    SA-->>GHA: SA access token
  else mismatch
    STS--xGHA: 403
  end
```

The `attribute_condition` (`assertion.repository == '${var.github_repo}'`) is the single line that stops other GitHub repos from minting tokens for this project even if audience and subject pattern match.

## What Terraform does NOT manage

- Cloud Run **services themselves** — created/updated by [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). Terraform only sets up the SAs and IAM bindings they need.
- `sa-lessonshub` → `run.invoker` on the AI service — the AI service doesn't exist until the workflow runs, so the workflow adds it idempotently.
- DNS / custom domains, VPC, Cloud NAT — not currently used. Cloud Run ships `*.run.app` URLs and managed networking.
