\# Security Policy



\## Supported Versions



Active development and security updates apply to the current `main` branch and active release/feature branches of \*\*SmartLib\*\*.



\## Reporting a Vulnerability



If you discover a security vulnerability in \*\*SmartLib\*\*, please do not open a public issue with exploit details.



Instead, report it privately to the project maintainers/team:

\- Contact the repository maintainers directly via your team communication channel or private message.

\- Include a clear description of the vulnerability, affected components/files, steps to reproduce, and potential impact.



We aim to acknowledge reports promptly, verify the issue, prepare a fix, and coordinate disclosure once resolved.



\## Security Practices in SmartLib



\- \*\*Database \& SQL Injection Prevention:\*\* Queries use Entity Framework Core / parameterized queries rather than raw string concatenation.

\- \*\*Authentication \& Password Storage:\*\* Passwords are stored using secure password hashing algorithms (`pbkdf2` / bcrypt `$2...` formats) rather than plain text.

\- \*\*Secrets \& Configuration:\*\* Avoid committing credentials, connection strings, API keys, or production secrets to version control.

