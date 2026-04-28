# 02 — Infrastructure (Terraform)

Per-resource inventory of [terraform/](../terraform/). All GCP resources for LessonsHub are defined here; nothing is created by-hand in the GCP console (except the initial GCP project).

> **Source files**: [terraform/apis.tf](../terraform/apis.tf), [terraform/cloud_sql.tf](../terraform/cloud_sql.tf), [terraform/secrets.tf](../terraform/secrets.tf), [terraform/service_accounts.tf](../terraform/service_accounts.tf), [terraform/wif.tf](../terraform/wif.tf), [terraform/document_storage.tf](../terraform/document_storage.tf), [terraform/artifact_registry.tf](../terraform/artifact_registry.tf), [terraform/main.tf](../terraform/main.tf), [terraform/variables.tf](../terraform/variables.tf), [terraform/outputs.tf](../terraform/outputs.tf).

## Resource graph

```mermaid
flowchart TD
  classDef external stroke-dasharray: 5 5,fill:#fff8e7
  classDef internal fill:#e3f2fd
  classDef data fill:#f3e5f5
  classDef iam fill:#fff3e0

  apis[google_project_service<br/>x8 enabled APIs]:::internal

  subgraph storage[Storage]
    direction LR
    sql_inst[google_sql_database_instance<br/>lessonshub-db<br/>POSTGRES_17]:::data
    db1[(google_sql_database<br/>LessonsHub)]:::data
    db2[(google_sql_database<br/>LessonsAi)]:::data
    sql_user[google_sql_user<br/>app + random_password]:::data
    bucket[("google_storage_bucket<br/>&lt;project&gt;-documents")]:::data
    ar[google_artifact_registry_repository<br/>lessonshub Docker repo]:::data
  end

  subgraph secrets_block[Secret Manager]
    direction LR
    s1[db-url-lessonshub<br/>auto-composed from SQL inst]:::internal
    s2[db-url-lessonsai<br/>auto-composed from SQL inst]:::internal
    s3[jwt-secret<br/>random 64-char]:::internal
    s4[google-oauth-client-id<br/>from tfvars]:::internal
    s5[context7-api-key<br/>from tfvars]:::internal
  end

  subgraph identities[Service Accounts]
    direction LR
    sa_api[sa-lessonshub]:::iam
    sa_ui[sa-lessonshub-ui]:::iam
    sa_ai[sa-lessons-ai-api]:::iam
    sa_gh[sa-github-deploy]:::iam
  end

  subgraph wif_block[Workload Identity Federation]
    direction LR
    pool[github-pool<br/>OIDC]:::iam
    provider[github-provider<br/>locked to repo]:::iam
  end

  apis --> sql_inst
  apis --> ar
  apis --> bucket
  sql_inst --> db1
  sql_inst --> db2
  sql_inst --> sql_user
  sql_user --> s1
  sql_user --> s2

  sa_api -- objectAdmin --> bucket
  sa_ai -- objectViewer --> bucket
  sa_api -- cloudsql.client + secretAccessor --> sql_inst
  sa_ui -- cloudsql.client + secretAccessor --> sql_inst
  sa_ai -- cloudsql.client + secretAccessor --> sql_inst

  sa_gh -- run.admin + artifactregistry.writer<br/>iam.serviceAccountUser + cloudsql.client --> apis
  pool --> provider
  provider -- workloadIdentityUser --> sa_gh
```

## Enabled APIs

[terraform/apis.tf](../terraform/apis.tf) toggles eight APIs:

| API | Why |
|---|---|
| `run.googleapis.com` | Cloud Run hosts the three services |
| `sqladmin.googleapis.com` | Cloud SQL provisioning |
| `artifactregistry.googleapis.com` | Docker image storage |
| `secretmanager.googleapis.com` | Per-service secret injection |
| `iamcredentials.googleapis.com` | SA impersonation (used by the .NET → AI invoker call) |
| `iam.googleapis.com` | Workload Identity Federation needs IAM admin |
| `sts.googleapis.com` | Federated token exchange (the OIDC step) |
| `cloudbuild.googleapis.com` | Triggered indirectly by `gcloud run deploy --source` if used |

`disable_on_destroy = false` so a `terraform destroy` doesn't yank APIs another project might share.

## Cloud SQL

