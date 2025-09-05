export interface Automation {
  id: number;
  name: string;
  description: string;
  prompt: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAutomationRequest {
  name: string;
  description: string;
  prompt: string;
}

export interface UpdateAutomationRequest {
  name?: string;
  description?: string;
  prompt?: string;
  isActive?: boolean;
}