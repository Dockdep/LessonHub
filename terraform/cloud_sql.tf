resource "google_sql_database_instance" "main" {
  name             = "lessonshub-db"
  region           = var.region
  database_version = "POSTGRES_17"

  settings {
    tier = var.sql_tier

    # pgvector is preinstalled on Cloud SQL Postgres 15+ — no instance flag
    # needed. Enable per-database via `CREATE EXTENSION vector;` when ready.

    ip_configuration {
      # Public IP enabled at the infra level (Cloud SQL refuses to provision
      # without at least one connectivity option). The instance is still
      # effectively private:
      #   - No `authorized_networks` block → no IP allowlist → no client on
      #     the public internet can reach the Postgres listener directly.
      #   - Cloud Run reaches it through the Cloud SQL Auth Proxy
      #     (--add-cloudsql-instances flag), which authorizes via IAM.
      #   - Local laptop access uses cloud-sql-proxy with your gcloud
      #     credentials — same IAM check, no IP allowlist needed.
      # This is the standard pattern; switch to Private Service Connect only
      # if you have a real reason (VPC isolation, hybrid cloud, compliance).
      ipv4_enabled = true
    }

    backup_configuration {
      enabled                        = true
      start_time                     = "03:00"
      point_in_time_recovery_enabled = false # cheaper; flip on for prod
    }

    insights_config {
      query_insights_enabled = true
    }
  }

  # Prevents accidental terraform destroy from deleting the DB.
  # Set to false explicitly when you want to tear down.
  deletion_protection = true

  depends_on = [google_project_service.apis]
}

resource "google_sql_database" "lessonshub" {
  name     = "LessonsHub"
  instance = google_sql_database_instance.main.name
}

resource "google_sql_database" "lessonsai" {
  name     = "LessonsAi"
  instance = google_sql_database_instance.main.name
}

# Random password kept in state; state file is local + gitignored.
# Avoid special characters that need URL-encoding in connection strings.
resource "random_password" "db_app_user" {
  length  = 32
  special = false
}

resource "google_sql_user" "app" {
  name     = "lessonshub"
  instance = google_sql_database_instance.main.name
  password = random_password.db_app_user.result
}
