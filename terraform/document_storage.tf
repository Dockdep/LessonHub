# ---------------------------------------------------------------------------
# GCS bucket for user-uploaded documents (books, articles, notes).
# The .NET service writes here; the Python AI service reads from here.
# Bucket names are globally unique — we prefix with the project ID.
# ---------------------------------------------------------------------------

resource "google_storage_bucket" "documents" {
  name     = "${var.project_id}-documents"
  location = var.region

  # Enforce uniform IAM (no legacy ACLs). Cleaner permission model.
  uniform_bucket_level_access = true

  # No public access — every read/write goes through IAM.
  public_access_prevention = "enforced"

  # Same-region for low-latency reads from Cloud Run.
  storage_class = "STANDARD"

  # User uploads aren't versioned — when they delete a doc, the file is gone.
  versioning {
    enabled = false
  }

  # Don't accidentally nuke user content with a bare `terraform destroy`.
  # Set to false explicitly when you actually want to tear it down.
  force_destroy = false

  depends_on = [google_project_service.apis]
}

# .NET service writes uploads + deletes when users remove docs.
resource "google_storage_bucket_iam_member" "lessonshub_writes" {
  bucket = google_storage_bucket.documents.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.cloud_run["lessonshub"].email}"
}

# Python AI service reads uploads to chunk + embed at ingest time and
# read source content when generating Document-type lessons.
resource "google_storage_bucket_iam_member" "lessons_ai_reads" {
  bucket = google_storage_bucket.documents.name
  role   = "roles/storage.objectViewer"
  member = "serviceAccount:${google_service_account.cloud_run["lessons-ai-api"].email}"
}
