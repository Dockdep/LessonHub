variable "project_id" {
  type        = string
  description = "GCP project ID."
}

variable "region" {
  type        = string
  default     = "us-central1"
  description = "GCP region for Cloud Run, Artifact Registry, Cloud SQL."
}

variable "github_repo" {
  type        = string
  description = "GitHub repository in 'owner/repo' format. Only this repository is allowed to impersonate sa-github-deploy via Workload Identity Federation."
}

variable "google_oauth_client_id" {
  type        = string
  sensitive   = true
  description = "Google OAuth 2.0 Client ID (from Google Cloud Console → APIs & Services → Credentials). The .NET service uses this to verify Google ID tokens at /api/auth/google."
}

variable "context7_api_key" {
  type        = string
  sensitive   = true
  description = "Context7 API key (ctx7sk-...). Server-wide, not per-user. Used by the Python AI service for documentation lookups."
}

variable "sql_tier" {
  type        = string
  default     = "db-f1-micro"
  description = "Cloud SQL machine tier. db-f1-micro is the cheapest (~$9/mo). Bump to db-g1-small or higher when load justifies."
}
