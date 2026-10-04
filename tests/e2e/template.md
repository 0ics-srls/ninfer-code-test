# E2E Test Suite - my-app

> SUPERSEDED 2026-09-04 by web/e2e (Playwright) — kept as manual reference.

## Test Run Info
- **Date:** 2026-06-23
- **Tester:** Claude Code (Autonomous)
- **Frontend URL:** http://localhost:4200
- **Backend URL:** http://localhost:5000
- **Auth method:** none
- **Database:** SQLite (local file)

## Legend
- [x] = PASS
- [!] = FAIL (with notes)
- [-] = SKIP (with reason)

---

## 0. PREREQUISITES

### 0.1 Backend
- [ ] Backend running on http://localhost:5000
- [ ] GET /health returns 200 with status "healthy"
- [ ] No startup errors

### 0.2 Frontend
- [ ] Frontend running on http://localhost:4200
- [ ] Angular app loads without errors
- [ ] No browser console errors on initial load
- [ ] PrimeNG styles loaded

### 0.3 Database
- [ ] SQLite database file exists
- [ ] Seed data present (at least 2 todos)

---

## 1. BACKEND API ENDPOINTS

### 1.1 Health & Version
- [ ] GET /health → 200, body has "healthy"
- [ ] GET /version → 200, body has version "1.0.N"

### 1.2 Todos GET
- [ ] GET /api/Todos → 200 with array
- [ ] Each item has id, title, status, createdAt (camelCase)
- [ ] Status is string ("Completed"/"Pending") not number

### 1.3 Todos POST (Create)
- [ ] POST /api/Todos with valid body → 201 + TodoDto with Id
- [ ] POST /api/Todos with empty title → 400

### 1.4 Todos PUT (Update)
- [ ] PUT /api/Todos/{id} with valid body → 200 + updated TodoDto
- [ ] PUT /api/Todos/999 → 404

### 1.5 Todos DELETE
- [ ] DELETE /api/Todos/{id} → 204
- [ ] DELETE /api/Todos/999 → 404

### 1.6 OpenAPI & Scalar
- [ ] GET /openapi/v1.json → 200, contains /api/Todos
- [ ] GET /scalar/v1 → 200

---

## 2. DARK MODE TOGGLE

### 2.1 Toggle Button
- [ ] Toggle button visible in header (sun/moon icon)
- [ ] No console errors

### 2.2 Theme Switching
- [ ] Click toggle → document gets 'app-dark' class
- [ ] Page appearance changes (dark theme)
- [ ] Click again → 'app-dark' class removed (light theme)
- [ ] Preference persists in localStorage after reload
- [ ] No console errors during toggle

---

## 3. 2 CARDS LAYOUT + STATS

### 3.1 Layout
- [ ] Page shows 2 p-card elements
- [ ] Card 1 header: "Stats"
- [ ] Card 2 header: "Todos"

### 3.2 Stats Card
- [ ] Total count matches number of todos (DOM verified)
- [ ] Completed count matches todos with status "Completed"
- [ ] Pending count matches todos without "Completed" status

---

## 4. TODO CRUD — READ

### 4.1 Table Display
- [ ] Todos table renders inside Card 2
- [ ] Table headers: ID, Title, Status, Created At, Actions
- [ ] Table rows match backend data (DOM cell content verified)
- [ ] No empty cells

---

## 5. TODO CRUD — CREATE

### 5.1 New Button
- [ ] "New Todo" button visible above table
- [ ] Click → dialog opens with empty form

### 5.2 Create Dialog
- [ ] Dialog title: "New Todo"
- [ ] Title input present (empty)
- [ ] Status select present (default "Pending")
- [ ] Save button disabled when title empty
- [ ] No console errors

### 5.3 Create Submit
- [ ] Fill title + select status → Save enabled
- [ ] Click Save → dialog closes
- [ ] New todo appears in table (DOM verified)
- [ ] Stats counts update

---

## 6. TODO CRUD — UPDATE

### 6.1 Edit Button
- [ ] Each row has edit button (pencil icon)
- [ ] Click edit → dialog opens with pre-filled form

### 6.2 Edit Dialog
- [ ] Dialog title: "Edit Todo"
- [ ] Title input shows current todo title
- [ ] Status select shows current todo status

### 6.3 Update Submit
- [ ] Change title/status → Save
- [ ] Dialog closes
- [ ] Table row shows updated values (DOM verified)
- [ ] Stats counts update if status changed

---

## 7. TODO CRUD — DELETE

### 7.1 Delete Button
- [ ] Each row has delete button (trash icon, red)
- [ ] Click delete → confirmation dialog appears

### 7.2 Confirm Delete
- [ ] Confirm dialog shows todo title
- [ ] Click accept → todo removed from table
- [ ] Stats counts update
- [ ] No console errors

---

## 8. BUILD VERIFICATION

### 8.1 Backend
- [ ] dotnet build → zero warnings
- [ ] Core tests: all pass
- [ ] Server tests: all pass

### 8.2 Frontend
- [ ] npm run build → zero errors
- [ ] npm run gen-api → client regenerates without error
