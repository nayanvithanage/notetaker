import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-test',
  template: `
    <div style="padding: 20px;">
      <h1>Test Page</h1>
      <p>If you can see this, Angular is working!</p>
      <button (click)="testClick()">Test Button</button>
      <p *ngIf="clicked">Button was clicked!</p>
    </div>
  `,
  imports: [CommonModule],
  standalone: true
})
export class TestComponent {
  clicked = false;

  testClick() {
    this.clicked = true;
    console.log('Button clicked!');
  }
}
