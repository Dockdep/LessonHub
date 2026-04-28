# LessonsHub — Terraform infrastructure

Manages every GCP resource the deploy workflow depends on:

- 8 GCP APIs enabled
- Artifact Registry Docker repo
- Cloud SQL Postgres 17 instance with pgvector flag, two databases (`LessonsHub`, `LessonsAi`), one app user
- 3 per-service identities (`sa-lessonshub`, `sa-lessonshub-ui`, `sa-lessons-ai-api`) + IAM bindings (Cloud SQL client, Secret Manager accessor)
- 1 CI/CD identity (`sa-github-deploy`) + IAM bindings (Cloud Run admin, Artifact Registry writer, IAM SA user)
- 5 Secret Manager entries with values:
  - `db-url-lessonshub`, `db-url-lessonsai` — auto-composed from a random app-user password
  - `jwt-secret` — random 64 chars
  - `google-oauth-client-id`, `context7-api-key` — from your tfvars
- Workload Identity Federation pool + GitHub OIDC provider, locked to your specific repo

What it does NOT manage:
- The Cloud Run services themselves (deployed by `.github/workflows/deploy.yml`)
- The `roles/run.invoker` binding for `.NET → AI` S2S auth (added idempotently by the workflow)

## First-time apply

```bash
cd terraform/

# 1. Bootstrap a couple of APIs that Terraform itself needs to call.
gcloud services enable cloudresourcemanager.googleapis.com serviceusage.googleapis.com --project=YOUR_PROJECT_ID

# 2. Authenticate locally so Terraform can call GCP.
gcloud auth application-default login
gcloud config set project YOUR_PROJECT_ID

# 3. Provide variable values.
cp terraform.tfvars.example terraform.tfvars
# edit terraform.tfvars with your project ID, GitHub repo, OAuth client ID, Context7 key

# 4. Init + apply.
terraform init
terraform apply
# review the plan, type 'yes' to apply

# 5. Read the outputs and add them to GitHub repo Variables.
terraform output github_actions_variables
```

## Subsequent applies

When you change variables or want to roll keys:

```bash
terraform plan        # see what changes
terraform apply       # apply
```

`terraform apply` is idempotent — running it on an unchanged config is a no-op.

## Rolling individual secrets

The auto-generated secrets (DB password, JWT secret) rotate by tainting and reapplying:

```bash
terraform taint random_password.jwt_secret
terraform apply        # generates a new JWT secret, writes a new secret version
```

Cloud Run services pick up new secret versions on next deploy (they reference `:latest`).

## Tearing down

```bash
# 1. Disable deletion protection on the SQL instance.
# Edit cloud_sql.tf: deletion_protection = false
terraform apply

# 2. Destroy.
terraform destroy
```

`disable_on_destroy = false` on APIs means they stay enabled — saves a slow re-enable on the next apply, and avoids breaking other projects sharing the same APIs.

## State management

State is **local** by default — the `.tfstate` file lives in this directory and is gitignored. It contains:
- Random passwords (DB user, JWT secret)
- Composed secret values (DB connection strings)
- Terraform's record of what it created

For a single-operator setup this is fine. To move to remote state (multi-operator, CI-applied):

1. Create a GCS bucket: `gsutil mb -l us-central1 gs://lessonshub-tfstate`
2. Uncomment the `backend "gcs"` block in `main.tf`
3. `terraform init -migrate-state`

## What lives where

| Thing | Owner |
|---|---|
| Project, billing | manual (one-time, in GCP Console) |
| APIs, SQL, IAM, secrets, WIF | **this Terraform** |
| Cloud Run services + revisions | `.github/workflows/deploy.yml` |
| Container images | built by the workflow, pushed to Artifact Registry |
| User data (Postgres rows) | the apps themselves |
| Per-user Gemini API keys | DB column `User.GoogleApiKey` |
