export interface LessonPlanRequest {
  lessonType: string;
  planName: string;
  numberOfDays: number | null;
  topic: string;
  description: string;
  /** Default/Technical: language for the lesson. Language: user's native language. */
  nativeLanguage?: string;
  /** Language lessons only — target language being studied. */
  languageToLearn?: string;
  /** Language lessons only — when true, lesson is rendered in nativeLanguage; when false, in languageToLearn (immersive). */
  useNativeLanguage?: boolean;
  bypassDocCache?: boolean;
  documentId?: number | null;
}

export const LESSON_TYPES = ['Technical', 'Language', 'Default'];

export interface GeneratedLesson {
  lessonNumber: number;
  name: string;
  shortDescription: string;
  topic: string;
  lessonTopic?: string;
  keyPoints?: string[];
}

export interface LessonPlanResponse {
  planName: string;
  topic: string;
  lessons: GeneratedLesson[];
}
