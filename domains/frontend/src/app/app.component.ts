import { Component } from "@angular/core";

@Component({
  selector: "app-root",
  styles: [
    `
      :host {
        display: block;
        min-height: 100%;
        min-width: 375px; // Magic UX number
        background: var(--watt-color-neutral-grey-100);
      }
    `,
  ],
  template: ` <router-outlet></router-outlet>`,
})
export class AppComponent {}
