---
description: >-
  Debug Protocol for E2E Testing - Multi-layer debugging (frontend + backend +
  DB)
---
# Debug Protocol - E2E Testing Extension

Extension of the base Debug Protocol for End-to-End testing scenarios where multiple layers interact (frontend, backend, database).

$include: ./debug-protocol.md

---

## E2E Context

### What Makes E2E Different

In E2E testing, you're debugging a **distributed system**:

```
[Browser/UI] → [API/Backend] → [Database]
     ↓              ↓              ↓
  Console       App Logs       Query Logs
```

**Challenges**:
- Error may APPEAR in one layer but ORIGINATE in another
- Logs are distributed across multiple systems
- Timing and async behavior add complexity
- State spans multiple components

### The Multi-Layer Question

When debugging E2E, always ask: **"Which layer owns this problem?"**

| Symptom | Likely Layer | Where to Look |
|---------|--------------|---------------|
| UI doesn't update | Frontend | Browser console, component state |
| API returns error | Backend | Application logs, request/response |
| Data wrong/missing | Database | Query logs, data state |
| Network error | Infrastructure | Network tab, CORS, connectivity |
| Timeout | Any | All logs, check each layer's response time |

---

## RIDHV in E2E Context

### READ - Multi-Source

In E2E, "read the error" means checking ALL sources:

**Frontend Sources**:
- Browser console (errors, warnings)
- Network tab (failed requests, response bodies)
- Component state/props (React DevTools, Vue DevTools, etc.)

**Backend Sources**:
- Application logs (primary source)
- Request/response logs
- Exception logs with stack traces

**Database Sources**:
- Query logs (if enabled)
- Constraint violation messages
- Connection errors

**Protocol**:
```
1. Check browser console FIRST (closest to user)
2. Check network tab for failed requests
3. Check backend logs for exceptions
4. Check database logs if data-related
```

**Output Required**:
```
**FRONTEND ERROR**: [browser console message or "none"]
**NETWORK ERROR**: [failed request details or "none"]
**BACKEND ERROR**: [log entry or "none"]
**DB ERROR**: [query error or "none"]
```

---

### ISOLATE - Layer Identification

**Critical Question**: "Which layer ORIGINATES the problem?"

**Technique - Follow the Request**:
```
1. UI action triggered? → Check browser
2. Request sent? → Check network tab
3. Request received? → Check backend entry log
4. Logic executed? → Check backend processing logs
5. DB queried? → Check query logs
6. Response sent? → Check backend exit log
7. Response received? → Check network tab
8. UI updated? → Check browser console/state
```

**Find the Break Point**:
- Where does the flow STOP working?
- Last successful step vs first failing step
- That boundary is where you focus

**Output Required**:
```
**FAILING LAYER**: [frontend/backend/database/network]
**LAST SUCCESS**: [last step that worked]
**FIRST FAILURE**: [first step that failed]
**ORIGIN**: [file:line in the originating layer]
```

---

### DOCS - Framework-Specific

In E2E, you deal with multiple frameworks. Check docs for:

**Frontend**:
- Angular/React/Vue error patterns
- State management (NgRx, Redux, etc.)
- HTTP client behavior

**Backend**:
- Framework error handling (.NET, Spring, Express, etc.)
- ORM behavior (EF Core, Hibernate, etc.)
- Authentication/Authorization flows

**Infrastructure**:
- CORS configuration
- Proxy/gateway behavior
- Container/deployment issues

---

### HYPOTHESIZE - Cross-Layer Thinking

E2E hypotheses often involve multiple layers:

**Pattern**: "The error appears in [Layer A] because [Layer B] did/didn't [action]"

**Examples**:
- "UI shows 'undefined' because API returned null instead of empty array"
- "API returns 500 because DB connection string is wrong in this environment"
- "Login fails because CORS blocks the preflight request"

**Output Required**:
```
**HYPOTHESIS**: [cross-layer cause]
**LAYER CHAIN**: [UI] ← [API] ← [DB] (show causation)
**REASONING**: [why this layer affects that layer]
**PREDICTION**: [what will change in EACH layer after fix]
```

---

### VERIFY - Full Stack Check

After fixing, verify ALL layers:

```
1. Fix applied in [layer]
2. Verify [layer] behavior changed
3. Verify downstream layers receive correct input
4. Verify upstream layers show correct output
5. Run full E2E scenario end-to-end
```

**Output Required**:
```
**CHANGE**: [what changed, in which layer]
**BACKEND RESULT**: [logs show...]
**FRONTEND RESULT**: [UI shows...]
**E2E RESULT**: [full flow works/fails]
**PREDICTION MATCH**: [yes/no + explanation]
```

---

## Pre-Debug Checklist

Before starting E2E debugging, verify:

### Services Running
- [ ] Database running and accessible
- [ ] Backend service running
- [ ] Frontend service running (dev server or built)
- [ ] All services can communicate (network, ports)

### Logs Accessible
- [ ] Know where backend logs are written
- [ ] Know the log format (timestamp, level, message)
- [ ] Can tail/search logs in real-time
- [ ] Browser DevTools open and ready

### Tools Connected
- [ ] Browser DevTools open (Console, Network tabs)
- [ ] chrome-devtools MCP connected (if using)
- [ ] Database client available (if needed)

### Environment Verified
- [ ] Correct environment (dev/staging/prod)
- [ ] Environment variables set correctly
- [ ] API URLs pointing to right services
- [ ] Authentication/tokens valid

