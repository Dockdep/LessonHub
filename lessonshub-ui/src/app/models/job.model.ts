// Mirrors LessonsHub.Domain.Entities.JobStatus (int values)
export enum JobStatus {
  Pending = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
}

// Mirrors LessonsHub.Application.Models.Jobs.JobEvent — pushed by the hub
// each time a Job transitions. `result` is the JSON-serialized executor output;
// callers unwrap it to whatever DTO they expected for that job type.
export interface JobEvent {
  id: string;
  type: string;
  status: JobStatus;
  result: string | null;
  error: string | null;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  timestamp: string;
}

// Mirrors JobAcceptedResponse (202 body).
export interface JobAcceptedResponse {
  jobId: string;
}

// JobDto — for HTTP polling fallback (GET /api/jobs/{id}).
export interface JobDto {
  id: string;
  type: string;
  status: JobStatus;
  result: string | null;
  error: string | null;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}
