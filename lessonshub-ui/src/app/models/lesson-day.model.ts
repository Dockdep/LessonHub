export interface LessonDay {
  id?: number;
  date: string;
  name: string;
  shortDescription: string;
  lessons: AssignedLesson[];
}

export interface AssignedLesson {
  id: number;
  lessonNumber: number;
  name: string;
  shortDescription: string;
  lessonPlanId: number;
  lessonPlanName: string;
  isCompleted: boolean;
}

export interface LessonPlanSummary {
  id: number;
  name: string;
  topic: string;
  description: string;
  createdDate: string;
  lessonsCount: number;
  isOwner: boolean;
  ownerName?: string;
}

export interface AvailableLesson {
  id: number;
  lessonNumber: number;
  name: string;
  shortDescription: string;
  lessonPlanId: number;
  lessonPlanName: string;
  isAssigned: boolean;
}

export interface AssignLessonRequest {
  lessonId: number;
  date: string;
  dayName: string;
  dayDescription: string;
}

export interface LessonPlanDetail {
  id: number;
  name: string;
  topic: string;
  description: string;
  nativeLanguage?: string;
  /** Language lessons only — target language being studied. */
  languageToLearn?: string;
  /** Language lessons only — when true, lesson is rendered in native; when false, in target. */
  useNativeLanguage?: boolean;
  createdDate: string;
  lessons: PlanLesson[];
  isOwner: boolean;
  ownerName?: string;
}

export interface LessonPlanShareItem {
  id: number;
  userId: number;
  email: string;
  name: string;
  sharedAt: string;
}

export interface AddShareRequest {
  email: string;
}

export interface PlanLesson {
  id: number;
  lessonNumber: number;
  name: string;
  shortDescription: string;
  lessonTopic: string;
  isCompleted: boolean;
}

export interface UpdateLessonPlanRequest {
  name: string;
  topic: string;
  description: string;
  nativeLanguage?: string;
  /** Language lessons only — target language being studied. */
  languageToLearn?: string;
  /** Language lessons only — when true, lesson is rendered in native; when false, in target. */
  useNativeLanguage?: boolean;
  lessons: UpdateLessonRequest[];
}

export interface UpdateLessonRequest {
  id?: number;
  lessonNumber: number;
  name: string;
  shortDescription: string;
  lessonTopic: string;
  keyPoints: string[];
}
