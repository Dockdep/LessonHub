# ---------------------------------------------------------------------------
# Secret Manager containers + initial versions. Cloud Run services receive
# the values via --set-secrets in the deploy workflow.
# ---------------------------------------------------------------------------

locals {
  secret_ids = [
    "db-url-lessonshub",
    "db-url-lessonsai",
    "jwt-secret",
    "google-oauth-client-id",
    "context7-api-key",
  ]
}

resource "google_secret_manager_secret" "secrets" {
  for_each = toset(local.secret_ids)

  secret_id = each.key

  replication {
    auto {}
  }

  depends_on = [google_project_service.apis]
}

# ---------------------------------------------------------------------------
# Auto-composed secret values (from random_password + Cloud SQL identifiers).
# ---------------------------------------------------------------------------

# .NET (Npgsql) connection string.
resource "google_secret_manager_secret_version" "db_url_lessonshub" {
  secret = google_secret_manager_secret.secrets["db-url-lessonshub"].id

  secret_data = "Host=/cloudsql/${var.project_id}:${var.region}:${google_sql_database_instance.main.name};Database=${google_sql_database.lessonshub.name};Username=${google_sql_user.app.name};Password=${random_password.db_app_user.result}"
}

# Python (asyncpg / SQLAlchemy) connection URL.
resource "google_secret_manager_secret_version" "db_url_lessonsai" {
  secret = google_secret_manager_secret.secrets["db-url-lessonsai"].id

  secret_data = "postgresql://${google_sql_user.app.name}:${random_password.db_app_user.result}@/${google_sql_database.lessonsai.name}?host=/cloudsql/${var.project_id}:${var.region}:${google_sql_database_instance.main.name}"
}

# JWT signing secret — random 64 chars.
resource "random_password" "jwt_secret" {
  length  = 64
  special = true
}

resource "google_secret_manager_secret_version" "jwt_secret" {
  secret      = google_secret_manager_secret.secrets["jwt-secret"].id
  secret_data = random_password.jwt_secret.result
}

# ---------------------------------------------------------------------------
# Externally-supplied secret values (you provide via terraform.tfvars).
# ---------------------------------------------------------------------------

resource "google_secret_manager_secret_version" "google_oauth_client_id" {
  secret      = google_secret_manager_secret.secrets["google-oauth-client-id"].id
  secret_data = var.google_oauth_client_id
}

resource "google_secret_manager_secret_version" "context7_api_key" {
  secret      = google_secret_manager_secret.secrets["context7-api-key"].id
  secret_data = var.context7_api_key
}
