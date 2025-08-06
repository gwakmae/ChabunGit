# ChabunGit

A simple Git GUI client for Windows, built with C# and WPF, featuring a powerful AI-assisted commit message generator.

*한글 설명: AI 커밋 메시지 생성 기능이 포함된, C#과 WPF로 만들어진 간단한 Git GUI 클라이언트입니다.*


> **Note:** Please replace the image link above with a real screenshot of your application! A GIF showing the workflow would be even better.

## About The Project

ChabunGit was created to simplify the Git workflow for developers who may find the command line interface challenging or time-consuming. It provides essential Git functionalities in a clean, intuitive graphical interface.

The standout feature of ChabunGit is its **AI-Assisted Commit Message Generator**. Instead of struggling to write conventional commits, you can analyze your changes and generate a perfect, ready-to-use prompt for your favorite AI model (like ChatGPT, Gemini, etc.). This helps you maintain a clean and meaningful commit history with minimal effort.

### Built With

*   [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
*   [WPF (Windows Presentation Foundation)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
*   [MVVM Design Pattern (CommunityToolkit.Mvvm)](https://docs.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
*   [Microsoft Extensions for Dependency Injection & Hosting](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

## Key Features

*   **Core Git Operations:** `Fetch`, `Pull`, and `Push` your changes with ease.
*   **Repository Management:**
    *   Easily open any project folder.
    *   Initialize a new Git repository (`git init`).
    *   Connect to a remote repository (`git remote add`).
*   **Commit Workflow:**
    *   View staged and unstaged changes.
    *   Craft commit messages with a title and body.
    *   Title character limit checker.
*   **History & Rollback:**
    *   View a clean list of your commit history.
    *   Double-click a commit to see its full details (`git show`).
    *   Undo the last commit (`git reset --soft`).
    *   Reset the entire project to a specific past commit (`git reset --hard`).
*   **✨ AI-Powered Commit Prompts:**
    *   Analyze all code changes with a single click (`git diff`).
    *   Generate a structured, detailed prompt based on the changes.
    *   Copy the prompt and use it with any AI to get a perfect commit message.
*   **.gitignore Management:**
    *   Create or edit the `.gitignore` file directly within the app.
    *   Generate an AI prompt to create a `.gitignore` file tailored to your project.
*   **Real-time Logging:** See the output of every Git command the application runs.

## Getting Started

To get a local copy up and running, follow these simple steps.

### Prerequisites

*   **Git:** Must be installed and accessible from your system's PATH.
    *   [https://git-scm.com/downloads](https://git-scm.com/downloads)
*   **Visual Studio 2022:** With the ".NET desktop development" workload installed.
    *   [https://visualstudio.microsoft.com/](https://visualstudio.microsoft.com/)

### Installation & Running

1.  Clone the repo:
    ```sh
    git clone https://github.com/gwakmae/ChabunGit.git
    ```
2.  Open the solution file `ChabunGit.sln` in Visual Studio.
3.  Press `F5` or click the "Start" button to build and run the project.

## Usage

Here is the primary workflow for creating a high-quality commit with ChabunGit:

1.  **Select Folder:** Launch the app and select your project folder.
2.  **Make Changes:** Write your code in your favorite editor. The changes will automatically appear in the "Changed Files" list.
3.  **Analyze Changes:** Click the **`변경점 분석`** (Analyze Changes) button.
4.  **Generate AI Prompt:** A new window will pop up showing the `git diff` output. In this window, click the **`✨ AI 프롬프트 생성`** (Generate AI Prompt) button.
5.  **Copy Prompt:** The window's content will transform into a detailed prompt. Click the copy button to copy it to your clipboard.
6.  **Get AI Message:** Paste the prompt into your AI service of choice (ChatGPT, Gemini, etc.) and get a well-written commit message.
7.  **Commit:** Paste the title and body of the AI-generated message into the commit fields in ChabunGit and click **`커밋`** (Commit).
8.  **Push:** Click **`✨ GitHub에 공유 (Push)`** to share your work!


## Contact


Project Link: [https://github.com/gwakmae/ChabunGit](https://github.com/gwakmae/ChabunGit)