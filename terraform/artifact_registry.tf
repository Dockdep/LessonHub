resource "google_artifact_registry_repository" "lessonshub" {
  location      = var.region
  repository_id = "lessonshub"
  format        = "DOCKER"
  description   = "Container images for LessonsHub services."

  depends_on = [google_project_service.apis]
}
