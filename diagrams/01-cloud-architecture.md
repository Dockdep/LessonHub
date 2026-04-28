# 01 — Cloud Architecture

The runtime topology of LessonsHub. Three Cloud Run services, one Cloud SQL instance with two databases, one GCS bucket, and a handful of external integrations.

> **Source files**: [docker-compose.example.yml](../docker-compose.example.yml), [terraform/](../terraform/), [Caddyfile](../Caddyfile), [.github/workflows/deploy.yml](../.github/workflows/deploy.yml). For per-resource Terraform inventory see [02-infrastructure-terraform.md](02-infrastructure-terraform.md).

## Production topology (GCP)

```mermaid
flowchart LR
  classDef external stroke-dasharray: 5 5,fill:#fff8e7
  classDef internal fill:#e3f2fd
  classDef data fill:#f3e5f5
  classDef cicd fill:#fff3e0

  user([Browser]):::external
  google((Google OAuth)):::external
  gemini((Gemini API)):::external
  ddg((DuckDuckGo HTML)):::external
  yt((YouTube Data API v3)):::external

  subgraph gcp[Google Cloud Platform]
    direction LR

    subgraph cr[Cloud Run]
      ui[lessonshub-ui<br/>Angular 21 SSR<br/>Node 20]:::internal
      api[lessonshub<br/>.NET 8 API]:::internal
      ai[lessons-ai-api<br/>Python 3.12 + FastAPI]:::internal
    end

    subgraph sql[Cloud SQL Postgres 17]
      db1[(LessonsHub DB)]:::data
      db2[(LessonsAi DB<br/>+ pgvector)]:::data
    end

    gcs[("GCS Bucket<br/>&lt;project&gt;-documents")]:::data

    sm[Secret Manager]:::internal
    ar[Artifact Registry]:::internal

    subgraph ci[GitHub Actions]
      gh[deploy.yml]:::cicd
    end
  end

  user -- HTTPS --> ui
  ui -- /api/* --> api
  ui --> google
  api -- HTTP IAM-signed<br/>Bearer token --> ai
  api --> db1
  api --> gcs
  ai --> db2
  ai --> gcs
  ai --> gemini
  ai --> ddg
  ai --> yt
  api --> sm
  ai --> sm
  ui --> sm
  gh -- WIF OIDC --> cr
  gh -- push images --> ar
  cr -- pull images --> ar
```

**Notes**

- The Angular UI runs as Server-Side-Rendered Node, *not* as a static SPA. Browsers hit it directly for the initial render; subsequent calls are XHR.
- `.NET` → Python AI is **service-to-service** via a Google ID token (the .NET service uses `IamAuthHandler` to mint tokens; Cloud Run validates them). The AI service is not publicly reachable.
- All three Cloud Run services connect to Postgres via the **Cloud SQL Auth Proxy** (`/cloudsql/<instance>` Unix socket); no public-IP allowlist.
- Workload Identity Federation lets GitHub Actions impersonate `sa-github-deploy` *without* a long-lived JSON key.

## Local-dev topology (docker-compose)

For development on a laptop, the same containers run behind a Caddy reverse-proxy on `:80`. Postgres is a single instance the developer runs locally with two databases (`LessonsHub`, `LessonsAi`) — same shape as prod, different connection details.

```mermaid
flowchart LR
  classDef external stroke-dasharray: 5 5,fill:#fff8e7
  classDef internal fill:#e3f2fd
  classDef data fill:#f3e5f5

  user([Browser http://localhost]):::external
  caddy[Caddy<br/>:80 reverse-proxy]:::internal
  ui[lessonshub-ui<br/>:4000]:::internal
  api[lessonshub<br/>:8080]:::internal
  ai[lessons-ai-api<br/>:8000]:::internal
  pg[(Postgres 17<br/>host.docker.internal:5432<br/>two databases)]:::data
  uploads[(./uploads/<br/>local FS volume)]:::data
  google((Google OAuth)):::external
  gemini((Gemini API)):::external

  user --> caddy
  caddy -- /api/* /swagger* --> api
  caddy -- everything else --> ui
  ui --> google
  api --> pg
  api --> ai
  api --> uploads
  ai --> pg
  ai --> uploads
  ai --> gemini
```

**Routing rules** (verbatim from [Caddyfile](../Caddyfile)):

- `/api/*` → `lessonshub:8080` (the .NET API)
- `/swagger*` → `lessonshub:8080` (Swagger UI for the .NET API)
- everything else → `lessonshub-ui:4000` (Angular SSR)

The single-origin trick means the browser sees one host (`http://localhost`), so there are no CORS preflights. The Angular `/api/*` URLs work as-is.

## External integrations

```mermaid
flowchart LR
  classDef external stroke-dasharray: 5 5,fill:#fff8e7
  classDef internal fill:#e3f2fd

  ui[Angular UI]:::internal
  api[.NET API]:::internal
  ai[Python AI]:::internal

  go((Google OAuth<br/>One Tap)):::external
  gem((Gemini API<br/>generation + embeddings)):::external
  ddg((DuckDuckGo<br/>HTML scrape)):::external
  yt((YouTube Data API v3)):::external

  ui --"id_token"--> go
  api --"validate id_token"--> go
  ai --"per-user API key"--> gem
  ai --"site:domain queries"--> ddg
  ai --"video search"--> yt
```

| Integration | Used by | Purpose |
|---|---|---|
| Google OAuth (One Tap) | `lessonshub-ui` (issue) + `lessonshub` (validate via `IGoogleTokenValidator`) | Auth |
| Gemini (`google-genai` SDK) | `lessons-ai-api` | LLM calls (CrewAI agents) + text embeddings (`text-embedding-004`) |
| DuckDuckGo (`ddgs`) | `lessons-ai-api` (`tools/documentation_search.py`) | Free web search for Technical-lesson framework grounding |
| YouTube Data API | `lessons-ai-api` (`tools/youtube_search_tool.py`) | Video resource lookups |

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
  Actions->>WIF: OIDC token (audience: GCP)
  WIF-->>Actions: federated access token
  Actions->>SA: impersonate
  Actions->>AR: docker push (3 images)
  Actions->>CR: deploy lessonshub
  Actions->>CR: deploy lessonshub-ui
  Actions->>CR: deploy lessons-ai-api
  Actions->>CR: bind sa-lessonshub<br/>as run.invoker on lessons-ai-api
```

The WIF binding is locked to a single GitHub repo via an `attribute_condition` on the OIDC pool provider — other repos cannot mint tokens for this project even if they aim at the same audience. See [terraform/wif.tf](../terraform/wif.tf).