```mermaid
classDiagram
  class google_sql_database_instance {
    +string name "lessonshub-db"
    +string region var.region
    +string database_version "POSTGRES_17"
    +string tier var.sql_tier
    +bool ipv4_enabled true
    +bool deletion_protection true
    +backup start_time "03:00"
    +bool point_in_time_recovery_enabled false
    +bool query_insights_enabled true
  }
  class google_sql_database {
    +string name
    +string instance
  }
  class google_sql_user {
    +string name "app"
    +string password from random_password
  }
  google_sql_database_instance "1" --> "2" google_sql_database : hosts
  google_sql_database_instance "1" --> "1" google_sql_user : owns
```

Two databases on one instance:

- **`LessonsHub`** — the .NET app's data (entities, plans, lessons, exercises, shares, etc.). Schema migrated by EF Core on app startup.
- **`LessonsAi`** — the Python AI service's data: `DocumentationCache` (search-result cache), `DocumentChunks` (pgvector embeddings for RAG). Schema bootstrapped by `init_schema()` in [tools/doc_cache.py](../lessons-ai-api/tools/doc_cache.py) and [tools/rag_store.py](../lessons-ai-api/tools/rag_store.py).

Both databases share the *same* `app` user with a random 64-character password generated at apply time. Connection strings are composed in [terraform/secrets.tf](../terraform/secrets.tf) and stored in Secret Manager.

> **Why `ipv4_enabled = true` if there's no public access?** Cloud SQL refuses to provision without at least one connectivity option enabled. With no `authorized_networks` block, the public IP exists but accepts zero connections. Cloud Run reaches the instance via the Cloud SQL Auth Proxy (`/cloudsql/<instance>` Unix socket); local access uses `cloud-sql-proxy` with `gcloud` credentials.

## Service accounts and IAM

[terraform/service_accounts.tf](../terraform/service_accounts.tf) creates 4 SAs:

```mermaid
flowchart LR
  classDef sa fill:#fff3e0

  sa_api[sa-lessonshub]:::sa
  sa_ui[sa-lessonshub-ui]:::sa
  sa_ai[sa-lessons-ai-api]:::sa
  sa_gh[sa-github-deploy]:::sa

  sa_api --> r1[roles/cloudsql.client]
  sa_api --> r2[roles/secretmanager.secretAccessor]
  sa_api --> r3[GCS objectAdmin on documents bucket]
  sa_api --> r4["roles/run.invoker on lessons-ai-api<br/>(bound by deploy workflow, not Terraform)"]

  sa_ui --> r1
  sa_ui --> r2

  sa_ai --> r1
  sa_ai --> r2
  sa_ai --> r5[GCS objectViewer on documents bucket]

  sa_gh --> r6[roles/run.admin]
  sa_gh --> r7[roles/artifactregistry.writer]
  sa_gh --> r8[roles/iam.serviceAccountUser]
  sa_gh --> r9[roles/cloudsql.client]
```

The `.NET → AI` `roles/run.invoker` binding is *not* in Terraform: the AI Cloud Run service is created by the deploy workflow (post-Terraform), so the deploy workflow adds that binding idempotently.

## GCS bucket for uploaded documents

[terraform/document_storage.tf](../terraform/document_storage.tf) — one regional bucket per project, used by the document-upload feature.

```mermaid
classDiagram
  class google_storage_bucket {
    +string name "&lt;project_id&gt;-documents"
    +string location var.region
    +bool uniform_bucket_level_access true
    +string public_access_prevention "enforced"
    +string storage_class "STANDARD"
    +bool versioning false
    +bool force_destroy false
  }
  class lessonshub_writes {
    +role objectAdmin
    +member sa-lessonshub
  }
  class lessons_ai_reads {
    +role objectViewer
    +member sa-lessons-ai-api
  }
  google_storage_bucket "1" --> "1" lessonshub_writes
  google_storage_bucket "1" --> "1" lessons_ai_reads
```

Asymmetric access by design: only the .NET service writes (uploads + deletes); the Python service only reads (chunk + embed at ingest).

## Secret Manager

[terraform/secrets.tf](../terraform/secrets.tf) creates five secret containers and writes initial versions:

