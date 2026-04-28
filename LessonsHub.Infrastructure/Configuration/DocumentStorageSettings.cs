namespace LessonsHub.Infrastructure.Configuration;

public class DocumentStorageSettings
{
    /// <summary>"Local" or "Gcs". Picks which IDocumentStorage implementation runs.</summary>
    public string Strategy { get; set; } = "Local";

    /// <summary>Base directory for the Local strategy. Mounted as a shared volume in docker-compose.</summary>
    public string LocalBasePath { get; set; } = "/app/uploads";

    /// <summary>GCS bucket name for the Gcs strategy (created by Terraform).</summary>
    public string GcsBucket { get; set; } = string.Empty;
}
