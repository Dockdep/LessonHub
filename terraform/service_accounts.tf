locals {
  cloud_run_services = ["lessonshub", "lessonshub-ui", "lessons-ai-api"]
}

# ---------------------------------------------------------------------------
# Per-Cloud-Run-service identities. Each service runs as its own SA so IAM
# permissions stay narrow.
# ---------------------------------------------------------------------------
resource "google_service_account" "cloud_run" {
  for_each = toset(local.cloud_run_services)

  account_id   = "sa-${each.key}"
  display_name = "Cloud Run: ${each.key}"
}

# All three need to talk to Cloud SQL.
resource "google_project_iam_member" "cloud_run_cloudsql" {
  for_each = google_service_account.cloud_run

  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${each.value.email}"
}

# All three read secrets at startup (DB URL, JWT secret, etc.).
resource "google_project_iam_member" "cloud_run_secrets" {
  for_each = google_service_account.cloud_run

  project = var.project_id
  role    = "roles/secretmanager.secretAccessor"
  member  = "serviceAccount:${each.value.email}"
}

# ---------------------------------------------------------------------------
# CI/CD identity. GitHub Actions impersonates this SA via Workload Identity
# Federation (no JSON keys involved — see wif.tf).
# ---------------------------------------------------------------------------
resource "google_service_account" "github_deploy" {
  account_id   = "sa-github-deploy"
  display_name = "GitHub Actions deploy"
}

resource "google_project_iam_member" "github_deploy" {
  for_each = toset([
    "roles/run.admin",                  # deploy + update Cloud Run services
    "roles/artifactregistry.writer",    # push images
    "roles/iam.serviceAccountUser",     # actAs the per-service SAs
    "roles/cloudsql.client",            # only for migration / setup work
  ])

  project = var.project_id
  role    = each.key
  member  = "serviceAccount:${google_service_account.github_deploy.email}"
}

# Note on the .NET → Python service-to-service IAM binding:
# `roles/run.invoker` on the AI service for sa-lessonshub is NOT managed here
# because the AI service itself is created by the deploy workflow, not by
# Terraform. The workflow adds the binding idempotently after deploy.
