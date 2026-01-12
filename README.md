<p align="center">
  <img src="wwwroot/images/logo.png" alt="PeP Logo" width="320" height="128">
</p>

<h1 align="center">PeP - Programming Examination Platform</h1>

<p align="center">
  <strong>A comprehensive, secure online examination system with SafeExamBrowser-like protection and AI-powered code evaluation</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#programming-exam-features-new">Programming Exams</a> •
  <a href="#live-demo">Live Demo</a> •
  <a href="#download">Download</a> •
  <a href="#installation">Installation</a> •
  <a href="#deployment">Deployment</a> •
  <a href="#security">Security</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Version-5.0-brightgreen?style=for-the-badge" alt="Version 5.0">
  <img src="https://img.shields.io/badge/.NET-6.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 6.0">
  <img src="https://img.shields.io/badge/Blazor-Server-512BD4?style=for-the-badge&logo=blazor" alt="Blazor Server">
  <img src="https://img.shields.io/badge/WPF-.NET%208-512BD4?style=for-the-badge&logo=windows" alt="WPF .NET 8">
  <img src="https://img.shields.io/badge/OpenAI-GPT--4o-412991?style=for-the-badge&logo=openai" alt="OpenAI GPT-4o">
  <img src="https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=for-the-badge&logo=microsoftsqlserver" alt="SQL Server">
  <img src="https://img.shields.io/badge/Google%20Cloud-Deployed-4285F4?style=for-the-badge&logo=googlecloud" alt="Google Cloud">
</p>

---

## 🆕 What's New in v5.0

- 💻 **Programming Exams** - Full IDE environment with Monaco editor for coding exams
- 🤖 **AI Code Evaluation** - Automatic grading with detailed feedback using GPT-4o
- 📊 **Multi-Metric Scoring** - Code Quality, Correctness, and Efficiency metrics
- 🎯 **Task Navigator** - Side panel with task instructions and progress tracking
- 📁 **Solution Explorer** - File tree navigation with starter files and student code
- 📄 **CSV Data Viewer** - Tab-based viewer for data files
- 🔄 **Resizable Splitter** - Adjustable split between code editor and task panel
- 🧠 **AI Exam Generation** - Generate complete programming projects with AI
- 📦 **Multi-File Projects** - Support for multiple source files in Java, Python, C#, and JavaScript

---

## 🌐 Live Demo

