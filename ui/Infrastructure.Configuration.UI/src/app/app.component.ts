import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  template: `
    <div class="container">
      <header>
        <h1>Configuration Management</h1>
        <nav>
          <a routerLink="/configurations" routerLinkActive="active">Configurations</a>
          <a routerLink="/feature-flags" routerLinkActive="active">Feature Flags</a>
          <a routerLink="/tenants" routerLinkActive="active">Tenants</a>
        </nav>
      </header>
      <main>
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 20px;
    }
    header {
      border-bottom: 2px solid #007bff;
      padding-bottom: 10px;
      margin-bottom: 20px;
    }
    h1 {
      color: #007bff;
      margin: 0 0 10px 0;
    }
    nav {
      display: flex;
      gap: 20px;
    }
    nav a {
      text-decoration: none;
      color: #333;
      padding: 8px 16px;
      border-radius: 4px;
      transition: background-color 0.3s;
    }
    nav a:hover, nav a.active {
      background-color: #007bff;
      color: white;
    }
    main {
      margin-top: 20px;
    }
  `]
})
export class AppComponent {
  title = 'Configuration Management';
}

