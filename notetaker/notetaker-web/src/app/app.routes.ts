import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { AuthCallbackComponent } from './pages/auth-callback/auth-callback.component';
import { MainLayoutComponent } from './components/main-layout/main-layout.component';
import { MeetingsComponent } from './pages/meetings/meetings';
import { MeetingDetailComponent } from './pages/meeting-detail/meeting-detail';
import { AutomationsComponent } from './pages/automations/automations';
import { SettingsComponent } from './pages/settings/settings';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/meetings', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'auth/callback', component: AuthCallbackComponent },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      { path: 'meetings', component: MeetingsComponent },
      { path: 'meetings/:id', component: MeetingDetailComponent },
      { path: 'automations', component: AutomationsComponent },
      { path: 'settings', component: SettingsComponent }
    ]
  },
  { path: '**', redirectTo: '/meetings' }
];