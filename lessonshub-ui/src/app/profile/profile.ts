import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { UserProfileService } from '../services/user-profile.service';
import { NotificationService } from '../services/notification.service';

@Component({
  selector: 'app-profile',
  imports: [CommonModule, FormsModule, MatIconModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  private profileService = inject(UserProfileService);
  private notifications = inject(NotificationService);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly email = signal('');
  protected readonly name = signal('');
  protected googleApiKey = '';
  protected showKey = false;

  ngOnInit(): void {
    this.profileService.getProfile().subscribe({
      next: (profile) => {
        this.email.set(profile.email);
        this.name.set(profile.name);
        this.googleApiKey = profile.googleApiKey ?? '';
        this.loading.set(false);
      },
      error: () => {
        this.notifications.error('Failed to load profile.');
        this.loading.set(false);
      }
    });
  }

  save(): void {
    this.saving.set(true);
    const value = this.googleApiKey.trim();
    this.profileService.updateProfile({ googleApiKey: value === '' ? null : value }).subscribe({
      next: (profile) => {
        this.googleApiKey = profile.googleApiKey ?? '';
        this.saving.set(false);
        this.notifications.success('Profile updated.');
      },
      error: () => {
        this.saving.set(false);
        this.notifications.error('Failed to update profile.');
      }
    });
  }
}
