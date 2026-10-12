# AcxiomCRM — Enterprise Sales & Relationship Management System

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue.svg)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC%20%2B%20API-512BD4.svg)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQLite-blueviolet.svg)
![Tests](https://img.shields.io/badge/Tests-24%20Passed-success.svg)
![Swagger](https://img.shields.io/badge/API-Swagger%20%2F%20OpenAPI-85EA2D.svg)

AcxiomCRM is an ASP.NET Core MVC CRM prototype for managing customers, leads, sales opportunities, follow-ups, activities, reports, and user access in one application.

## Table of Contents

- [Overview](#overview)
- [Architecture and Technology Stack](#architecture-and-technology-stack)
- [Demo Accounts](#demo-accounts)
- [Role-Based Access Control](#role-based-access-control)
- [Core Workflows](#core-workflows)
- [Validation and Business Rules](#validation-and-business-rules)
- [Security](#security)
- [REST API](#rest-api)
- [Testing](#testing)
- [Quick Start](#quick-start)
- [GitHub Setup](#github-setup)

## Overview

The application supports these CRM workflows:

1. **Lead capture and qualification** — track leads and convert eligible leads into customers.
2. **Customer management** — maintain customer details and sales ownership.
3. **Opportunity pipeline** — track deal stages, amounts, probabilities, and expected close dates.
4. **Follow-ups and activities** — manage planned and overdue follow-ups and record calls, meetings, emails, and tasks.
5. **Dashboard and analytics** — display KPI cards and Chart.js visualizations.
6. **Audit history** — record selected user actions for authorized review.
7. **REST API** — expose supported CRM operations through documented API endpoints.

## Architecture and Technology Stack

- **Backend:** C#, .NET 8, ASP.NET Core MVC and Web API
- **Data access:** Entity Framework Core
- **Database:** SQLite
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Bootstrap 5, jQuery validation, Chart.js
- **API documentation:** Swagger / OpenAPI
- **Tests:** .NET test project (`AcxiomCRM.Tests`)

```text
AcxiomCRM/
├── AcxiomCRM.sln
├── Dockerfile
├── docker-compose.yml
├── README.md
├── src/
│   └── AcxiomCRM/
│       ├── Controllers/
│       │   └── Api/
│       ├── Data/
│       ├── DTOs/
│       ├── Models/
│       ├── Services/
│       ├── ViewModels/
│       ├── Views/
│       ├── wwwroot/
│       ├── AcxiomCRM.csproj
│       └── Program.cs
└── tests/
    └── AcxiomCRM.Tests/
```

## Demo Accounts

The project was supplied with the following development/demo credentials. They work only if the database seeding configuration still uses these values.

| Role | Email | Password |
|---|---|---|
| Admin | `admin@acxiomcrm.local` | `Admin@12345!` |
| Manager | `manager@acxiomcrm.local` | `Manager@12345!` |
| Sales Executive | `sales@acxiomcrm.local` | `Sales@12345!` |
| Sales Executive 2 | `jane.sales@acxiomcrm.local` | `Sales@12345!` |

**Security warning:** These are demo credentials, not production credentials. Change or remove them before deploying the application publicly. Never commit real secrets or personal data.

## Role-Based Access Control

The application is designed to provide role-based access for administrators, managers, and sales executives. Sales-executive data visibility is intended to be limited to assigned records; administrators have access to user administration. Verify the behavior by signing in with each demo role before relying on this table for a formal security claim.

| Area | Admin | Manager | Sales Executive |
|---|---|---|---|
| Dashboard and CRM records | Organization-wide | Team-level, as configured | Assigned records, as configured |
| Reports | Administrative access | Management reports | Depends on endpoint permissions |
| User and role administration | Yes | No | No |
| Audit logs | Authorized administrative access | Limited access, as configured | No access, as configured |

## Core Workflows

### Lead-to-customer conversion

The lead workflow supports status tracking and a conversion action. Conversion is intended to create a customer record with a generated customer code and may also create an opportunity, depending on the conversion inputs and implementation.

### Opportunity pipeline

The application tracks opportunity stages, amounts, probability, and expected close dates. The weighted pipeline formula is:

`Weighted Value = Amount × Probability / 100`

### Follow-ups and activities

Users can track planned, completed, and overdue follow-ups and record activities such as calls, meetings, emails, and tasks.

## Validation and Business Rules

The project is designed to validate important business inputs, including:

- Opportunity amount must be greater than zero.
- Opportunity probability must be between 0 and 100 inclusive.
- Active opportunities must not have an expected close date in the past.
- Planned follow-ups must not be scheduled before the current date.
- Customer email and phone values are validated, including uniqueness checks.
- Repeated failed sign-in attempts can trigger account lockout.

Exact validation messages and behavior should be confirmed against the current source code and tests.

## Security

Security-related features in the project include role-based authorization, ASP.NET Core Identity, anti-forgery protection for applicable MVC forms, and Entity Framework Core parameterized queries. Audit logging is available for selected actions.

This project has been tested locally; do not describe it as production-ready without a separate security review, deployment configuration, and secret-management review.

## REST API

Swagger/OpenAPI documentation is available at `/swagger` while the application is running in an environment where Swagger is enabled.

The project includes API controller groups for areas such as customers, leads, opportunities, follow-ups, reports, and authentication. Common routes in the current project include:

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/auth/login` | API login, if enabled/configured |
| `GET` | `/api/customers` | List or search customers |
| `GET` | `/api/customers/{id}` | Retrieve a customer |
| `POST` | `/api/customers` | Create a customer |
| `PUT` | `/api/customers/{id}` | Update a customer |
| `DELETE` | `/api/customers/{id}` | Delete a customer, if supported |
| `GET` | `/api/leads` | List leads |
| `POST` | `/api/leads` | Create a lead |
| `POST` | `/api/leads/{id}/convert` | Convert a lead, if enabled |
| `GET` | `/api/opportunities` | List opportunities |
| `POST` | `/api/opportunities` | Create an opportunity |
| `GET` | `/api/followups` | List follow-ups |
| `POST` | `/api/followups` | Create a follow-up |
| `GET` | `/api/reports/pipeline` | Retrieve pipeline report data |

Use the live Swagger page as the source of truth for the exact routes, request schemas, and supported HTTP methods.

## Testing

Run the automated test suite from the repository root:

```powershell
dotnet test
```

Most recent test result reported in the development session:

```text
Passed! - Failed: 0, Passed: 24, Skipped: 0, Total: 24
```

Run the tests again after code changes and update this section if the result changes. Passing automated tests does not by itself guarantee that every browser workflow or security requirement has been verified.

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Restore and build

Run these commands from the repository root:

```powershell
dotnet restore
dotnet build
```

### 2. Run tests

```powershell
dotnet test
```

### 3. Start the application

```powershell
cd src\AcxiomCRM
dotnet run
```

Use the URL printed in the terminal. In the local setup tested during development, the app was available at `http://localhost:5220`, with Swagger at `http://localhost:5220/swagger`. Your port may differ based on launch settings.

### Docker (optional)

The repository contains a `Dockerfile` and `docker-compose.yml`. If Docker is installed and those files are configured correctly, you can try:

```powershell
docker compose up --build
```

Use the port mapping configured in `docker-compose.yml`; do not assume it is the same as the local `dotnet run` port.

## GitHub Setup

From the project root, check the working tree and existing remote first:

```powershell
git status
git remote -v
git branch
```

If you have not configured a remote, add your repository URL (replace the placeholders):

```powershell
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPOSITORY.git
git branch -M main
git add .
git commit -m "Add AcxiomCRM project documentation"
git push -u origin main
```

If `origin` already exists, do not run `git remote add origin` again. Verify or update the existing remote instead. If your branch is not `main`, confirm the branch name before pushing.

Before pushing, inspect `git status` and `.gitignore`. Do not publish real credentials, secret keys, local databases containing sensitive information, or generated `bin/` and `obj/` folders.

## License

No license has been specified. Add a `LICENSE` file if you want to grant others explicit permission to use, modify, or distribute the project.
<!-- "http://localhost:5220/" -->