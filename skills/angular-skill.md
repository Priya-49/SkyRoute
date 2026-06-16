# Angular Skill
> Stack: Angular 20 · Standalone Components · Angular Signals · Reactive Forms · TypeScript Strict

---

## Purpose

Define coding standards, signal patterns, component conventions, and guardrails for all Angular implementation in the SkyRoute platform. Every generated Angular file must comply with this skill before it is considered complete.

---

## Best Practices

- Use standalone components exclusively — declare `standalone: true` on every component
- Use `inject()` function for all dependencies — not constructor injection in components
- Use Angular Signals (`signal()`, `computed()`, `effect()`) for all reactive state
- Use Reactive Forms for all user input — never template-driven forms
- Use `input()` and `output()` (signal-based) for component I/O — not `@Input()` / `@Output()`
- Use `provideHttpClient(withInterceptors([...]))` — never `HttpClientModule`
- Use lazy loading (`loadComponent`) for all feature routes
- Use Angular built-in pipes (`DatePipe`, `CurrencyPipe`) — never moment.js or date-fns
- Apply `readonly` to all interface properties that must not be mutated

---

## Architecture Patterns

### Signal State Pattern
```typescript
// Writable signals — private, mutated only by this component/service
private _results = signal<FlightResult[]>([]);

// Read-only signals — exposed to template and children
readonly results = this._results.asReadonly();

// Computed signals — derived, no side effects
readonly sortedResults = computed(() => sortFlights(this._results(), this.sortKey()));

// Effects — side effects triggered by signal changes
effect(() => {
  console.log('Sort changed:', this.sortKey());
});
```

### Feature-Based Folder Structure
```
features/
  search/
    search-form/
      search-form.component.ts
      search-form.component.html
      search-form.component.scss
    flight-results/
    flight-card/
  booking/
    booking-detail/
    passenger-form/
    booking-confirm/
```

### Functional HTTP Interceptor
```typescript
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const message = error.error?.detail ?? 'An unexpected error occurred.';
      return throwError(() => ({ status: error.status, message }));
    })
  );
};
```

### Route Guard (Signal-based)
```typescript
// Inline functional guard using FlightStateService signal
canActivate: [() => inject(FlightStateService).selectedFlight() !== null]
```

---

## Do Rules

- **Do** use `signal()` for all mutable component state
- **Do** use `computed()` for all derived values — sort order, document label, route type
- **Do** use `effect()` for side effects driven by signal changes (e.g. updating validators)
- **Do** use `input.required<T>()` for required component inputs
- **Do** use `output<T>()` for component event emitters
- **Do** use `inject()` at the field level — `private service = inject(MyService)`
- **Do** use `finalize()` in RxJS pipes to reset `isLoading` on both success and error
- **Do** use `DatePipe` for time formatting in templates
- **Do** use `readonly` on all TypeScript interface properties
- **Do** type all signals explicitly — `signal<FlightResult[]>([])`

---

## Don't Rules

- **Don't** use NgModules — strictly prohibited in this project
- **Don't** use NgRx or any third-party state management
- **Don't** use template-driven forms (`ngModel`, `FormsModule`)
- **Don't** use `@Input()` / `@Output()` decorators — use signal-based `input()` / `output()`
- **Don't** use `HttpClientModule` — use `provideHttpClient()`
- **Don't** use `any` type anywhere
- **Don't** trigger HTTP calls on sort change — sorting is always a `computed()` signal
- **Don't** send price values in `CreateBookingCommand` — backend recalculates
- **Don't** import moment.js, date-fns, or lodash
- **Don't** use constructor injection in components — use `inject()`
- **Don't** compare airport names or city names for route type — compare `countryCode` only

---

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
| Route file | `app.routes.ts` | — |
| Model file | `{name}.model.ts` | `flight.model.ts` |
| Validator function | `{name}Validator` | `documentNumberValidator` |

---

## Code Standards

### Component Setup
```typescript
@Component({
  selector: 'app-flight-card',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './flight-card.component.html',
})
export class FlightCardComponent {
  // Signal-based inputs
  flight = input.required<FlightResult>();

  // Signal-based outputs
  select = output<FlightResult>();

  // Injected services
  private router = inject(Router);

  // Computed signals
  formattedDuration = computed(() => {
    const mins = this.flight().durationMinutes;
    return `${Math.floor(mins / 60)}h ${mins % 60}m`;
  });

  onSelect(): void {
    this.select.emit(this.flight());
  }
}
```

### Dynamic Validator Update (effect pattern)
```typescript
constructor() {
  effect(() => {
    const type = this.documentType(); // signal input
    const control = this.form.get('documentNumber');
    control?.setValidators([Validators.required, documentNumberValidator(type)]);
    control?.updateValueAndValidity();
  });
}
```

### HTTP Service
```typescript
@Injectable({ providedIn: 'root' })
export class FlightService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiBaseUrl}/flights`;

  search(query: FlightSearchQuery): Observable<FlightSearchResponse> {
    return this.http.post<FlightSearchResponse>(`${this.apiUrl}/search`, query);
  }
}
```

### Loading State with finalize
```typescript
this.flightService.search(query).pipe(
  finalize(() => this.isLoading.set(false))
).subscribe({
  next: (response) => this.searchResults.set(response.results),
  error: (err) => this.errorMessage.set(err.message),
});
```

---

## Decision Tree

### When to use `signal()` vs `computed()`
- Use `signal()` when: the value is set by user action or API response
- Use `computed()` when: the value is always derived from one or more other signals
- Never use `computed()` to make HTTP calls — use `effect()` or subscribe in a method

### When to use `effect()`
- Use when: a signal change must trigger a side effect (updating a validator, logging)
- Do not use when: the value is just a transformation — use `computed()` instead

### When to extract a child component
- Extract when: a section of a template has its own inputs, outputs, or internal state
- Keep in parent when: the markup is display-only with no interactivity

### When to use a route guard
- Use when: a route requires state that may not exist (selected flight, booking confirmation)
- Guard pattern: inline functional guard reading from `FlightStateService` signal

---

## Validation Checklist
- [ ] `ng build` — zero errors, zero warnings
- [ ] No NgModule anywhere in the project
- [ ] No `any` type in any `.ts` file
- [ ] No `@Input()` / `@Output()` decorators — signal-based only
- [ ] Sorting does not trigger HTTP call (verified in browser DevTools Network tab)
- [ ] Document label updates reactively on route type change
- [ ] `CreateBookingCommand` has no price field
- [ ] All routes use `loadComponent` (lazy loading)
- [ ] Guards redirect correctly when state is missing

---

## Common Pitfalls

| Pitfall | Consequence | Fix |
|---|---|---|
| Triggering HTTP on sort | Unnecessary API calls, poor UX | Use `computed()` signal |
| Comparing city/airport names for route type | Wrong domestic/international detection | Compare `countryCode` only |
| Sending price in booking command | Security risk, bypasses server pricing | Remove price from command |
| Using `@Input()` instead of `input()` | Breaks signal reactivity | Use `input.required<T>()` |
| Forgetting `finalize()` | Loading spinner never stops on error | Always pipe `finalize()` |
| Missing `updateValueAndValidity()` | Validator change not reflected | Always call after `setValidators()` |
| Using `any` type | TypeScript safety lost | Define explicit interface |
| Importing NgModule | Conflicts with standalone architecture | Remove — use `imports: []` in component |