**Web Application:** [https://pep-webapp-795388481242.us-central1.run.app](https://pep-webapp-795388481242.us-central1.run.app)

## 📥 Download

### PeP Exam App (Secure Desktop Client)

Download the secure exam browser for Windows:

| Version | Download | Size | What's New |
|---------|----------|------|------------|
| **v5.0.0** | [⬇️ Download PeP.ExamApp_Setup_5.0.0.exe](https://storage.googleapis.com/pep-downloads/PeP.ExamApp_Setup_5.0.0.exe) | ~50 MB | Programming Exams, AI Evaluation |
| v1.0.0 | [Download](https://storage.googleapis.com/pep-downloads/PeP.ExamApp_Setup_1.0.0.exe) | ~48 MB | Initial release |

**System Requirements:**
- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included in installer)
- WebView2 Runtime (auto-installed if missing)
- Administrator privileges (required for exam mode)

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Architecture](#-architecture)
- [Technology Stack](#-technology-stack)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Deployment](#-deployment)
- [Usage Guide](#-usage-guide)
- [Security Features](#-security-features)
- [API Documentation](#-api-documentation)
- [Troubleshooting](#-troubleshooting)
- [License](#-license)

---

## 🌟 Overview

**PeP (Professional Exam Platform)** is a complete examination solution designed for educational institutions, training centers, and certification bodies. It consists of two main components:

1. **PeP Web Application** - A Blazor Server web application for exam management, creation, and administration
2. **PeP ExamApp** - A secure WPF desktop application that provides SafeExamBrowser-like lockdown features for high-stakes exams

### Why PeP?

- ✅ **Secure Testing Environment** - Prevents cheating with comprehensive system lockdown
- ✅ **Easy to Use** - Intuitive interface for teachers and students
- ✅ **Flexible Scoring** - Multiple scoring modes including partial credit
- ✅ **Real-time Monitoring** - Track student progress and violations
- ✅ **Cross-platform Web Access** - Manage exams from any browser
- ✅ **AI-Powered** - Optional AI assistance for question generation

---

## ✨ Features

### 🌐 Web Application Features

#### For Administrators
| Feature | Description |
|---------|-------------|
| 👥 **User Management** | Create, edit, and manage teacher and student accounts |
| 📊 **Platform Analytics** | View platform-wide statistics and reports |
| ⚙️ **System Settings** | Configure platform settings, branding, and policies |
| 📈 **Performance Reports** | Detailed reports on teacher and student performance |

#### For Teachers
| Feature | Description |
|---------|-------------|
| 📚 **Course Management** | Create and organize courses |
| 📝 **Exam Builder** | Intuitive drag-and-drop exam creation |
| 💻 **Programming Exam Builder** | Create coding exams with AI-powered task generation |
| ❓ **Question Bank** | Reusable question library with multiple types |
| 🔢 **Exam Codes** | Generate secure access codes with expiration |
| 👨‍🎓 **Student Management** | Add students individually or bulk import via CSV |
| 📊 **Results & Analytics** | Detailed exam results and performance analytics |
| 🤖 **AI Question Generation** | Generate questions using OpenAI integration |
| 🧠 **AI Code Evaluation** | Automatic code grading with detailed feedback |

#### For Students
| Feature | Description |
|---------|-------------|
| 🎯 **Take Exams** | Clean, distraction-free exam interface |
| � **Programming Exams** | Full-featured IDE with Monaco editor, file explorer, and console |
| 📱 **Responsive Design** | Works on desktop, tablet, and mobile |
| 📊 **View Results** | Detailed breakdown of exam performance |
| 🧠 **AI Evaluation Report** | Comprehensive code feedback with quality metrics |
| 📜 **Exam History** | Access all past exam attempts |

### 🖥️ ExamApp (Secure Desktop Client)

#### Lockdown Features
| Feature | Description |
|---------|-------------|
| 🔒 **Fullscreen Mode** | Forces fullscreen, prevents window switching |
| ⌨️ **Keyboard Blocking** | Blocks Alt+Tab, Win key, Print Screen, etc. |
| 📋 **Clipboard Control** | Clears and monitors clipboard |
| 🖥️ **Taskbar Hiding** | Hides Windows taskbar completely |
| 📸 **Screen Capture Protection** | Prevents screenshots and screen recording |
| 🔍 **Focus Monitoring** | Detects and logs focus changes |
| 🛡️ **Process Monitoring** | Detects forbidden applications |

#### Anti-Cheat Features
| Feature | Description |
|---------|-------------|
| 🖥️ **VM Detection** | Detects VMware, VirtualBox, Hyper-V, etc. |
| 🐛 **Debugger Detection** | Detects attached debuggers |
| 📺 **Multi-Monitor Detection** | Ensures single display usage |
| 🌐 **Remote Session Detection** | Blocks Remote Desktop sessions |
| ⚠️ **Violation Tracking** | Logs all security violations |
| 🔐 **Watermarking** | Overlays student info on exam content |

#### Custom Taskbar
| Feature | Description |
|---------|-------------|
| 🕐 **Clock & Date** | Real-time clock display |
| 🌐 **Internet Status** | Network connectivity indicator (click to open network panel) |
| ⚠️ **Violation Counter** | Shows security violation count |
| 🚪 **Secure Exit** | Password-protected exam exit |

#### Network Settings Panel
| Feature | Description |
|---------|-------------|
| 📶 **Available Networks** | Lists all nearby WiFi networks with signal strength |
| 🔒 **Secure Networks** | Shows lock icon for password-protected networks |
| 🔐 **WiFi Password** | Enter password to connect to secured networks |
| 🔄 **Refresh** | Scan for new available networks |
| ⚡ **Current Connection** | Shows connected network name and status |
| 🔌 **Disconnect** | Disconnect from current WiFi network |
| 🔌 **Ethernet Status** | Shows real Ethernet connection (excludes virtual adapters) |

### 💻 Programming Exam Features (NEW)

#### IDE Environment
| Feature | Description |
|---------|-------------|
| 📝 **Monaco Code Editor** | VS Code-style editor with syntax highlighting, IntelliSense, and auto-completion |
| 📁 **Solution Explorer** | File tree navigation with starter files and student-created files |
| 🎯 **Task Navigator** | Side panel showing task instructions, hints, and progress tracking |
| 📊 **Resizable Splitter** | Adjustable split between code editor and task panel |
| ⏱️ **Live Timer** | Countdown timer with color-coded urgency indicators |
| 🔄 **Auto-save** | Automatic progress saving during exam |
| 🖥️ **Console Output** | Integrated console for program output and errors |
| 📄 **CSV Data Viewer** | Tab-based viewer for CSV data files |

#### AI-Powered Code Evaluation
| Feature | Description |
|---------|-------------|
| 🧠 **Automatic Grading** | AI evaluates code correctness, quality, and efficiency |
| 📊 **Multi-Metric Scoring** | Code Quality (25%), Correctness (40%), Completion (20%), Efficiency (15%) |
| 🎯 **Arc Gauge Visualization** | Visual representation of scores with color-coded indicators |
| 💬 **Detailed Feedback** | Strengths, areas for improvement, and solution suggestions |
| 📝 **Code Snippets** | Specific code excerpts with inline comments and suggestions |
| ⚖️ **Balanced Evaluation** | Fair but thorough - strict on empty code, partial credit for genuine attempts |
| 🔄 **Re-evaluation** | Teachers can trigger AI re-evaluation for any submission |

#### Programming Languages Supported
| Language | Features |
|----------|----------|
| 🐍 **Python** | Full syntax support, CSV parsing, pandas integration, multi-file projects |
| ☕ **Java** | Class-based projects, multi-file support, automatic class combining for execution |
| 🌐 **JavaScript** | ES6+ syntax, Node.js-style console output, multi-file modules |
| 💜 **C#** | .NET-style projects with proper class structure, multi-file support |

> **📝 Technical Note: Java Multi-File Support**
> 
> Java projects with multiple classes are automatically combined into a single source file for execution via the Piston API. The main class (containing `public static void main`) is placed first and remains `public`, while all other classes are converted to package-private (non-public) to ensure successful compilation. Import statements are deduplicated and placed at the top of the combined file.

#### Teacher Tools
| Feature | Description |
|---------|-------------|
| 🤖 **AI Exam Generator** | Generate complete programming exams with AI |
| 📋 **Task Builder** | Manual task creation with hints, target files, and test cases |
| 📁 **Starter Files** | Upload CSV data files and initial code templates |
| 📊 **Grading Dashboard** | View all submissions with AI scores and manual override |
| 📥 **Code Download** | Download student code as ZIP archive |

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        PeP Platform                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────┐       ┌─────────────────────────────┐  │
│  │   PeP Web App       │       │      PeP ExamApp            │  │
│  │   (Blazor Server)   │       │      (WPF .NET 8)           │  │
│  │                     │       │                             │  │
│  │  • Admin Portal     │       │  • Secure Browser           │  │
│  │  • Teacher Portal   │◄─────►│  • System Lockdown          │  │
│  │  • Student Portal   │  API  │  • Anti-Cheat Engine        │  │
│  │  • Exam API         │       │  • WebView2 Integration     │  │
│  └──────────┬──────────┘       └─────────────────────────────┘  │
│             │                                                   │
│             ▼                                                   │
│  ┌─────────────────────┐       ┌─────────────────────────────┐  │
│  │   SQL Server        │       │      External Services      │  │
│  │   Database          │       │                             │  │
│  │                     │       │  • OpenAI API (Optional)    │  │
│  │  • Users            │       │  • SMTP Email               │  │
│  │  • Courses          │       │  • Azure AD (Optional)      │  │
│  │  • Exams            │       │                             │  │
│  │  • Questions        │       └─────────────────────────────┘  │
│  │  • Attempts         │                                        │
│  └─────────────────────┘                                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
PeP/
├── 📁 Controllers/           # API Controllers
│   ├── AccountController.cs
│   └── ExamAppController.cs
├── 📁 Data/                  # Database Context & Seed Data
│   └── ApplicationDbContext.cs
├── 📁 Models/                # Domain Models
│   ├── ApplicationUser.cs
│   ├── ExamModels.cs
│   ├── ProgrammingExamModels.cs  # NEW: Programming exam entities
│   └── ExamAppModels.cs
├── 📁 Services/              # Business Logic
│   ├── ExamService.cs
│   ├── ProgrammingExamService.cs # NEW: Programming exam logic
│   ├── CodeExecutionService.cs   # NEW: Code execution & testing
│   ├── UserService.cs
│   └── OpenAIService.cs          # AI evaluation & generation
├── 📁 Pages/                 # Blazor Pages
│   ├── 📁 Admin/            # Admin pages
│   ├── 📁 Teacher/          # Teacher pages
│   │   ├── CreateProgrammingExam.razor  # NEW: Programming exam builder
│   │   └── GradeProgrammingExam.razor   # NEW: AI grading dashboard
│   ├── 📁 Student/          # Student pages
│   │   ├── TakeProgrammingExam.razor    # NEW: Programming exam IDE
│   │   └── ExamEvaluation.razor         # NEW: AI evaluation results
│   └── 📁 Account/          # Authentication pages
├── 📁 Shared/                # Shared Components
│   ├── MainLayout.razor
│   ├── NavMenu.razor
│   ├── 📁 Components/       # NEW: Reusable components
│   │   └── TaskNavigator.razor          # NEW: Task navigation panel
│   └── 📁 Dialogs/
├── 📁 ViewModels/            # View Models
├── 📁 wwwroot/               # Static Files
│   └── 📁 css/
├── 📄 Program.cs             # Application Entry Point
├── 📄 appsettings.json       # Configuration
└── 📄 PeP.csproj             # Project File

PeP.ExamApp/
├── 📁 Controls/              # Custom WPF Controls
│   └── ExamTaskbar.xaml
├── 📁 Pages/                 # Application Pages
│   ├── ConnectPage.xaml
│   ├── LoginPage.xaml
│   ├── ExamCodePage.xaml
│   ├── TeacherAuthPage.xaml
│   ├── TutorialPage.xaml
│   ├── AntiCheatPage.xaml
│   └── ExamRunnerPage.xaml
├── 📁 Themes/                # UI Themes
│   └── ModernTheme.xaml
├── 📄 MainWindow.xaml        # Main Window
├── 📄 ExamModeManager.cs     # System Lockdown Manager
├── 📄 SecurityChecks.cs      # Anti-Cheat Engine
├── 📄 NativeMethods.cs       # Windows API Declarations
├── 📄 ExamAppApiClient.cs    # API Client
├── 📄 ExamAppState.cs        # Application State
└── 📄 PeP.ExamApp.csproj     # Project File
```

---

## 🛠️ Technology Stack

### Backend & Web
| Technology | Purpose |
|------------|---------|
| **.NET 6.0** | Web application framework |
| **Blazor Server** | Interactive web UI |
| **Entity Framework Core** | ORM & database access |
| **ASP.NET Core Identity** | Authentication & authorization |
| **SQL Server** | Database |
| **Radzen Blazor** | UI component library |
| **Monaco Editor** | VS Code-style code editor for programming exams |

### Desktop Application
| Technology | Purpose |
|------------|---------|
| **.NET 8.0** | Desktop framework |
| **WPF** | Desktop UI framework |
| **WebView2** | Embedded browser |
| **Win32 API** | System lockdown features |

### External Integrations
| Service | Purpose |
|---------|---------|
| **OpenAI API (GPT-4o-mini)** | AI question generation & code evaluation |
| **Piston API** | Code execution engine for Python, Java, JavaScript, and C# |
| **SMTP** | Email notifications (optional) |

### AI Features (OpenAI Integration)
| Feature | Description |
|---------|-------------|
| **Question Generation** | Generate multiple-choice questions from topic prompts |
| **Programming Exam Generation** | Generate complete coding projects with tasks and starter files |
| **Code Evaluation** | Automatic grading with detailed feedback on code quality, correctness, and efficiency |
| **Configurable Model** | Support for GPT-4o, GPT-4o-mini, and other OpenAI models |

---

## 📦 Installation

### Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (for Web App)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for ExamApp)
- [SQL Server](https://www.microsoft.com/sql-server) (or SQL Server Express/LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (for ExamApp)

### Quick Start

#### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/PeP.git
cd PeP
```

#### 2. Configure the Database

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PePDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

#### 3. Apply Database Migrations

```bash
dotnet ef database update
```

#### 4. Run the Web Application

```bash
dotnet run
```

The application will be available at `https://localhost:5001`

#### 5. Build the ExamApp (Optional)

```bash
cd PeP.ExamApp
dotnet build
dotnet run
```

### Default Credentials

After the first run, the following default accounts are created:

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@pep.com` | `Admin123!` |

> ⚠️ **Important:** Change these passwords immediately in production!

---

## ⚙️ Configuration

### Application Settings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your SQL Server connection string"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4",
    "Enabled": true
  },
  "Email": {
    "SmtpServer": "smtp.example.com",
    "SmtpPort": 587,
    "Username": "noreply@example.com",
    "Password": "your-email-password",
    "FromAddress": "noreply@example.com",
    "FromName": "PeP Platform"
  },
  "Platform": {
    "Name": "PeP - Professional Exam Platform",
    "AllowRegistration": false,
    "RequireEmailConfirmation": true,
    "DefaultExamDuration": 60,
    "MaxFileUploadSize": 10485760
  }
}
```

### ExamApp Configuration

The ExamApp reads the server URL from user input. For deployment, you can set defaults in `ExamAppState.cs`.

### Environment Variables

For production, use environment variables or Azure Key Vault:

```bash
# Database
export ConnectionStrings__DefaultConnection="Server=..."

# OpenAI
export OpenAI__ApiKey="sk-..."

# Email
export Email__Password="..."
```

---

## � Deployment

### Google Cloud Deployment

PeP is deployed on Google Cloud using Cloud Run and Cloud SQL for SQL Server.

#### Architecture
- **Cloud Run** - Containerized web application (auto-scaling)
- **Cloud SQL** - Managed SQL Server database
- **Cloud Storage** - Hosting for ExamApp installer downloads

#### Deploying Updates

1. **Build and push Docker image:**
```bash
gcloud builds submit --tag us-central1-docker.pkg.dev/pep-platform/pep-docker/pep-webapp
```

2. **Deploy to Cloud Run:**
```bash
gcloud run deploy pep-webapp \
    --image us-central1-docker.pkg.dev/pep-platform/pep-docker/pep-webapp:latest \
    --region us-central1
```

#### Uploading New ExamApp Installer

1. **Build the installer:**
```powershell
cd PeP.ExamApp\Installer
.\build-installer.ps1 -Version "1.1.0"
```

2. **Upload to Cloud Storage:**
```bash
gcloud storage cp "Output\PeP.ExamApp_Setup_1.1.0.exe" gs://pep-downloads/
```

3. **Update the download URL** in `Pages/Index.razor` if the version changed.

#### Estimated Monthly Costs

| Service | Specification | Est. Cost |
|---------|--------------|-----------|
| Cloud SQL for SQL Server | 1 vCPU, 3.75GB RAM | ~$50-80 |
| Cloud Run | Auto-scaling (0-10 instances) | ~$5-20 |
| Cloud Storage | ExamApp installer hosting | ~$1 |
| **Total** | | **~$56-100/month** |

---

### Building the ExamApp Installer

The ExamApp uses Inno Setup for creating Windows installers.

#### Prerequisites
- [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- .NET 8.0 SDK

#### Build Steps

```powershell
cd PeP.ExamApp\Installer

# Build with version
.\build-installer.ps1 -Version "1.0.0"

# Output: Output\PeP.ExamApp_Setup_1.0.0.exe
```

#### Installer Features
- Silent installation (`/SILENT` or `/VERYSILENT`)
- Custom installation directory
- Desktop shortcut (optional)
- Start menu group
- Auto-update support
- Clean uninstall

---

## �📖 Usage Guide

### 👨‍💼 Administrator Guide

#### Managing Teachers

1. Navigate to **Admin** → **Teachers**
2. Click **Add Teacher** to create a new teacher account
3. Fill in the required information (name, email, password)
4. Assign courses and permissions

#### Viewing Platform Reports

1. Navigate to **Admin** → **Reports**
2. View platform-wide statistics:
   - Total users, exams, and attempts
   - Average scores and completion rates
   - Top-performing students

#### Configuring Settings

1. Navigate to **Admin** → **Settings**
2. Configure:
   - Platform branding
   - Registration policies
   - Default exam settings

---

### 👨‍🏫 Teacher Guide

#### Creating a Course

1. Navigate to **Courses** in the sidebar
2. Click **Create Course**
3. Enter course details (name, description, code)
4. Click **Save**

#### Creating an Exam

1. Navigate to **Exams** → **Create Exam**
2. Fill in exam details:
   - Title and description
   - Duration (in minutes)
   - Scoring type (All-or-Nothing, Partial Credit, Single Correct)
   - Randomization options
3. Add questions:
   - Click **Add Question**
   - Enter question text
   - Add answer choices
   - Mark correct answer(s)
   - Set point value
4. Click **Save Exam**

#### Creating a Programming Exam (NEW)

1. Navigate to **Exams** → **Create Programming Exam**
2. Fill in exam details:
   - Title, description, and project name
   - Select course and programming language (Python, Java, JavaScript, C#)
   - Set duration (10-300 minutes)
   - Total points allocation
3. **Option A - Generate with AI:**
   - Click **Generate with AI**
   - Enter a topic description (e.g., "Data analysis with CSV files and pandas")
   - Select difficulty level
   - AI generates complete project with tasks and starter files
4. **Option B - Create Manually:**
   - Add starter files (CSV data, initial code templates)
   - Create tasks with:
     - Task title and instructions (Markdown supported)
     - Point value
     - Optional hints
     - Target files (which files students should edit)
5. Review and click **Save Exam**

#### Grading Programming Exams

1. Navigate to **Exams** → select a Programming Exam
2. Click **View Submissions** to see all attempts
3. For each submission:
   - View AI-generated scores (Quality, Correctness, Efficiency)
   - Read detailed feedback and code snippets
   - Override scores manually if needed
   - Click **Re-evaluate with AI** to regenerate feedback
4. Click **Save Grades** to finalize

#### Generating Exam Codes

1. Navigate to **Exams** → **Exam Codes**
2. Click **Generate Code**
3. Configure:
   - Expiration date/time
   - Maximum uses (optional)
   - Description/label
4. Share the code with students

#### Managing Students

1. Navigate to **Students**
2. Options:
   - **Add Student**: Create individual accounts
   - **Import Students**: Bulk import via CSV

##### CSV Import Format

```csv
Email,FirstName,LastName,Password
john@example.com,John,Doe,SecurePass123!
jane@example.com,Jane,Smith,SecurePass456!
```

#### Viewing Results

1. Navigate to **Reports**
2. Select an exam to view:
   - Attempt statistics
   - Score distribution
   - Individual student results

---

### 👨‍🎓 Student Guide

#### Taking an Exam (Web Browser)

1. Log in to the PeP platform
2. Navigate to **Take Exam**
3. Enter the exam code provided by your teacher
4. Click **Start Exam**
5. Answer all questions
6. Click **Submit Exam** when finished

#### Taking an Exam (ExamApp - Secure Mode)

1. Launch **PeP ExamApp**
2. Enter the server URL (provided by your institution)
3. Log in with your student credentials
4. Enter the exam code
5. Enter the teacher password (provided by your teacher)
6. Review the security tutorial
7. Pass the security check
8. Take the exam in the secure environment

##### Security Requirements

- ✅ Run as Administrator
- ✅ Single monitor only
- ✅ No virtual machines
- ✅ No remote desktop
- ✅ Close all forbidden applications

##### Exiting the Secure Exam

- Click **Exit Exam** in the taskbar
- Enter the teacher password
- The exam will be submitted automatically
- Results will open in your default browser

#### Taking a Programming Exam (NEW)

1. **Launch via PeP ExamApp** (required for secure mode)
2. Enter exam code and teacher password
3. **Using the IDE:**
   - **Solution Explorer** (left): Navigate between files
     - 📁 **Starter Files**: Read-only data files (CSV, etc.)
     - 📄 **Your Files**: Editable code files
   - **Code Editor** (center): Write your code with:
     - Syntax highlighting
     - Auto-completion
     - Error indicators
   - **Task Panel** (right): View current task instructions
     - Use the **splitter bar** to resize panels
     - Navigate between tasks using task buttons
     - View hints if available
   - **Console** (bottom): View program output
4. **Complete each task:**
   - Read instructions in the Task Navigator
   - Edit the target files as specified
   - Use hints if you're stuck
   - Progress is auto-saved periodically
5. **Submit exam:**
   - Click **Submit Exam** when finished
   - Confirm submission in the dialog
   - Wait for AI evaluation

#### Viewing Programming Exam Results (NEW)

1. After submission, you'll see the **AI Evaluation Report**:
   - **Final Grade**: Overall percentage with points earned
   - **Score Gauges**: Visual indicators for Code Quality, Correctness, and Efficiency
2. **Task-by-Task Feedback**:
   - Completion percentage and points for each task
   - Progress bars for Quality, Correctness, Efficiency
   - **Strengths**: What you did well
   - **Areas for Improvement**: Suggestions for better code
   - **Code Snippets**: Specific excerpts with inline feedback
3. Click **Download Code** to get a ZIP of your submission

#### Viewing Results

1. Navigate to **My Results**
2. Click on any exam to view:
   - Your score and percentage
   - Time spent
   - Question-by-question breakdown
   - Correct answers (if enabled by teacher)

---

## 🔒 Security Features

### Web Application Security

| Feature | Implementation |
|---------|----------------|
| **Authentication** | ASP.NET Core Identity with secure password hashing |
| **Authorization** | Role-based access control (Admin, Teacher, Student) |
| **CSRF Protection** | Anti-forgery tokens on all forms |
| **XSS Prevention** | Automatic HTML encoding in Blazor |
| **SQL Injection** | Parameterized queries via EF Core |
| **HTTPS** | TLS encryption for all communications |

### ExamApp Security

| Feature | Implementation |
|---------|----------------|
| **Keyboard Hook** | Low-level keyboard hook (WH_KEYBOARD_LL) blocks dangerous shortcuts |
| **Focus Monitor** | WinEvent hook detects focus changes |
| **Display Affinity** | WDA_EXCLUDEFROMCAPTURE prevents screen capture |
| **Process Detection** | 80+ blacklisted processes (recording, remote access, cheats) |
| **VM Detection** | WMI queries, registry checks, MAC address analysis |
| **Debugger Detection** | IsDebuggerPresent, CheckRemoteDebuggerPresent, NtQueryInformationProcess |
| **Clipboard Control** | Periodic clipboard clearing |
| **Taskbar Hiding** | Shell_TrayWnd manipulation |

### Blocked Keyboard Shortcuts

| Shortcut | Reason |
|----------|--------|
| `Win` | Start menu access |
| `Alt+Tab` | Window switching |
| `Alt+F4` | Close application |
| `Alt+Esc` | Window cycling |
| `Ctrl+Esc` | Start menu |
| `Ctrl+Shift+Esc` | Task Manager |
| `Ctrl+Alt+Del` | (Cannot be blocked, but logged) |
| `PrintScreen` | Screenshots |
| `Win+PrintScreen` | Screenshots |

### Blacklisted Applications

<details>
<summary>Click to expand full list</summary>

**Screen Recording:**
- OBS Studio
- Bandicam
- Camtasia
- Fraps
- ShareX
- NVIDIA ShadowPlay
- AMD ReLive
- Xbox Game Bar

**Remote Access:**
- TeamViewer
- AnyDesk
- Chrome Remote Desktop
- VNC variants
- Parsec
- RustDesk

**Communication:**
- Discord
- Zoom
- Microsoft Teams
- Slack
- Skype

**Debugging/Cheating:**
- Cheat Engine
- x64dbg
- OllyDbg
- IDA Pro
- Wireshark
- Fiddler

</details>

---

## 🔌 API Documentation

### ExamApp API Endpoints

#### Get Exam Info
```http
GET /api/exam-app/code/{code}
```

**Response:**
```json
{
  "success": true,
  "exam": {
    "examId": 1,
    "examCodeId": 1,
    "examTitle": "Midterm Exam",
    "courseName": "Computer Science 101",
    "durationMinutes": 60,
    "teacherName": "Dr. Smith"
  }
}
```

#### Authorize Exam Launch
```http
POST /api/exam-app/authorize
Content-Type: application/json

{
  "code": "ABC123",
  "teacherPassword": "teacher-password"
}
```

**Response:**
```json
{
  "success": true,
  "authorizationToken": "eyJ...",
  "expiresAtUtc": "2025-12-20T12:00:00Z",
  "exam": { ... }
}
```

#### Start Exam
```http
POST /api/exam-app/start
Content-Type: application/json

{
  "authorizationToken": "eyJ..."
}
```

**Response:**
```json
{
  "success": true,
  "attemptId": 42,
  "launchToken": "xyz...",
  "expiresAtUtc": "2025-12-20T13:00:00Z"
}
```

#### Submit Exam
```http
POST /api/exam-app/submit
Content-Type: application/json

{
  "attemptId": 42
}
```

**Response:**
```json
{
  "success": true
}
```

---

## 🗄️ Database Schema

### Core Entities

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  ApplicationUser│     │     Course      │     │      Exam       │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ Id              │     │ Id              │     │ Id              │
│ Email           │────►│ Name            │◄────│ Title           │
│ FirstName       │     │ Description     │     │ Description     │
│ LastName        │     │ Code            │     │ Duration        │
│ Role            │     │ TeacherId       │     │ CourseId        │
└─────────────────┘     └─────────────────┘     │ ScoringType     │
                                                │ IsRandomized    │
                                                └────────┬────────┘
                                                         │
                        ┌─────────────────┐              │
                        │    Question     │◄─────────────┘
                        ├─────────────────┤
                        │ Id              │
                        │ Text            │
                        │ Points          │
                        │ ExamId          │
                        └────────┬────────┘
                                 │
                        ┌────────▼────────┐
                        │ QuestionChoice  │
                        ├─────────────────┤
                        │ Id              │
                        │ Text            │
                        │ IsCorrect       │
                        │ QuestionId      │
                        └─────────────────┘

┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   ExamAttempt   │     │  StudentAnswer  │     │    ExamCode     │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ Id              │────►│ Id              │     │ Id              │
│ StudentId       │     │ AttemptId       │     │ Code            │
│ ExamId          │     │ QuestionId      │     │ ExamId          │
│ StartedAt       │     │ SelectedChoices │     │ ExpiresAt       │
│ SubmittedAt     │     │ PointsEarned    │     │ MaxUses         │
│ TotalScore      │     └─────────────────┘     │ TimesUsed       │
│ Status          │                             │ IsActive        │
└─────────────────┘                             └─────────────────┘
```

### Programming Exam Entities (NEW)

```
┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
│  ProgrammingExam    │     │  ProgrammingTask    │     │  TaskTestCase       │
├─────────────────────┤     ├─────────────────────┤     ├─────────────────────┤
│ Id                  │     │ Id                  │     │ Id                  │
│ Title               │────►│ Title               │────►│ Name                │
│ Description         │     │ Instructions        │     │ Input               │
│ ProjectName         │     │ Order               │     │ ExpectedOutput      │
│ Language (Python,   │     │ Points              │     │ IsHidden            │
│  Java, JS, C#)      │     │ Hint                │     │ TaskId              │
│ DurationMinutes     │     │ TargetFiles         │     └─────────────────────┘
│ TotalPoints         │     │ ProgrammingExamId   │
│ CourseId            │     └─────────────────────┘
│ StarterFilesJson    │
└─────────────────────┘

┌─────────────────────┐     ┌─────────────────────┐
│ProgrammingExamAttempt     │  TaskProgress       │
├─────────────────────┤     ├─────────────────────┤
│ Id                  │     │ Id                  │
│ StudentId           │────►│ AttemptId           │
│ ProgrammingExamId   │     │ TaskId              │
│ Status (InProgress, │     │ StudentCode (JSON)  │
│  Completed, etc.)   │     │ CompletionPercentage│
│ StartedAt           │     │ AIScore             │
│ SubmittedAt         │     │ CodeQualityScore    │
│ TotalScore          │     │ CorrectnessScore    │
│ AIEvaluationCompleted     │ EfficiencyScore     │
│ StudentFilesJson    │     │ AIFeedback          │
└─────────────────────┘     │ AIStrengths         │
                            │ AIImprovements      │
                            │ AICodeSnippets (JSON)│
                            └─────────────────────┘
```

---

## 🔧 Troubleshooting

### Common Issues

#### Web Application

<details>
<summary><strong>Database connection failed</strong></summary>

1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. Ensure the database exists or run migrations:
   ```bash
   dotnet ef database update
   ```
</details>

<details>
<summary><strong>Login not working</strong></summary>

1. Check if the user exists in the database
2. Verify password meets requirements (8+ chars, uppercase, lowercase, digit, special)
3. Check for account lockout after failed attempts
</details>

<details>
<summary><strong>Radzen components not rendering</strong></summary>

1. Ensure `_Imports.razor` includes `@using Radzen` and `@using Radzen.Blazor`
2. Verify `RadzenComponents` is added in `_Layout.cshtml`
3. Clear browser cache and restart
</details>

#### Programming Exams (NEW)

<details>
<summary><strong>AI evaluation not completing</strong></summary>

1. Verify OpenAI API key is configured in **Admin** → **Settings**
2. Check that the API key has sufficient credits
3. Ensure the model (e.g., `gpt-4o-mini`) is available in your OpenAI account
4. Check server logs for API errors
</details>

<details>
<summary><strong>Monaco editor not loading</strong></summary>

1. Clear browser cache and hard refresh (Ctrl+Shift+R)
2. Ensure JavaScript is enabled
3. Check browser console for errors
4. Verify `_framework/blazor.server.js` is loading
</details>

<details>
<summary><strong>CSV files not displaying in tabs</strong></summary>

1. Ensure CSV files are added as starter files (read-only)
2. Click on the file in Solution Explorer to view content
3. Tab switching should update content automatically
</details>

<details>
<summary><strong>Exam evaluation shows blank page</strong></summary>

1. Ensure the exam was submitted successfully
2. Wait for AI evaluation to complete (may take 15-30 seconds)
3. If stuck on loading, refresh the page - cached results will display
</details>

#### ExamApp

<details>
<summary><strong>VM Detection false positive</strong></summary>

If running on a physical machine with Hyper-V enabled:
- This is expected behavior
- Disable Hyper-V features if not needed
- Or modify `SecurityChecks.cs` to adjust detection sensitivity
</details>

<details>
<summary><strong>Keyboard not working in password dialog</strong></summary>

This was fixed in recent updates. Ensure you have the latest version with `SuspendHooks()` implementation.
</details>

<details>
<summary><strong>WebView2 not loading</strong></summary>

1. Install [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
2. Check if Edge browser is installed
3. Verify internet connectivity
</details>

<details>
<summary><strong>App requires administrator</strong></summary>

The ExamApp requires administrator privileges for:
- Hiding the Windows taskbar
- Installing keyboard hooks
- Protecting against tampering

Right-click → Run as Administrator
</details>

### Getting Help

- 📧 Email: support@pep.edu
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/PeP/issues)
- 📖 Wiki: [GitHub Wiki](https://github.com/yourusername/PeP/wiki)

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/AmazingFeature`)
3. **Commit** your changes (`git commit -m 'Add some AmazingFeature'`)
4. **Push** to the branch (`git push origin feature/AmazingFeature`)
5. **Open** a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation as needed
- Keep commits atomic and well-described

### Code Style

```csharp
// Use meaningful names
public async Task<ExamAttempt> GetExamResultAsync(int attemptId, string userId)

// Use async/await properly
var result = await _context.ExamAttempts.FindAsync(attemptId);

// Handle nulls explicitly
if (result is null)
    throw new NotFoundException($"Attempt {attemptId} not found");
```

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

```
MIT License

Copyright (c) 2025 PeP Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 🙏 Acknowledgments

- [Radzen Blazor](https://blazor.radzen.com/) - UI Components
- [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) - Embedded Browser
- [Entity Framework Core](https://docs.microsoft.com/ef/core/) - ORM
- [ASP.NET Core Identity](https://docs.microsoft.com/aspnet/core/security/authentication/identity) - Authentication

---

<p align="center">
  Made with ❤️ by Infinity Atom
</p>

<p align="center">
  <a href="#-pep---professional-exam-platform">Back to Top ⬆️</a>
</p>
