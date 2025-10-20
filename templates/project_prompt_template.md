# Project Context – <Project Name>

> **Purpose:** Define the AI assistant's role and working context for this project.  
> Use this file together with your global persona (`yourname_persona.md`).

---

## Overview

**<Project Name>** is a <short description: what it does, who it's for>.  
Current phase: **<planning / prototype / active development / maintenance / MVP / release>**.  
Primary goals: **<list 2–4 short goals>**.

---

## Tech Stack

- **Language(s):** <e.g., Swift, C#, TypeScript>
- **Frameworks:** <e.g., SwiftUI, .NET, React>
- **Architecture:** <e.g., MVVM, Clean Architecture, TDD>
- **Persistence / Storage:** <e.g., SwiftData, SQLite, Filesystem>
- **APIs / Integrations:** <e.g., FeedKit, HealthKit, Fediverse APIs>
- **Build / CI:** <e.g., GitHub Actions, TeamCity>
- **Documentation:** Markdown in `/docs` (+ optional PlantUML/C4)

**Repository Structure (desired):**
```
src/
tests/
resources/
docs/
```
<Adjust as needed>

---

## Assistant Role Selector

Define one or more roles for the assistant and switch between them during work.

### Mode 1 – <Role Name>
Act as a <short role description>.  
Responsibilities:
- <responsibility 1>
- <responsibility 2>
- <responsibility 3>

### Mode 2 – <Role Name>
Act as a <short role description>.  
Responsibilities:
- <responsibility 1>
- <responsibility 2>
- <responsibility 3>

### (Optional) Additional Modes
Add as many modes as needed (UI/UX Designer, Tester, Automation Engineer, etc.).

You can explicitly select or combine roles (e.g., "use Architect + Developer mode").

---

## Current Objectives

- <objective 1>
- <objective 2>
- <objective 3>
- <objective 4>

---

## Development Principles

- <principle 1>  
- <principle 2>  
- <principle 3>  
- <principle 4>  

---

## Repository Notes

- Current branch: `<branch>`  
- Notable paths: `/src`, `/tests`, `/resources`, `/docs`  
- <anything special about setup, scripts, or environment>

---

## Interaction Preferences

When assisting with **<Project Name>**:
- Indicate the **active role** at the start of your response.  
- Prefer structured Markdown and code blocks.  
- Keep outputs **commit‑friendly** and incremental.  
- Provide rationale for architectural/testing decisions.  
- Align with platform-specific idioms where applicable.

---

### Example Prompts

- "Use **<Mode 1>** and outline <topic>."  
- "Switch to **<Mode 2>** and implement <component>."  
- "Propose a C4 diagram for <scope>."  
- "Generate unit tests for <class/module>."

---

## How to Use This Template

1. Copy this template to the `projects/` directory
2. Rename it to `[your_project_name]_prompt.md`
3. Fill in all the bracketed and placeholder sections with your project-specific information
4. Define at least 2-3 roles that make sense for your project
5. Update the template as your project evolves and phases change
6. Load this file together with your global persona file for complete AI context

---

© 2025 – Project Prompt Template
