---
paths:
  - "src/Design/**/*"
  - "docs/**/*.md"
  - "README.md"
  - "skills/RemoteFactory/**/*"
---

- `src/Design/` (Design.Domain, Design.Tests, Design.Server, Design.Client.Blazor) is the single source of every C# snippet in `docs/`, `README.md` and `skills/RemoteFactory/` (`docs/VisionAlignment.md` R-A). `src/docs/reference-app` and `src/docs/samples` are being retired (todo EXSET); add nothing to them.
- Snippets are `#region name` ... `#endregion` blocks; keep a region tight around the code it shows. Every snippet's behaviour is pinned by a Design test on the real client/server containers.
- Region names are globally unique across the repository, because mdsnippets scans all of it: never a bare word (`src/Tests` and `src/Generator` already use `Fetch`, `Remote`, ...). Keep the topic prefixes (`operations-*`, `save-*`, `skill-*`, ...). A region migrating from the reference app keeps its name, and its old definition is deleted in the same change, or the page renders both copies.
- Code follows the doctrine in `docs/VisionAlignment.md` sections 0 and 2 and compiles. LazyLoad is excluded (R-K).
- Never hand-write C# in a markdown file; a short block labelled WRONG that shows a forbidden pattern is the only exception.
- Placeholders are written as a pair — the opening `snippet:` comment and its `endSnippet` comment — never the opening marker alone (see `CLAUDE.md`).
- After a change: build and test **both** `src/Neatoo.RemoteFactory.sln` and `src/Design/Design.sln` with output to files; run `mdsnippets` with its log outside the repository; check `git diff --stat` for a file whose deletions exceed the edit, and for duplicate or missing snippets.
