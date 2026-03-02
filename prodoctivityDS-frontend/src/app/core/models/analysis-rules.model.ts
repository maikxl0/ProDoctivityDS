import { Criterion } from './criterion.model';
import { NormalizationOptions } from './normalization-options.model';

export interface AnalysisRules {
  criterion1: Criterion;
  criterion2: Criterion;
  normalization: NormalizationOptions;
  evaluationLogic: string; // "Or" por ahora
}