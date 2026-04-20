# Security Policy

## Supported Versions

The latest `main` branch and active `release/*` branches are supported with security updates.

## Reporting a Vulnerability

If you discover a security issue, please open a private security advisory in GitHub:

- https://github.com/UTM-Online/UTMO.Text.FileGenerator/security/advisories/new

Do not create a public issue for unpatched vulnerabilities.

## Dependency Security Practices

- Dependabot is configured for weekly NuGet and GitHub Actions updates.
- Pull requests run a NuGet vulnerability audit (`dotnet list package --vulnerable --include-transitive`).
- CodeQL scans run weekly and on pull requests.

## Local Dependency Audit

Run the following command from the repository root:

```powershell
./scripts/check-vulnerabilities.ps1
```

