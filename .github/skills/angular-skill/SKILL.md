---
applyTo: "src/app/**/*.ts,src/app/**/*.html,src/app/**/*.scss"
---

# Angular Rules — SkyRoute

Stack: Angular 20 · Standalone Components · Angular Signals · Reactive Forms · TypeScript Strict

## Non-Negotiables

- Standalone components only — every component must have `standalone: true`. NgModules are strictly prohibited.
- Use `inject()` for all dependencies at the field level — never constructor injection in components.
- Use Angular Signals (`signal()`, `computed()`, `effect()`) for all reactive state — never NgRx or any third-party store.
- Use `input()` and `output()` (signal-based) for component I/O — never `@Input()` / `@Output()` decorators.
- Use Reactive Forms for all user input — never template-driven forms (`ngModel`, `FormsModule`).
- Use `provideHttpClient(withInterceptors([...]))` — never `HttpClientModule`.
- Use lazy loading (`loadComponent`) for all feature routes.
- Never use `any` type anywhere.

## Signal Patterns

```typescript
// Writable — private, mutated only by this component/service
private _results = signal<FlightResult[]>([]);

// Read-only — exposed to template and children
readonly results = this._results.asReadonly();

// Computed — derived, no side effects, never triggers HTTP
readonly sortedResults = computed(() => sortFlights(this._results(), this.sortKey()));

// Effect — side effects only (e.g. updating validators on signal change)
effect(() => {
  const type = this.documentType();
  const control = this.form.get('documentNumber');
  control?.setValidators([Validators.required, documentNumberValidator(type)]);
  control?.updateValueAndValidity();
});
```

- Use `signal()` when: the value is set by user action or API response.
- Use `computed()` when: the value is always derived from other signals. Never make HTTP calls inside `computed()`.
- Use `effect()` when: a signal change must trigger a side effect (validator update, logging). Not for value transformations.

## Sorting Rule

Sorting search results must always be a `computed()` over the in-memory array. It must never trigger an HTTP request. Verify in DevTools Network tab that no request fires on sort change.

## Document Field Rule

The passenger form document label and validator are driven entirely by an `isInternational` computed signal that compares origin/destination `countryCode` values. Never compare airport names or city names. Never hardcode the label or validator per route.

## HTTP & Error Handling

- All HTTP calls go through `FlightService` / `BookingService` — never raw `HttpClient` calls in components.
- Errors are handled by a single `ErrorInterceptor` — not per-component try/catch.
- Always pipe `finalize(() => this.isLoading.set(false))` to reset loading state on both success and error.

## Route Guards

Guards must block direct navigation to `/results`, `/booking/:id`, and `/confirm` without the required upstream state. Use inline functional guards reading from the relevant service signal:

```typescript
canActivate: [() => inject(FlightStateService).selectedFlight() !== null]
```

## Component Setup Pattern

```typescript
@Component({
  selector: 'app-flight-card',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './flight-card.component.html',
})
export class FlightCardComponent {
  flight = input.required<FlightResult>();
  select = output<FlightResult>();

  private router = inject(Router);

  formattedDuration = computed(() => {
    const mins = this.flight().durationMinutes;
    return `${Math.floor(mins / 60)}h ${mins % 60}m`;
  });

  onSelect(): void {
    this.select.emit(this.flight());
  }
}
```

## Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Component class | `{Name}Component` | `SearchFormComponent` |
| Component selector | `app-{kebab-name}` | `app-search-form` |
| Service | `{Name}Service` | `FlightService` |
| Interceptor | `{name}Interceptor` (functional) | `errorInterceptor` |
| Interface | PascalCase, no `I` prefix | `FlightResult` |
| Signal (writable) | camelCase noun | `searchResults`, `isLoading` |
| Signal (computed) | camelCase derived noun | `sortedResults`, `documentLabel` |
| Model file | `{name}.model.ts` | `flight.model.ts` |
| Validator function | `{name}Validator` | `documentNumberValidator` |

## Feature Folder Structure

```
features/
  search/
    search-form/
    flight-results/
    flight-card/
  booking/
    booking-detail/
    passenger-form/
    booking-confirm/
```

## Never

- No NgModules anywhere.
- No `@Input()` / `@Output()` decorators — signal-based only.
- No `any` type.
- No moment.js, date-fns, or lodash.
- No `HttpClientModule`.
- No HTTP calls triggered by sort change.
- No hardcoded document labels or validators per route — use `countryCode` comparison only.
