# AI Persona System

> **Author:** Marco Siccardi (MSiccDev Software Development)  
> **Purpose:** A structured framework for maintaining consistent AI collaboration across different LLM providers, projects, and development phases.

---

## Overview

This repository provides a **comprehensive AI persona and context management system** designed to enable consistent, context-aware AI collaboration across multiple projects and platforms.

The system consists of:
- **Personal persona definitions** that capture your professional background, skills, preferences, and working style
- **Project-specific context files** that define scope, tech stack, roles, and current objectives for each active project
- **Specification documents** that define how AI assistants should maintain and adapt context during work sessions
- **Templates** for creating new persona files and project contexts

All components are designed to work seamlessly across different LLM environments (Anthropic Claude, Mistral, Gemini, LM Studio, Ollama, etc.), ensuring that every AI assistant understands your background, working style, and project context without repeated explanations.

---

## Repository Structure

```
persona/
│
├── README.md                          # This file
│
├── projects/
│   └── project1_prompt.md             # Example project-specific context
│
├── specs/
│   └── context_aware_ai_session_spec.md  # Specification for AI session management
│
└── templates/
   ├── persona_template.md            # Template for creating personal personas
   └── project_prompt_template.md     # Template for project-specific contexts
```

---

## Core Concepts

### 1. **Personal Persona**
Your foundational AI context that includes:
- Professional background and current role
- Technical skills and expertise areas
- Active projects and goals
- Preferred working style and communication preferences
- Constraints and learning journey

**Purpose:** Serves as the base layer that AI assistants load first to understand who you are and how you work.

### 2. **Project Contexts**
Project-specific extensions that define:
- Project scope and objectives
- Technology stack and architecture
- Recommended AI roles (Architect, Developer, Designer, etc.)
- Default work phases (Planning, Implementation, Debugging, Review)
- Project-specific constraints and guidelines

**Purpose:** Provides focused context for AI to assist with specific projects effectively.

### 3. **Session State Model**
A structured approach to AI collaboration that manages:
- **Role/Mode:** The cognitive stance the AI should take (e.g., Architect, Developer, Reviewer)
- **Phase:** Current work stage (Planning, Implementation, Debugging, Review)
- **Output Style:** Verbosity and formatting preferences
- **Tone:** Communication voice and attitude
- **Interaction Mode:** How proactive the AI should be (Advisory, Pair-programming, Driver)

**Purpose:** Ensures AI behavior adapts appropriately as you move through different stages of work.

---

## Quick Start

### For First-Time Users

1. **Create Your Persona:**
   - Copy `templates/persona_template.md`
   - Fill in your professional details, skills, projects, and preferences
   - Save as `yourname_persona.md` in the root folder

2. **Create Project Contexts:**
   - Copy `templates/project_prompt_template.md` for each active project
   - Define default roles, phases, and project-specific guidelines
   - Save in `projects/` folder with descriptive names

3. **Load Into Your AI Environment:**
   - See platform-specific instructions below

### For Returning Users

- Load your persona file as the base context
- Add the relevant project context on top
- The AI will maintain state across your work session

---

## Loading Context in Different AI Platforms

| Platform | Method |
|----------|---------|
| **Anthropic Claude Projects** | Paste persona + project context into the *Project Knowledge* or *Custom Instructions* field |
| **GitHub Copilot Edits** | Reference persona file in workspace; Copilot reads it automatically |
| **LM Studio / Ollama** | Save `.md` files as system prompts or context presets |
| **OpenAI ChatGPT** | Paste into *Custom Instructions* or upload as a file |
| **Gemini** | Paste into chat or use as system instruction |
| **Local scripts / APIs** | Concatenate persona + project prompt when initializing conversations |
| **IDE integrations** | Reference files in config or load via custom extensions |

---

## Key Principles

### Session State Management

The system follows a **deterministic session model** where AI behavior is governed by:

| Element | Description | Example Values |
|---------|-------------|----------------|
| **Persona** | Your identity, skills, and preferences | Defined in your persona file |
| **Project** | Active domain or codebase | "Mobile UI app", "Backend API" |
| **Role/Mode** | AI's cognitive stance | Architect, Developer, Designer, Reviewer |
| **Phase** | Current work stage | Planning, Implementation, Debugging, Review |
| **Output Style** | Response verbosity | Step-by-step, Minimal code, Annotated |
| **Tone** | Communication voice | Analytical, Direct, Encouraging |
| **Interaction Mode** | AI proactivity level | Advisory, Pair-programming, Driver |

### Context Control

You can modify session state using:

- **Natural language:** "Switch to Developer Mode" or "Move to Implementation Phase"
- **Commands:** `/mode developer`, `/phase implementation`, `/context` (shows current state)
- **Project defaults:** Each project can define typical starting configurations

### Core Principles

- **Determinism:** Same context + same query = consistent responses
- **Explicitness:** AI confirms context changes rather than assuming
- **Continuity:** Session state persists across conversation turns
- **Reversibility:** All context changes can be undone
- **Transparency:** Current context is always visible on request

---

## Documentation

### For Users
- `templates/persona_template.md` - Template for creating your personal AI persona with detailed instructions
- `templates/project_prompt_template.md` - Template for creating project-specific contexts

### For Developers & AI Engineers
- `specs/context_aware_ai_session_spec_v1.md` - Complete specification for implementing context-aware AI sessions
  - Session state model
  - Context initialization flows
  - State transition rules
  - Command interface definitions
  - Example scenarios

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

### Creating Project Contexts

Each project context should define:
- Project description and tech stack
- Supported AI roles for this project
- Default role and phase
- Phase-specific guidelines
- Output preferences
- Constraints and special considerations

---

## Conventions

- **File format:** UTF-8 Markdown
- **Naming:** lowercase with underscores (`project_name_prompt.md`)
- **Structure:** Consistent headings and sections across all files
- **Languages:** Technical content in English; adapt as needed
- **Versioning:** Update persona when skills/preferences evolve; update project contexts when phases change

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

## Contributing

This is a template repository designed to be:
- Forked or cloned for your own AI persona system
- Customized with your personal and project details
- Extended with additional templates and specifications
- Adapted for team or organizational workflows

To get started:
1. Fork this repository - make it private (recommended for personal data!)
2. Replace the example content with your own details
3. Add your projects and customize templates as needed
4. Consider contributing improvements back to the template

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

**Note:** While the templates and specifications are open source, your personal persona files should remain private and not be shared without redacting sensitive information.

---

## Author

**Marco Siccardi** – MSiccDev Software Development

---

## Acknowledgments

This system was designed to solve the challenge of maintaining consistent AI collaboration across multiple platforms and projects. It represents lessons learned from extensive work with various LLM providers and real-world development workflows.

