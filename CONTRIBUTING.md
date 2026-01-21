# Contributing to TempCleaner

Thank you for your interest in contributing! This document explains the preferred workflow, coding standards, and expectations for contributions.

1. How to contribute

- Report bugs by opening an issue with steps to reproduce, expected vs actual behavior, and screenshots/logs if applicable.
- Propose features by opening an issue tagged `enhancement` describing the use case and a suggested approach.
- For code contributions, open a pull request (PR) from a branch off `main` named using the pattern `feat/<short-desc>` or `fix/<short-desc>`.

2. Development setup
1. Clone the repository and switch to `main`.
1. Create a feature branch:

```powershell
git checkout -b feat/your-feature
```

3. Recommended local steps

- Restore and build: `dotnet restore && dotnet build`
- Use `dotnet format` or your editor's formatting to keep styles consistent.

3. Coding standards

- Language: C# targeting .NET 8.0.
- Follow idiomatic C#: PascalCase for public types/members, camelCase for private fields (or `_camelCase` if used in the repo).
- Keep methods small and single-responsibility.
- Add XML documentation for public surface area where appropriate.
- Avoid breaking public APIs without prior discussion.

4. Pull request process

- Open a PR against `main` with a clear description of the change and the problem it solves.
- Include before/after behaviour and screenshots if the UI changes.
- Keep PRs focused and small when possible.
- Link related issues in the PR description.
- At least one maintainer review is required before merge.

5. Tests and validation

- Add unit tests for new logic where feasible. There is no test project currently; consider adding a `tests/` project for new code.
- Run a full build locally and validate the application starts.

6. Commit messages

- Use concise, descriptive commit messages. Consider the Conventional Commits style: `feat:`, `fix:`, `docs:`, `chore:`.

7. Code of conduct & communication

- Use the repository issues and PRs for discussion. If real-time chat is desired, add contact instructions to the README.

8. Release and packaging

- Releases are recommended to follow semantic versioning when publishing binaries.
- Packaging uses the `Installer/` WiX sources; coordinate with maintainers before changing installer behavior.

9. Maintainers

- Repository maintainers review PRs and manage releases. If you're interested in becoming a maintainer, start by contributing high-quality PRs and participating in reviews.

Thank you for improving TempCleaner — contributions and clear issues are highly appreciated.
