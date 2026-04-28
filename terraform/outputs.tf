# ---------------------------------------------------------------------------
# After `terraform apply`, paste these into GitHub →
# Settings → Secrets and variables → Actions → Variables tab.
# (Variables, not Secrets — these values aren't sensitive.)
# ---------------------------------------------------------------------------

output "github_actions_variables" {
  description = "Add these as GitHub repo Variables. The deploy workflow reads them via vars.NAME."

  value = {
    GCP_PROJECT_ID = var.project_id
    WIF_PROVIDER   = "projects/${data.google_project.current.number}/locations/global/workloadIdentityPools/${google_iam_workload_identity_pool.github.workload_identity_pool_id}/providers/${google_iam_workload_identity_pool_provider.github.workload_identity_pool_provider_id}"
    DEPLOY_SA      = google_service_account.github_deploy.email
  }
}

output "sql_instance_connection_name" {
  description = "Cloud SQL instance connection name (PROJECT:REGION:INSTANCE). Used by Cloud Run --add-cloudsql-instances."
  value       = google_sql_database_instance.main.connection_name
}

output "secret_ids" {
  description = "Names of the Secret Manager entries the deploy workflow reads from."
  value       = local.secret_ids
}

output "cloud_run_service_accounts" {
  description = "Per-service identities the deploy workflow assigns to each Cloud Run service."
  value       = { for k, sa in google_service_account.cloud_run : k => sa.email }
}

# ---------------------------------------------------------------------------
# Local-dev DB access. The instance has no public IP, so use the Cloud SQL
# Auth Proxy locally — it tunnels through GCP APIs using your gcloud auth.
# ---------------------------------------------------------------------------

output "db_app_user" {
  description = "Postgres user name used by both apps and local-dev connections."
  value       = google_sql_user.app.name
}

output "db_app_user_password" {
  description = "Postgres password for the app user. Same value used in Secret Manager connection strings."
  value       = random_password.db_app_user.result
  sensitive   = true
}

output "documents_bucket" {
  description = "Name of the GCS bucket where user-uploaded documents are stored."
  value       = google_storage_bucket.documents.name
}
