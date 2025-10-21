# AI Persona & Instruction System

> **Author:** Marco Siccardi (MSiccDev Software Development)  
> **Purpose:** A structured instruction framework for maintaining consistent, context-aware AI collaboration across different LLM providers, projects, and development phases.

---

## Overview

This repository provides a **comprehensive AI instruction and workspace configuration system** designed to enable consistent, context-aware AI collaboration across multiple projects and platforms.

### The Evolution

What began as a way to extract and reuse prompts across AI providers has evolved into a sophisticated **instruction-based architecture** for AI collaboration:

- **Not just prompts** – These are persistent instruction sets that define working context
- **Layered architecture** – Personal persona + project-specific instructions create complete AI workspace configurations
- **Provider-agnostic** – Works seamlessly across different LLM environments

### The System

This framework consists of:
- **Personal persona instructions** – Your professional identity, skills, preferences, and working style
- **Project-specific instructions** – Scope, tech stack, roles, objectives, and guidelines per project
- **Session specification** – How AI assistants should maintain and adapt context during work sessions
- **Templates** – For creating new instruction sets quickly and consistently

All components work seamlessly across different LLM environments (Anthropic Claude, GitHub Copilot, Mistral, Gemini, LM Studio, Ollama, etc.), ensuring that every AI assistant understands your background, working style, and project context without repeated explanations.

---

## Repository Structure

```
persona/
│
├── README.md                          # This file
│
├── projects/
│   └── project1_project.instructions.md      # Example project-specific instructions
│
├── specs/
│   └── context_aware_ai_session_spec.md  # Specification for AI session management
│
└── templates/
   ├── persona_template.instructions.md     # Template for creating personal personas
   └── project_template.instructions.md     # Template for project-specific instructions
```

---

## Core Concepts

### The Instruction vs. Prompt Distinction

**Instructions** are persistent context and guidelines that define:
- WHO you are (persona)
- WHAT the project is (project context)
- HOW the AI should behave (roles, phases, preferences)

**Prompts** are your day-to-day requests within that instructed environment:
- "Create a new API endpoint"
- "Review this code for security issues"
- "Switch to Developer Mode and implement this feature"

This repository provides the **instruction layer** that makes your prompts more effective.

---

### 1. **Personal Persona Instructions**
Your foundational AI context that includes:
- Professional background and current role
- Technical skills and expertise areas
- Active projects and goals
- Preferred working style and communication preferences
- Constraints and limitations

**Purpose:** Serves as the base instruction layer that AI assistants load first to understand who you are and how you work globally.

### 2. **Project Instructions**
Project-specific instruction sets that define:
- Project scope and objectives
- Technology stack and architecture
- Recommended AI roles (Architect, Developer, Designer, etc.)
- Default work phases (Planning, Implementation, Debugging, Review)
- Project-specific constraints and guidelines

**Purpose:** Provides focused, project-specific instructions that layer on top of your persona to create complete working context.

### 3. **Session State Model**
A structured approach to AI collaboration that manages context dynamically:

| Element | Description | Example Values |
|---------|-------------|----------------|
| **Persona** | Your identity, skills, and preferences | Defined in your persona instructions |
| **Project** | Active domain or codebase | "Mobile UI app", "Backend API" |
| **Role/Mode** | AI's cognitive stance | Architect, Developer, Designer, Reviewer |
| **Phase** | Current work stage | Planning, Implementation, Debugging, Review |
| **Output Style** | Response verbosity | Step-by-step, Minimal code, Annotated |
| **Tone** | Communication voice | Analytical, Direct, Encouraging |
| **Interaction Mode** | AI proactivity level | Advisory, Pair-programming, Driver |

**Purpose:** Ensures AI behavior adapts appropriately as you move through different stages of work.

---

## Quick Start

### For First-Time Users

1. **Create Your Persona:**
   - Copy `templates/persona_template.instructions.md`
   - Fill in your professional details, skills, projects, and preferences
   - Save as `yourname_persona.instructions.md` in the root folder

2. **Create Project Instructions:**
   - Copy `templates/project_template.instructions.md` for each active project
   - Define default roles, phases, and project-specific guidelines
   - Save in `projects/` folder with descriptive names (e.g., `projectname_project.instructions.md`)

3. **Load Into Your AI Environment:**
   - See platform-specific instructions below

### For Returning Users

- Load your persona instructions as the base context
- Add the relevant project instructions on top
- The AI will maintain state across your work session

---

## Loading Context in Different AI Platforms

| Platform | Method |
|----------|---------|
| **Anthropic Claude Projects** | Paste persona + project instructions into the *Project Knowledge* or *Custom Instructions* field |
| **GitHub Copilot Edits** | Place `.instructions.md` files in workspace; Copilot automatically discovers and loads them |
| **LM Studio / Ollama** | Save `.instructions.md` files as system prompts or instruction presets |
| **OpenAI ChatGPT** | Paste into *Custom Instructions* or upload as a file |
| **Gemini** | Paste into chat or use as system instruction |
| **Local scripts / APIs** | Concatenate persona + project instructions when initializing conversations |
| **IDE integrations** | Reference `.instructions.md` files in config or load via custom extensions |

