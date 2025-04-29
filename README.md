# 🌱 Path Finder

A modern job board web application that connects job seekers with companies and HR professionals. Built with ASP.NET Core MVC and Entity Framework Core, this site offers a complete solution for posting jobs, applying to offers, managing candidates, and administering users and roles.

---

## 🔍 Features

### 👤 User Roles
- **Unregistered users**: Can browse job offers and view company profiles.
- **Registered users**: Can manage their personal profile and apply to job offers.
- **Companies**: Can create a company profile, post jobs, and manage applications.
- **HR staff**: Can edit job postings, manage applications, and communicate via internal tickets.
- **Admins**: Have full control — manage users, roles, offers, categories, companies, and resolve HR tickets.

### 📄 Job Offers
- Create, edit, and delete offers.
- Filter offers by keyword, category, and location.
- Public job browsing and detailed offer views.

### 🧾 Applications
- Registered users can apply with resume and portfolio.
- Companies and HRs can review, approve, or reject candidates.
- Application status: `Pending → Approved / Rejected`.

### 🏢 Company Profiles
- Company logo, banner, description, industry, and contact info.
- Public page showcasing all posted jobs.
- Admin approval required for verification.

### 🎛️ Admin Panel
- Dashboards for HR and Admin users.
- Manage categories, users, companies, job listings, and tickets.
- Visual statistics and job posting trends.

### 🔔 Notifications
- Email alerts on status changes for job applications.
- Toast messages for success/error feedback across the UI.

---

## 🛠 Technologies Used

| Layer            | Tech Stack                             |
|------------------|-----------------------------------------|
| Backend          | ASP.NET Core MVC (.NET 8)              |
| Frontend         | Razor Views + Bootstrap 5              |
| Database         | MS SQL Server + Entity Framework Core  |
| Authentication   | ASP.NET Core Identity                  |
| File Storage     | Amazon S3 (logos, banners, resumes)    |
| Email Service    | SMTP (Gmail) via `IEmailSender`        |
| Hosting / CI/CD  | Docker + GitHub Actions (planned)      |

---

## 🚀 Getting Started

### 📦 Clone & Run Locally

```bash
git clone https://github.com/evam1dshk/JobOffer-WebSite.git
cd JobOffer-WebSite
