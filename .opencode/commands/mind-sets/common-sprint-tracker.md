---
description: >-
  Sprint status tracker — YAML single source of truth for task states,
  auto-discovery of next work
---
# Sprint Status Tracker

A YAML file that tracks granular per-task state. Workflows DISCOVER next work from it automatically.

## When to Use
- Projects with 3+ tasks planned
- When using j-develop with TDDAB or TDD methodology
- When you want to resume work across sessions without re-reading everything

## File Location
Create `sprint-status.yaml` in the task folder (`{@tasks}/NN-feature-name/`).

## Template

```yaml
# Sprint Status — Single Source of Truth
# Updated automatically by j-develop and j-close

project: "{ProjectName}"
feature: "NN-feature-name"
branch: "feature/NN-feature-name"
last_updated: "YYYY-MM-DD"

tasks:
  task-1-name:
    status: "done"          # backlog | ready | in-progress | review | done
    complexity: 3            # 1-10 from complexity scoring
    subtasks: 2
    completed: "2026-04-06"

  task-2-name:
    status: "in-progress"
    complexity: 7
    subtasks: 5
    current_subtask: 3       # which subtask is being worked on
    
  task-3-name:
    status: "ready"          # dependencies met, ready to start
    complexity: 5
    subtasks: 3
    depends_on: ["task-1-name"]

  task-4-name:
    status: "backlog"        # not yet ready (dependencies not met)
    complexity: 8
    subtasks: 0              # not expanded yet
    depends_on: ["task-2-name", "task-3-name"]
```

## Status State Machine

```
backlog → ready → in-progress → review → done
                       ↓
                    blocked (if dependency fails)
```

- **backlog**: Not yet ready (dependencies not met or not expanded)
- **ready**: All dependencies met, can be started
- **in-progress**: Currently being worked on
- **review**: Implementation complete, under review
- **done**: Fully complete, DoD gates passed
- **blocked**: Was in-progress but encountered a blocker

## Auto-Discovery Rules

When j-develop starts:
1. Read `sprint-status.yaml`
2. Find FIRST task where `status == "ready"` or `status == "in-progress"`
3. If `in-progress` found → resume that task
4. If only `ready` found → start that task, set to `in-progress`
5. If all `done` → report "All tasks complete"

When a task completes:
1. Set task `status: "done"` + `completed: today`
2. Check all tasks that `depends_on` this task
3. If ALL their dependencies are `done` → set those to `status: "ready"`
4. Auto-discover next ready task

## Integration with j-* Commands

- **j-new-feature** creates `sprint-status.yaml` during PLAN phase (after complexity scoring)
- **j-develop** reads it at start, updates during work, writes on completion
- **j-status** reads it to show progress overview
- **j-close** verifies ALL tasks are `done` before allowing merge
- **j-continue** reads it to find where to resume