---

## Logging Discovery

### Find Backend Logs

**.NET**:
```bash
grep -r "WriteTo.File\|AddSerilog\|LoggerFactory" --include="*.cs"
grep -r "Logging" --include="appsettings*.json"
```

**Node.js**:
```bash
grep -r "winston\|pino\|createLogger" --include="*.js" --include="*.ts"
```

**Java**:
```bash
grep -r "LoggerFactory\|@Slf4j" --include="*.java"
grep -r "logging" --include="application*.yml" --include="application*.properties"
```

### Common Log Locations
```
./logs/
/var/log/[app-name]/
~/logs/
stdout (containerized apps)
```

### Browser Logs
```
Console tab → Errors/Warnings
Network tab → Failed requests (red)
Application tab → Storage, cookies, tokens
```

---

## Common E2E Patterns

### 404 Not Found

**Symptoms**: Network tab shows 404, UI shows "not found" or fails silently

**Diagnose**:
```bash
# Check route exists
grep -r "Route\|HttpGet\|HttpPost\|@GetMapping" --include="*.cs" --include="*.java"
# Check URL matches
grep -r "/api/[endpoint]" --include="*.ts" --include="*.js"
```

**Common Causes**:
- Route attribute missing/wrong
- Wrong HTTP verb (GET vs POST)
- URL path mismatch (case sensitivity, trailing slash)
- Controller not registered

---

### 500 Internal Server Error

**Symptoms**: Network returns 500, UI shows generic error

**Diagnose**:
```bash
# Check backend logs for exception
grep -i "exception\|error\|fail" [logfile] | tail -50
```

**Common Causes**:
- Null reference exception
- Database connection failed
- Dependency injection not configured
- Unhandled exception in business logic

---

### CORS Errors

**Symptoms**: Browser console shows CORS error, request blocked

**Diagnose**:
```bash
# Check CORS config
grep -r "Cors\|AllowOrigin\|cors" --include="*.cs" --include="*.ts" --include="*.js"
```

**Common Causes**:
- CORS not enabled
- Origin not in allowed list
- Preflight (OPTIONS) not handled
- Credentials mode mismatch

---

### Authentication Failures

**Symptoms**: 401/403 errors, redirect to login

**Diagnose**:
```bash
# Check token handling
grep -r "Authorization\|Bearer\|JWT" --include="*.ts" --include="*.cs"
# Check backend logs for auth failures
grep -i "auth\|token\|unauthorized" [logfile] | tail -30
```

**Common Causes**:
- Token expired/invalid
- Token not sent in header
- Wrong authentication scheme
- Missing role/claim

---

### Database Errors

**Symptoms**: 500 error with DB message, data not saving

**Diagnose**:
```bash
# Check connection string
grep -r "ConnectionString\|DataSource" --include="*.json" --include="*.cs"
# Check DB logs or backend logs for query errors
grep -i "sql\|database\|entity\|constraint" [logfile] | tail -30
```

**Common Causes**:
- Connection string wrong for environment
- Database not running
- Migration not applied
- Constraint violation (unique, foreign key)

---

### SignalR/WebSocket Failures

**Symptoms**: Real-time features don't work, connection errors

**Diagnose**:
```bash
# Check hub registration
grep -r "MapHub\|AddSignalR" --include="*.cs"
# Check client connection
grep -r "HubConnection\|signalR" --include="*.ts"
```

**Common Causes**:
- Hub not mapped
- URL mismatch
- CORS not configured for SignalR
- Transport fallback issues

---

## E2E Debug Workflow

```
┌─────────────────────────────────────────────────────────┐
│ 1. REPRODUCE                                            │
│    Run the failing E2E scenario                         │
│    Note exactly what fails and when                     │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 2. READ (all sources)                                   │
│    Browser console → Network tab → Backend logs → DB    │
│    Quote exact errors from each layer                   │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 3. ISOLATE (find the layer)                             │
│    Follow the request through each layer                │
│    Find where it breaks: UI? API? DB?                   │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 4. DOCS (if framework/library error)                    │
│    Check docs for the specific framework in that layer  │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 5. HYPOTHESIZE                                          │
│    "Error in [LayerA] because [LayerB] does [X]"        │
│    Predict what will change in each layer after fix    │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│ 6. VERIFY (full stack)                                  │
│    Make ONE change                                      │
│    Check backend logs + frontend behavior               │
│    Run full E2E scenario                                │
│    If wrong → back to READ with new information         │
└─────────────────────────────────────────────────────────┘
```

---

## Rules for E2E Debugging

### DO
- ✅ Check ALL log sources before forming hypothesis
- ✅ Identify which layer owns the problem
- ✅ Verify services are running before debugging code
- ✅ Use browser DevTools actively
- ✅ Trace requests through entire stack
- ✅ Verify fix in all affected layers

### DON'T
- ❌ Assume the problem is where the error shows
- ❌ Debug code without checking logs first
- ❌ Ignore network tab information
- ❌ Fix frontend when problem is backend (or vice versa)
- ❌ Skip the pre-debug checklist
- ❌ Declare fixed without full E2E verification

---

## Remember

```
E2E = Multiple Layers = Multiple Log Sources

ALWAYS ask: "Which layer OWNS this problem?"

Browser Console → Network Tab → Backend Logs → Database

Trace the request. Find the break point. Fix the right layer.
```