---

## How It Works

### Context Control

You can modify session state dynamically using:

- **Natural language:** "Switch to Developer Mode" or "Move to Implementation Phase"
- **Commands:** `/mode developer`, `/phase implementation`, `/context` (shows current state)
- **Project defaults:** Each project can define typical starting configurations

### Design Principles

- **Determinism:** Same context + same query = consistent responses
- **Explicitness:** AI confirms context changes rather than assuming
- **Continuity:** Session state persists across conversation turns
- **Reversibility:** All context changes can be undone
- **Transparency:** Current context is always visible on request

---

## Customization

### Creating Your Persona

The persona file includes both a **human-readable system prompt** and **machine-readable JSON metadata**, ensuring compatibility with various AI platforms.

Key sections to customize:
- About (role, location, ecosystem preferences)
- Projects (with platforms and status)
- Skills (categorized by domain)
- Goals and Constraints
- Preferred working style
- Current focus areas

### Creating Project Instructions

Each project instruction set should define:
- Project description and tech stack
- Supported AI roles for this project
- Default role and phase
- Phase-specific guidelines
- Output preferences
- Constraints and special considerations

---

## Conventions

- **File format:** UTF-8 Markdown
  - `.instructions.md` for both persona and project files (persistent context and guidelines)
  - Actual prompts/queries are what you ask the AI day-to-day within this instructed environment
- **Naming:** lowercase with underscores (e.g., `yourname_persona.instructions.md`, `projectname_project.instructions.md`)
- **Structure:** Consistent headings and sections across all files
- **Languages:** Technical content in English; adapt as needed
- **Versioning:** Update persona when skills/preferences evolve; update project instructions when phases change
- **Discoverability:** Semantic file extensions help AI tools identify and load the appropriate instructions automatically

---

## Benefits

### Consistency
- Same quality of AI assistance across different platforms
- No need to re-explain your background repeatedly

### Efficiency  
- AI understands your context from the start
- Faster onboarding when switching projects
- Less cognitive overhead managing AI interactions

### Adaptability
- AI behavior adjusts to your current work phase
- Easy to switch between different roles (planning, coding, reviewing)
- Context evolves with your projects

### Portability
- Works across multiple LLM providers
- Can be versioned and backed up
- Shareable with team members (with appropriate redactions)

---

## Getting Started with This Template

This is a **GitHub template repository**. Here's how to use it:

### Creating Your Own Instance

1. **Use the Template:**
   - Click the green **"Use this template"** button on GitHub
   - Choose **"Create a new repository"**
   - Give it a name (e.g., `my-ai-instructions` or `ai-workspace-config`)
   - **Make it Private** (recommended - contains personal information)
   - GitHub will create a fresh copy for you

2. **Customize Your Instance:**
   - Clone your new repository locally
   - Start with `templates/persona_template.instructions.md`
   - Fill in your professional details, skills, and preferences
   - Save as `yourname_persona.instructions.md` in the root
   - Create project instructions from `templates/project_template.instructions.md`
   - Save in `projects/` folder

3. **Keep It Updated:**
   - Update your persona as your skills evolve
   - Add new projects as you start them
   - Version control tracks your AI workspace evolution

### Why Use Template Instead of Fork?

- **Clean history:** Your repository starts fresh without this template's history
- **Private by default:** Easily make your instance private (recommended)
- **No upstream confusion:** It's your repository, not a fork
- **Your data, your control:** Personal instructions stay in your private repo

### Contributing Back

Found a bug or have an improvement to the **template itself**?

1. Create an issue in the [original template repository](https://github.com/MSiccDev/persona-template)
2. Submit a pull request with improvements to:
   - Template structure
   - Documentation clarity
   - Specification enhancements
   - Example improvements

**Note:** Never contribute your personal persona or project files - keep those private!

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Note:** While the templates and specifications are open source, your personal persona files should remain private and not be shared without redacting sensitive information.

---

## Author

**Marco Siccardi** – MSiccDev Software Development

---

## Acknowledgments

This instruction-based system evolved from the challenge of maintaining consistent AI collaboration across multiple platforms and projects. 

**The Evolution:**
- **Phase 1:** Started as a way to extract and reuse prompts across AI providers
- **Phase 2:** Evolved into structured, persistent context management
- **Phase 3:** Matured into a complete instruction-based architecture for AI workspace configuration

What makes this approach powerful is the shift from treating every AI interaction as isolated to creating **persistent, layered instruction sets** that transform how AI assistants understand and support your work.

This represents lessons learned from extensive work with various LLM providers (Anthropic Claude, GitHub Copilot, OpenAI, Mistral, Gemini, and local models) and real-world development workflows across multiple projects and domains.

---

## Philosophy

**Traditional approach:**
- User sends isolated prompts
- AI has no continuity between sessions
- Constant re-explanation of context
- Inconsistent results across providers

**Instruction-based approach:**
- User loads instruction sets once
- AI maintains persistent understanding
- Context builds and evolves naturally
- Consistent collaboration regardless of provider

This isn't just about efficiency—it's about creating a fundamentally different relationship between developers and AI assistants, where the AI becomes a true collaborative partner rather than a stateless tool.