| Secret | Source | Consumer |
|---|---|---|
| `db-url-lessonshub` | composed from Cloud SQL instance + db + user | `lessonshub` (.NET, Npgsql format) |
| `db-url-lessonsai` | composed from Cloud SQL instance + db + user | `lessons-ai-api` (asyncpg format) |
| `jwt-secret` | `random_password` (64 chars) | `lessonshub` (signs JWTs issued after Google login) |
| `google-oauth-client-id` | `var.google_oauth_client_id` (you provide via tfvars) | `lessonshub-ui` (One Tap) + `lessonshub` (token validation) |
| `context7-api-key` | `var.context7_api_key` | (legacy, unused after the framework-analyzer refactor; keep for now) |

Cloud Run services inject these via `--set-secrets` flags in the deploy workflow, so the running container only sees env vars — no SDK calls to fetch secrets at runtime.

## Workload Identity Federation (GitHub Actions)

[terraform/wif.tf](../terraform/wif.tf) — replaces long-lived JSON keys for CI/CD.

```mermaid
sequenceDiagram
  autonumber
  participant GHA as GitHub Actions
  participant OIDC as token.actions.githubusercontent.com
  participant Pool as github-pool
  participant Provider as github-provider<br/>(attribute_condition)
  participant STS as Google STS
  participant SA as sa-github-deploy

  GHA->>OIDC: request OIDC token (audience: GCP)
  OIDC-->>GHA: signed JWT (sub, repository, repository_owner)
  GHA->>STS: exchange JWT
  STS->>Provider: verify issuer + claims
  Provider->>Provider: assert<br/>repository == 'owner/repo'
  alt repo matches
    STS-->>GHA: federated access token
    GHA->>SA: impersonate (workloadIdentityUser binding)
    SA-->>GHA: service account access token
  else repo mismatch
    STS--xGHA: deny (403)
  end
```

The `attribute_condition` (`assertion.repository == '${var.github_repo}'`) is what stops *any other GitHub repo* from minting tokens for this project, even if the audience and subject pattern match.

## Artifact Registry

[terraform/artifact_registry.tf](../terraform/artifact_registry.tf) — one Docker repository per project, in the same region as Cloud Run for fastest pulls. Repo ID `lessonshub`. The deploy workflow tags images as:

- `<region>-docker.pkg.dev/<project>/lessonshub/lessonshub:<sha>`
- `<region>-docker.pkg.dev/<project>/lessonshub/lessonshub-ui:<sha>`
- `<region>-docker.pkg.dev/<project>/lessonshub/lessons-ai-api:<sha>`

## State management

`main.tf` uses **local state** (`terraform.tfstate` + backup files committed to `.gitignore`). For multi-developer workflows, swap to a `gcs` backend block — the comment in [terraform/main.tf](../terraform/main.tf) outlines this.

## What Terraform does NOT manage

- The Cloud Run **services themselves** — created/updated by the deploy workflow ([.github/workflows/deploy.yml](../.github/workflows/deploy.yml)). Terraform only sets up the SAs and IAM bindings they need.
- The `.NET → AI` `run.invoker` binding — same reason (the AI service doesn't exist until the workflow runs).
- DNS / custom domains — not currently configured. Cloud Run ships `*.run.app` URLs.
- VPC / Cloud NAT — not used. All Cloud Run traffic is public-internet-routed via Cloud Run's managed networking.

## Variables ([variables.tf](../terraform/variables.tf))

| Variable | Default | Purpose |
|---|---|---|
| `project_id` | (required) | GCP project ID |
| `region` | (required) | GCP region for all resources |
| `sql_tier` | (required) | Cloud SQL machine tier (e.g. `db-f1-micro`) |
| `github_repo` | (required) | `owner/repo` for WIF binding |
| `google_oauth_client_id` | (required) | Stored as a secret |
| `context7_api_key` | "" | Legacy — unused post-refactor |

[terraform.tfvars.example](../terraform/terraform.tfvars.example) shows the expected shape; copy to `terraform.tfvars` (gitignored) and fill in.

## Outputs ([outputs.tf](../terraform/outputs.tf))

The deploy workflow consumes these to know what to deploy *into*:

- Cloud SQL instance connection name (for `--add-cloudsql-instances` flag).
- Service account emails (for `--service-account` flag per Cloud Run service).
- Artifact Registry repo URL (for `docker push`).
- Workload Identity provider full resource path (for `google-github-actions/auth`).
