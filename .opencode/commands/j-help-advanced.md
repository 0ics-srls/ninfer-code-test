---
description: >-
  Advanced help with visual diagrams (junior workflow). Opens an interactive
  HTML page with Mermaid flow diagrams in the browser. Use when the user wants
  to understand how commands connect to each other.
---
## Action

Open the pre-built visual guide in the browser:

```bash
# Find the HTML file (installed via setup.sh into project root)
HTML_FILE=""
if [ -f ".opencode/support/j-help-advanced.html" ]; then
    HTML_FILE=".opencode/support/j-help-advanced.html"
fi

if [ -z "$HTML_FILE" ]; then
    echo "File not found. Check .opencode/support/j-help-advanced.html"
    exit 1
fi

# Open in browser (WSL → Mac → Linux fallback)
explorer.exe "$(wslpath -w "$(realpath "$HTML_FILE")")" 2>/dev/null \
  || open "$HTML_FILE" 2>/dev/null \
  || xdg-open "$HTML_FILE" 2>/dev/null \
  || echo "Open this file in your browser: $HTML_FILE"
```

Then say:
```
Visual guide opened in your browser!

If it didn't open, open this file manually:
  .opencode/support/j-help-advanced.html

For the text-only command list, use j-help.
```
