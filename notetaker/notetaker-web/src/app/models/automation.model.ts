export interface Automation {
  id: number;
  name: string;
  platform: string;
  description: string;
  exampleText?: string;
  enabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAutomation {
  name: string;
  platform: string;
  description: string;
  exampleText?: string;
  enabled: boolean;
}

export interface UpdateAutomation {
  name: string;
  platform: string;
  description: string;
  exampleText?: string;
  enabled: boolean;
}
