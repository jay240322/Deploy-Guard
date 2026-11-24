<div align="center">

  <img src="wwwroot/images/logo.png" alt="DeployGuard Logo" width="120" height="120" />

  # DeployGuard
  ### Automated Source Code Security Scanner, Secret Detection Engine & CI/CD Pipeline Visualizer

  [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
  [![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/mvc)
  [![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-316192?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
  [![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
  [![QuestPDF](https://img.shields.io/badge/QuestPDF-Vector%20Reports-007ACC?style=for-the-badge)](https://www.questpdf.com/)
  [![Bootstrap 5](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
  [![License: MIT](https://img.shields.io/badge/License-MIT-success.svg?style=for-the-badge)](LICENSE)

  <p align="center">
    <strong>DeployGuard</strong> is a high-performance web platform that continuously scans Git repositories for exposed API tokens, database credentials, and cloud secrets, while parsing CI/CD workflows into interactive dependency graphs before production deployment.
  </p>

</div>

---

## Overview

**DeployGuard** is an automated code security and workflow analysis platform engineered with **ASP.NET Core (.NET 10)** and **PostgreSQL**. Designed for engineering teams and modern development workflows, DeployGuard proactively intercepts hardcoded credentials, cloud API tokens, and cryptographic keys from source code repositories.

In addition to static secret detection, DeployGuard inspects workflow configuration files (GitHub Actions, GitLab CI, Azure Pipelines, and Jenkinsfiles) and parses them into structured execution stages with interactive dependency maps.

### Key Problems Solved
1. **Accidental Credential Exposure**: Catches leaked credentials (AWS, Stripe, GitHub, database connection strings) before release.
2. **Zero-Retention Sandboxing**: Employs shallow single-branch cloning and purges ingested code from disk immediately after scanning.
3. **Pipeline Observability**: Parses complex YAML/Jenkinsfile pipelines into visual stages with dependencies and conditional triggers.
4. **Audit Readiness**: Generates vector-based PDF audit reports complete with severity metrics and line-by-line remediation coordinates.
5. **Private Repo Safety**: Automatically guards against unauthorized private repository access with graceful client-side notifications.

---

## Key Features

- **Multi-Threaded Parallel Scanning Engine**: Employs `Parallel.ForEachAsync` with precompiled regular expressions and smart directory exclusion (`.git`, `node_modules`, `bin`, `obj`, lock files, bundles) to scan full repositories in seconds.
- **Zero Data Retention Sandbox**: Ingests code using single-branch shallow clones (`git clone --depth 1 --single-branch --no-tags`) with strict `GIT_TERMINAL_PROMPT=0` safeguards, automatically wiping files upon scan termination.
- **Private Repository Detection**: Instantly identifies private or inaccessible repositories without server hangs, presenting an informative modal popup in the browser.
- **Defense-in-Depth Token Masking**: Raw secrets are masked at the service boundary (e.g., `AKIA****************1234`) to prevent secondary leakage in database records, web logs, or reports.
- **Interactive CI/CD Workflow Visualizer**: AST-based workflow parser mapping jobs, prerequisite dependencies (`needs`), parallel execution flags, and execution conditions (`if`).
- **Vector PDF Audit Reports**: Powered by **QuestPDF** to generate crisp, print-ready security audit summaries with severity breakdowns, risk scores, and finding timestamps.
- **Role-Based Access Control & Auth**: Cookie authentication with PBKDF2 password hashing, user registration, and a dedicated administration dashboard for system analytics.
- **Minimal UI Design System**: Built on Bootstrap 5 with responsive dark/light mode toggles, interactive status spinners, and clean typography.

---

## System Architecture

<div align="center">
  <img src="wwwroot/images/architecture-diagram.png" alt="DeployGuard Architecture Diagram" width="100%" />
</div>

<br/>

### Scan Execution & Processing Workflow

```mermaid
flowchart TD
    A([User Input: Repo URL]) --> B[RepositoryUrlParser]
    B --> C{Validate Provider}
    C -->|Supported| D[IGitProvider API Metadata Fetch]
    C -->|Unsupported| E[Error Notification]
    
    D --> F[Database: Register Repository]
    F --> G[User clicks Scan Repository]
    
    G --> H[RepositoryCloneService]
    H -->|Shallow clone: depth 1| I{Git Clone Check}
    I -->|Private / Auth Failed| J[Throw PrivateRepositoryException]
    J --> K[Client Popup: Private Repo Detected]
    
    I -->|Success| L[SecretScannerService]
    L -->|Filter Directories & Files| M[Parallel.ForEachAsync]
    M -->|Precompiled Regex Matching| N[Collect Masked Findings]
    
    L --> O[PipelineDetectionService]
    O -->|Parse YAML / AST| P[Extract Stages & Dependencies]
    
    N --> Q[Save SecretScan & Findings to PostgreSQL]
    P --> Q
    Q --> R[Wipe Sandbox Directory from Disk]
    R --> S[Redirect to Scan Details & PDF Export]
```

---

## Technology Stack

| Layer | Technology | Purpose |
|---|---|---|
| **Framework** | **ASP.NET Core 10 (MVC)** | High-throughput asynchronous backend web application |
| **Language** | **C# 13 / .NET 10** | Type-safe, high-performance object-oriented programming |
| **ORM & Data** | **Entity Framework Core 10** | Code-First migrations, automated seeding, relational queries |
| **Database** | **PostgreSQL (Npgsql)** | ACID-compliant relational storage (supports Neon, Supabase, Railway) |
| **PDF Generation** | **QuestPDF (Community)** | Fluent, in-memory vector PDF document generation |
| **Workflow Parsing** | **YamlDotNet** | Abstract Syntax Tree (AST) parser for CI/CD workflow files |
| **Authentication** | **Cookie Auth + ASP.NET Identity** | PBKDF2 password hashing and secure HTTP-only cookies |
| **Frontend & UI** | **Bootstrap 5.3 + Bootstrap Icons** | Responsive UI with custom theme engine (Dark / Light mode) |
| **Containerization** | **Docker** | Multi-stage production container with Git CLI preinstalled |

---

## Supported Secret Rules

DeployGuard ships with a comprehensive set of pre-seeded regex detection rules:

| Category | Secret Type | Default Severity | Description |
|---|---|:---:|---|
| **Cloud** | AWS Access Key | `Critical` | AWS access key identifiers (`AKIA...`) |
| **Cloud** | AWS Secret Key | `Critical` | AWS secret access credentials |
| **Cloud** | Google API Key | `High` | Public/Private Google Cloud API keys |
| **Source Control** | GitHub Personal Token | `Critical` | Classic and fine-grained GitHub PATs |
| **Source Control** | GitLab Personal Token | `Critical` | GitLab personal and deploy tokens |
| **Payments** | Stripe Live Secret Key | `Critical` | Production Stripe secret keys (`sk_live_...`) |
| **Payments** | Stripe Test Secret Key | `High` | Test Stripe API keys (`sk_test_...`) |
| **Database** | Database Connection URI | `Critical` | Hardcoded MySQL, PostgreSQL, and SQL passwords |
| **Cryptography** | Private Cryptographic Key | `Critical` | RSA, EC, DSA, and OpenSSH private keys |
| **Auth** | JWT Secret Key | `High` | Hardcoded JWT HMAC signing secrets |
| **Messaging** | Slack API / Bot Token | `High` | Slack bot and workspace user tokens |
| **Messaging** | Twilio API Secret | `High` | Twilio communication service keys |
| **Email** | SendGrid API Key | `High` | SendGrid mail delivery service keys |
| **AI / ML** | OpenAI API Key | `Critical` | OpenAI developer platform keys |

---

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git CLI](https://git-scm.com/) installed and available on system `PATH`
- [PostgreSQL](https://www.postgresql.org/) (Local instance or cloud provider like Neon / Supabase)

### 1. Clone the Repository
```bash
git clone https://github.com/jay240322/Deploy-Guard.git
cd Deployguard
```

### 2. Configure Environment Variables
Copy the template `.env.example` file to `.env`:
```bash
cp .env.example .env
```

Edit `.env` with your PostgreSQL database connection string and administrator credentials:
```env
ASPNETCORE_ENVIRONMENT=Development
DATABASE_URL=postgresql://postgres:password@localhost:5432/deployguard?sslmode=prefer
ADMIN_PASSWORD=YourSecureAdminPassword123!
```

### 3. Run Database Migrations & Seed
EF Core will automatically apply schema migrations and seed the default rules on application launch. To manually run migrations via CLI:
```bash
dotnet ef database update
```

### 4. Build & Run
```bash
# Build the project
dotnet build

# Launch the application
dotnet run
```

Open your browser at:
- **HTTP**: `http://localhost:5044`
- **HTTPS**: `https://localhost:7270`

---

## Docker & Deployment

DeployGuard includes a production-ready, multi-stage Dockerfile with Git CLI preinstalled in the runtime container.

### Running with Docker

```bash
# Build the Docker image
docker build -t deployguard:latest .

# Run the container
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e PORT=8080 \
  -e DATABASE_URL="postgresql://username:password@hostname:5432/dbname?sslmode=require" \
  -e ADMIN_PASSWORD="YourStrongPassword" \
  --name deployguard-app deployguard:latest
```

### Deploying to Railway / Render

1. Create a new service from your GitHub repository.
2. Railway/Render will automatically detect the **Dockerfile**.
3. In the **Variables** tab, supply the following:
   ```env
   ASPNETCORE_ENVIRONMENT=Production
   PORT=8080
   DATABASE_URL=postgresql://username:password@hostname:5432/dbname?sslmode=require
   ADMIN_PASSWORD=YourAdminPasswordHere
   ```
4. Deploy the service. The database schema will automatically migrate and seed on startup.

---

## Health Check Endpoint

DeployGuard provides an unauthenticated health probe endpoint for container orchestrators and uptime monitors:

- **Endpoint**: `GET /health`
- **Response**:
  ```json
  {
    "status": "ok",
    "timestamp": "2026-11-24T08:30:00.0000000Z",
    "uptime": 1420.5
  }
  ```

---

## Database Schema

```mermaid
erDiagram
    AppUser ||--o{ Repository : "owns"
    AppUser ||--o{ SecretScan : "triggers"
    Repository ||--o{ SecretScan : "has"
    Repository ||--o{ PipelineFile : "contains"
    SecretScan ||--o{ SecretFinding : "produces"
    SecretRule ||--o{ SecretFinding : "matches"
    PipelineFile ||--o{ PipelineStage : "defines"

    AppUser {
        int Id PK
        string UserName
        string Email
        string FullName
        string Role
        bool IsActive
        DateTime CreatedAt
    }

    Repository {
        int Id PK
        string Name
        string Owner
        string RepoName
        string RepoUrl
        string Provider
        string DefaultBranch
        string Language
        int SizeKb
        int CreatedByUserId FK
        DateTime CreatedAt
        DateTime LastScannedAt
    }

    SecretScan {
        int Id PK
        int RepositoryId FK
        int TriggeredByUserId FK
        int FilesScanned
        int SecretsFound
        string Status
        DateTime StartedAt
        DateTime CompletedAt
    }

    SecretFinding {
        int Id PK
        int SecretScanId FK
        int SecretRuleId FK
        string FilePath
        int LineNumber
        string MaskedValue
        string Severity
        bool IsFalsePositive
    }

    SecretRule {
        int Id PK
        string Name
        string Pattern
        string Severity
        string Category
        bool IsActive
    }

    PipelineFile {
        int Id PK
        int RepositoryId FK
        string Provider
        string FilePath
        string RawYaml
    }

    PipelineStage {
        int Id PK
        int PipelineFileId FK
        string Name
        int Order
        string DependsOn
        bool IsParallel
        string Condition
    }
```

---

## Contributing

Contributions, bug reports, and feature proposals are welcome!

1. **Fork** the repository.
2. **Create** a feature branch:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Commit** your changes following standard conventional commits:
   ```bash
   git commit -m "feat: add support for Bitbucket Pipelines detection"
   ```
4. **Push** to your branch:
   ```bash
   git push origin feature/your-feature-name
   ```
5. **Open** a Pull Request against `main`.

---

## Author & Contact

**Jay Patel**
- **Portfolio**: [jaypatel.online](https://www.jaypatel.online)
- **GitHub**: [@jay240322](https://github.com/jay240322)
- **LinkedIn**: [Jay Patel](https://linkedin.com)
- **Email**: [pateljay0j@gmail.com](mailto:pateljay0j@gmail.com)
- **Location**: Ahmedabad, Gujarat, India

---

## License

This project is licensed under the [MIT License](LICENSE) — feel free to use and modify it for personal and commercial projects.
