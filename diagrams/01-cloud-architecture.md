# 01 — Cloud Architecture

Three Cloud Run services, one Cloud SQL instance with two databases, one GCS bucket, and a handful of external integrations.

> **Source files**: [terraform/](../terraform/), [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). For per-resource Terraform inventory see [02-infrastructure-terraform.md](02-infrastructure-terraform.md).

## Production topology

```mermaid
flowchart LR
  classDef external stroke-dasharray: 5 5,fill:#fff8e7,color:#1a1a1a
  classDef internal fill:#e3f2fd,color:#1a1a1a
  classDef data fill:#f3e5f5,color:#1a1a1a
  classDef cicd fill:#fff3e0,color:#1a1a1a

  user([Browser]):::external
  google((Google OAuth)):::external
  gemini((Gemini API)):::external
  ddg((DuckDuckGo)):::external
  yt((YouTube Data API)):::external

  subgraph gcp[Google Cloud Platform]
    direction LR

    subgraph cr[Cloud Run]
      ui[lessonshub-ui<br/>Angular 21 SSR]:::internal
      api[lessonshub<br/>.NET 8 API]:::internal
      ai[lessons-ai-api<br/>Python + FastAPI]:::internal
    end

    subgraph sql[Cloud SQL Postgres 17]
      db1[(LessonsHub DB)]:::data
      db2[(LessonsAi DB<br/>+ pgvector)]:::data
    end

    gcs[("GCS Bucket")]:::data
    sm[Secret Manager]:::internal
    ar[Artifact Registry]:::internal
    gh[GitHub Actions deploy.yml]:::cicd
  end

  user -- HTML/JS/CSS --> ui
  user -- /api/* /hubs/* --> api
  ui --> google
  api -- IAM-signed Bearer --> ai
  api --> db1
  api --> gcs
  ai --> db2
  ai --> gcs
  ai --> gemini
  ai --> ddg
  ai --> yt
  api --> sm
  ai --> sm
  gh -- WIF OIDC --> cr
  gh --> ar
  cr -- pull --> ar
```

## Key facts

- **No reverse proxy in front of Cloud Run.** Each service has its own `*.a.run.app` URL. The Angular SSR server injects `API_BASE_URL` into rendered HTML as `<meta name="api-base-url">`; browser-side code reads it and prefixes `/api/*` and `/hubs/*` URLs. The browser talks **directly** to the API service cross-origin. CORS is configured on the .NET side with `AllowCredentials()` (required for SignalR negotiate).
- **`.NET → Python AI` is service-to-service** via a Google ID token (`IamAuthHandler` mints, Cloud Run validates). The AI service is not publicly reachable.
- **Postgres** is reached via the Cloud SQL Auth Proxy (Unix socket), no public-IP allowlist.
- The `lessonshub` service runs `--min-instances=1 --no-cpu-throttling --max-instances=1` because SignalR needs an always-warm CPU and the in-memory job queue can't span instances without a Redis backplane.
- **WIF** lets GitHub Actions impersonate `sa-github-deploy` without long-lived JSON keys, locked to a single repo via `attribute_condition`.

## External integrations

| Integration | Used by | Purpose |
| --- | --- | --- |
| Google OAuth (One Tap) | UI (issue) + .NET (validate) | Auth |
| Gemini (`google-genai`) | Python AI | LLM calls + embeddings (`text-embedding-004`) |
| DuckDuckGo (`ddgs`) | Python AI | Free web search for Technical-lesson framework grounding |
| YouTube Data API | Python AI | Video resource lookups |

## CI/CD pipeline

```mermaid
sequenceDiagram
  autonumber
  actor Dev
  participant GH as GitHub
  participant Actions as GitHub Actions
  participant WIF as GCP Workload Identity
  participant SA as sa-github-deploy
  participant AR as Artifact Registry
  participant CR as Cloud Run

  Dev->>GH: git push main
  GH->>Actions: trigger deploy.yml
  Actions->>WIF: OIDC token
  WIF-->>Actions: federated access token
  Actions->>SA: impersonate
  Actions->>AR: docker push (3 images)
  Actions->>CR: deploy 3 services
  Actions->>CR: bind sa-lessonshub as run.invoker on lessons-ai-api
```

WIF binding is locked to a single GitHub repo via `attribute_condition` on the OIDC pool provider — see [terraform/wif.tf](../terraform/wif.tf).
