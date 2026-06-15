# Quick Start Guide

This guide will help you get NAgent up and running in minutes.

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- A modern web browser (Chrome, Firefox, Edge, Safari)
- (Optional) [Git](https://git-scm.com/) for cloning the repository

## Step 1: Get the Code

### Option A: Clone from Git

```bash
git clone https://github.com/your-org/nagent.git
cd nagent
```

### Option B: Download ZIP

Download the latest release ZIP file and extract it to your preferred location.

## Step 2: Build the Project

```bash
# Restore NuGet packages
dotnet restore

# Build the entire solution
dotnet build
```

If the build succeeds, you should see output similar to:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Step 3: Run the Application

```bash
dotnet run --project src/NAgent.Api
```

The application will start and listen on:
- HTTP: `http://localhost:9527`

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:9527
```

## Step 4: Initial Setup

1. Open your browser and navigate to `http://localhost:9527`
2. You will be redirected to the **System Initialization** page
3. Fill in the form:
   - **Username**: Choose an admin username (e.g., `admin`)
   - **Email**: Your email address
   - **Password**: A strong password (minimum 6 characters)
4. Click **Initialize System**
5. The system will create the admin account and initialize the database

## Step 5: Log In

1. After initialization, you will be redirected to the **Login** page
2. Enter your admin credentials
3. Click **Log In**
4. You will be taken to the **Dashboard**

## Step 6: Configure LLM Provider

Before using the AI Agent, you need to configure at least one LLM provider:

1. In the dashboard sidebar, click **Model Management** (admin only)
2. Click **Add Provider**
3. Fill in the provider details:
   - **Name**: e.g., "OpenAI"
   - **Protocol Type**: OpenAI or Anthropic
   - **Base URL**: e.g., `https://api.openai.com`
   - **API Key**: Your API key from the provider
4. Click **Save**
5. The system will automatically fetch available models from the provider

### Supported Providers

| Provider | Protocol | Base URL Example |
|----------|----------|-----------------|
| OpenAI | OpenAI | `https://api.openai.com` |
| Anthropic | Anthropic | `https://api.anthropic.com` |
| Ollama (Local) | OpenAI | `http://localhost:11434` |
| Azure OpenAI | OpenAI | `https://your-resource.openai.azure.com` |

## Step 7: Create Your First Project

1. In the dashboard sidebar, click **Project Management**
2. Click **Create New Project**
3. Fill in:
   - **Project Name**: e.g., "My First Project"
   - **Description**: Optional project description
4. Click **Create**
5. The project will be created with its own workspace directory

## Step 8: Chat with the AI Agent

1. In the sidebar, click **AI Agent Chat**
2. Select your project from the dropdown
3. Type a message in the input box and press Enter or click **Send**
4. The AI will respond based on the selected LLM model

### Streaming Mode

For real-time responses, the chat interface uses Server-Sent Events (SSE) to stream the AI's response word by word.

## Step 9: Add Skills (Optional)

Skills extend the AI Agent's capabilities:

1. Create a Markdown file in the `skills/` directory:

```markdown
---
name: code-review
description: Review code for quality and bugs
category: development
version: 1.0.0
---

# Code Review Skill

## Overview
This skill helps review code for quality issues, bugs, and improvements.

## Tools
- code_linter
- security_scanner

## Examples
#### Example 1: Review Python Code
Input: ```python
def add(a, b):
    return a + b
```
Output: The code is simple and correct. Consider adding type hints.
```

2. Go to **Skills Management** in the dashboard
3. Click **Reload All Skills**
4. The new skill will be loaded and available to the Agent

## Step 10: Add Tools (Optional)

Tools provide executable capabilities to the Agent:

1. Create a YAML file in the `tools/` directory:

```yaml
name: weather_lookup
description: Get current weather for a location
category: utility
security_level: low

parameters:
  - name: city
    description: City name
    type: string
    required: true

execution:
  type: http
  endpoint: "https://api.weather.com/v1/current"
  http_method: GET
  timeout_seconds: 10
```

2. Restart the application to load new tools

## Common Tasks

### Switch LLM Model

1. Go to **Model Management**
2. Find the model you want to use
3. Click **Set as Default** or use the model switcher in the chat interface

### Manage Users (Admin)

1. Go to **User Management** in the sidebar
2. View all users, create new accounts, or manage existing ones
3. Toggle admin roles, activate/deactivate accounts, or reset passwords

### Clear Project Memory

1. Go to **Project Management**
2. Find your project
3. Memory can be cleared via the API or automatically managed based on expiration settings

## Troubleshooting

### Port Already in Use

If port 9527 is already in use, you can change it in `src/NAgent.Api/Properties/launchSettings.json`:

```json
"applicationUrl": "http://localhost:9527"
```

### Database Issues

If you encounter database errors:

```bash
# Delete the database file to start fresh
rm nagent.db

# Re-run the application
dotnet run --project src/NAgent.Api
```

### LLM Provider Connection Failed

- Verify your API key is correct
- Check that the base URL is accessible from your network
- For local providers (Ollama), ensure the service is running

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

## Next Steps

- Read the [Project Structure](PROJECT_STRUCTURE.md) to understand the codebase
- Check the [API Reference](README.md#api-reference) for programmatic access
- Explore the [Generation Report](GENERATION_REPORT.md) for design decisions

## Getting Help

- Check the [Issues](https://github.com/your-org/nagent/issues) page
- Review the logs in the `logs/` directory
- Enable detailed logging by setting `"Default": "Debug"` in `appsettings.json`
