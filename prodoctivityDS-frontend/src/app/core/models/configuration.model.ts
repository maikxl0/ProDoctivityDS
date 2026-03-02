import { ApiCredentials } from './api-credentials.model';
import { ProcessingOptions } from './processing-options.model';
import { AnalysisRules } from './analysis-rules.model';

export interface Configuration {
  baseUrl: string;
  apiKey: string;
  apiSecret: string;
  bearerToken: string;
  cookieSessionId: string;
  processingOptions: ProcessingOptions;
  analysisRules: AnalysisRules;
}

// Para guardar (el backend espera ApiCredentials aparte)
export interface SaveConfigurationRequest {
  apiCredentials: ApiCredentials;
  processingOptions: ProcessingOptions;
  analysisRules: AnalysisRules;
}