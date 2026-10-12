# AcxiomCRM — Enterprise Sales & Relationship Management System

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC%20%2B%20API-512BD4.svg)](https://learn.microsoft.com/aspnet/core)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-SQLite-blueviolet.svg)](https://learn.microsoft.com/ef/core)
[![Tests Passing](https://img.shields.io/badge/Tests-22%20Passed-success.svg)](#testing--acceptance-verification)
[![Swagger Enabled](https://img.shields.io/badge/Swagger-OpenAPI%20v1-85EA2D.svg)](#rest-api-specification)

A production-grade, enterprise Customer Relationship Management (CRM) prototype built in strict accordance with the **AcxiomCRM Functional & Technical Project Documentation**.

---

## 📑 Table of Contents
- [Executive Overview](#executive-overview)
- [Architecture & Tech Stack](#architecture--tech-stack)
- [Pre-Seeded Demo Credentials](#pre-seeded-demo-credentials)
- [Role-Based Access Control (RBAC)](#role-based-access-control-rbac)
- [Core CRM Workflows & Modules](#core-crm-workflows--modules)
- [Validation & Business Rules](#validation--business-rules)
- [Security & Compliance](#security--compliance)
- [REST API Specification](#rest-api-specification)
- [Testing & Acceptance Verification](#testing--acceptance-verification)
- [Quick Start Guide](#quick-start-guide)
- [GitHub Upload Instructions](#github-upload-instructions)

---

## 🏛 Executive Overview

AcxiomCRM implements the full sales and customer lifecycle:
1. **Lead Capture & Qualification** &rarr; Multi-channel lead tracking with source attribution and conversion into accounts.
2. **Customer Master Management** &rarr; Corporate directory with strict email/phone deduplication and sales executive ownership.
3. **Opportunity & Pipeline Forecasting** &rarr; Multi-stage pipeline with automated probability-weighted valuations (`Amount × Probability / 100`).
4. **Follow-Up & Activity Scheduling** &rarr; Interactive reminders with forward-dating rules and overdue escalations.
5. **Interactive Dashboard & Analytics** &rarr; Real-time Chart.js visualisations and role-scoped KPI cards.
6. **Governance & Audit Trail** &rarr; Immutable, append-only security and data mutation logging.
7. **RESTful Web APIs** &rarr; OpenAPI/Swagger documented endpoints returning standardized JSON payloads.

---

## 🏗 Architecture & Tech Stack

Following the layered design mandated in the specification:

```
                  ┌────────────────────────────────────────┐
                  │               Browser UI               │
                  │   (Bootstrap 5 + jQuery + Chart.js)    │
                  └───────────────────┬────────────────────┘
                                      │
           ┌──────────────────────────┴──────────────────────────┐
           │                                                     │
           ▼                                                     ▼
┌───────────────────────┐                             ┌───────────────────────┐
│    MVC Controllers    │                             │  REST API Controllers │
│  (Razor UI & Forms)   │                             │  (Swagger & JSON DTO) │
└──────────┬────────────┘                             └──────────┬────────────┘
           │                                                     │
           └──────────────────────────┬──────────────────────────┘
                                      │
                                      ▼
                       ┌─────────────────────────────┐
                       │      Application Layer      │
                       │   ViewModels, DTOs, Auth    │
                       └──────────────┬──────────────┘
                                      │
                                      ▼
                       ┌─────────────────────────────┐
                       │    Domain & Business Layer   │
                       │ ICrmValidationService, RBAC │
                       └──────────────┬──────────────┘
                                      │
                                      ▼
                       ┌─────────────────────────────┐
                       │      Data Access Layer      │
                       │   EF Core, AuditService     │
                       └──────────────┬──────────────┘
                                      │
                                      ▼
                       ┌─────────────────────────────┐
                       │        Database Layer       │
                       │  SQLite / Relational Schema │
                       └─────────────────────────────┘
```

- **Backend**: ASP.NET Core 8.0 MVC & Web API (C#)
- **Data Persistence**: Entity Framework Core 8.0 with SQLite (zero configuration, runs on any developer workstation)
- **Authentication**: ASP.NET Core Identity with PBKDF2 adaptive password hashing, strict password policy, and lockout handling
- **Frontend**: Bootstrap 5, Bootstrap Icons, Chart.js 4.4, jQuery Validate + Unobtrusive Validation
- **Documentation**: Swashbuckle Swagger UI (`/swagger`)

---

## 🔑 Pre-Seeded Demo Credentials

The application initializes with realistic test data and configured roles:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.local` | `Admin@12345!` | Full system administration, user & role management, security audits, all CRM records |
| **Manager** | `manager@acxiomcrm.local` | `Manager@12345!` | Full team pipeline oversight, reports, audit visibility (no security privilege escalations) |
| **Sales Executive** | `sales@acxiomcrm.local` | `Sales@12345!` | Assigned customer accounts, leads, opportunities, follow-ups |
| **Sales Executive 2** | `jane.sales@acxiomcrm.local` | `Sales@12345!` | Secondary executive account for testing multi-user scoping |

---

## 🛡 Role-Based Access Control (RBAC)

| Module / Action | Admin | Manager | Sales Executive |
| :--- | :---: | :---: | :---: |
| **Dashboard KPIs** | Organization-wide | Team-wide | Assigned Records Only |
| **Customers** | Full (All Records) | Full (All Records) | Assigned Records Only |
| **Leads** | Full (All Records) | Full (All Records) | Assigned Records Only |
| **Opportunities** | Full (All Records) | Full (All Records) | Assigned Records Only |
| **Follow-Ups** | Full (All Records) | Full (All Records) | Assigned Records Only |
| **Reports** | All Reports + CSV | All Reports + CSV | Scoped Pipeline/CSV |
| **User & Role Admin** | Full Access | No | No |
| **Unlock Accounts** | Full Access | No | No |
| **Audit Logs** | Full Unrestricted | Limited Operations | No Access |

---

## 💼 Core CRM Workflows & Modules

### 1. Lead-to-Customer Conversion
- Prospective leads advance through statuses: `New` &rarr; `Contacted` &rarr; `Qualified` &rarr; `Converted` (or `Lost`).
- Clicking **Convert Lead** presents a dedicated conversion workflow:
  - Automatically synthesizes a customer master account with a generated `CustomerCode`.
  - Optionally spins off an active sales opportunity with target amount, stage, probability, and expected close date.
  - Generates comprehensive audit log trails.

### 2. Opportunity Pipeline & Weighted Revenue
- Active stages: `Qualification` &rarr; `Proposal` &rarr; `Negotiation` &rarr; `Won` / `Lost`.
- Automatic weighted pipeline calculation: `Weighted Value = Amount × Probability / 100`.
- Summary dashboards display both Face Value and Risk-Adjusted Pipeline Forecast.

### 3. Follow-Up Management & Reminders
- Scheduled activities against Customer, Lead, or Opportunity.
- Tabbed dashboard filters: `All`, `Upcoming Due`, and `Overdue Alerts`.
- One-click completion with outcome note journaling.

---

## 📐 Validation & Business Rules

AcxiomCRM implements multi-level validation: client-side for user experience, server-side for integrity, and business rule services:

| Field / Area | Rule Enforced | Rejection Behavior |
| :--- | :--- | :--- |
| **Opportunity Amount** | Must be strictly greater than 0 (`Amount > 0`) | Rejects with *"Opportunity Amount must be greater than 0."* |
| **Opportunity Probability** | Must be between 0 and 100 inclusive (`0 ≤ Probability ≤ 100`) | Rejects with *"Probability must be between 0 and 100."* |
| **Opportunity Close Date** | Active opportunities cannot have a close date in the past | Rejects with *"Expected Close Date cannot be in the past."* |
| **Follow-Up Date** | Cannot be earlier than current date for planned follow-ups | Rejects with *"Follow-up date cannot be earlier than today."* |
| **Customer Email** | Format check + unique constraint | Rejects with *"A customer with this email address already exists."* |
| **Customer Phone** | Format check (7–20 digits) + unique constraint | Rejects with *"A customer with this phone number already exists."* |
| **Lead Status** | Must conform to configured lifecycle set | Rejects with *"Invalid lead status."* |

---

## 🔒 Security & Compliance

- **Password Policy**: Minimum 8 characters, requiring uppercase, lowercase, digit, and special character.
- **Account Lockout**: 5 failed consecutive attempts triggers an automatic 15-minute account lockout. Administrators can manually unlock accounts via the User Administration portal.
- **Anti-Forgery Protection**: State-changing POST forms protected via `[ValidateAntiForgeryToken]`.
- **SQL Injection Prevention**: Entity Framework Core parameterized query execution across all repository operations.
- **Audit Logging**: Append-only audit logs capturing `UserId`, `Action`, `EntityName`, `RecordId`, `OldValue`, `NewValue`, `Timestamp`, and `IpAddress`.

---

## 🌐 REST API Specification

OpenAPI / Swagger documentation is available interactively at `/swagger`.

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Authenticate API client and return user profile details |
| `POST` | `/api/auth/logout` | Terminate session |
| `GET` | `/api/customers` | Search & list customers (scoped to user role) |
| `GET` | `/api/customers/{id}` | Retrieve customer detail |
| `POST` | `/api/customers` | Create new customer (auto-generates `CustomerCode`) |
| `PUT` | `/api/customers/{id}` | Update customer record |
| `DELETE`| `/api/customers/{id}` | Delete / de-activate customer |
| `GET` | `/api/leads` | Search & list leads |
| `POST` | `/api/leads` | Create prospective lead |
| `POST` | `/api/leads/{id}/convert` | Convert lead into Customer and Opportunity |
| `GET` | `/api/opportunities` | Search & list active opportunities |
| `POST` | `/api/opportunities` | Create sales opportunity |
| `GET` | `/api/followups` | Retrieve follow-up tasks |
| `POST` | `/api/followups` | Schedule new follow-up |
| `GET` | `/api/reports/pipeline` | Retrieve aggregated pipeline stage and owner distribution |

---

## 🧪 Testing & Acceptance Verification

All 14 acceptance criteria specified in Section 17.19 are covered by automated unit and integration tests:

```powershell
dotnet test
```

### Test Suite Execution Output:
```text
Passed!  - Failed: 0, Passed: 22, Skipped: 0, Total: 22, Duration: 1 s - AcxiomCRM.Tests.dll
```

### Coverage Matrix:
- `AcceptanceCriteriaTests.cs`: Validates specific test cases from Section 17.19 (Amount &le; 0 rejection, Probability &gt; 100 rejection, past date checks, immutable audit entries, REST API serialization).
- `BusinessValidationTests.cs`: Unit tests for customer uniqueness, email format, phone format, and date bounds.
- `LeadWorkflowTests.cs`: Unit tests for Lead-to-Customer conversion and weighted opportunity math.
- `RoleScopingAndApiTests.cs`: Tests data isolation between Admin and Sales Executive scopes.
- `AuditLogTests.cs`: Verifies append-only logging of user mutations and payload diffs.

---

## 🚀 Quick Start Guide

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Clone & Build
```bash
git clone <your-github-repo-url>
cd AcxiomCRM
dotnet restore
dotnet build
```

### 2. Run the Application
```bash
cd src/AcxiomCRM
dotnet run
```
Navigate to: **`http://localhost:5000`** (or the port specified in console output).

### 3. Run with Docker (Alternative)
```bash
docker-compose up --build
```
Navigate to: **`http://localhost:5000`**.

---

## 📦 GitHub Upload Instructions

To upload this solution to your GitHub repository:

```bash
# 1. Initialize git in the root folder
cd C:\Users\sabya\.gemini\antigravity\scratch\AcxiomCRM
git init

# 2. Stage and commit all files
git add .
git commit -m "feat: complete production-grade AcxiomCRM prototype implementation"

# 3. Rename branch to main
git branch -M main

# 4. Add your GitHub repository as remote origin
git remote add origin https://github.com/<your-username>/<your-repo-name>.git

# 5. Push to GitHub
git push -u origin main
```
