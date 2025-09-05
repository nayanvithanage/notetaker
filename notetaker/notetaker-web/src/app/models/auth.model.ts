export interface User {
  id: number;
  email: string;
  name: string;
  pictureUrl?: string;
  picture?: string; // For Google OAuth compatibility
  googleId?: string;
  authProvider: string;
  createdAt: string;
}

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface SocialAccount {
  id: number;
  platform: string;
  accountId: string;
  displayName: string;
  pages: SocialPage[];
  selectedPageId?: string;
  createdAt: string;
}

export interface SocialPage {
  id: string;
  name: string;
  category?: string;
}
