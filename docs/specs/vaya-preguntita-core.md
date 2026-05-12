# Spec: Vaya Preguntita - Core Architecture & Question Types

## 1. Product Overview

**Features:**

- Users can add questions to a pool.
- Daily selection: One participant chooses the next day's question from the unasked pool.
- Priority system: Ability to prioritize specific questions or increase the daily question limit.
- Gamification: Users guess the author of the question.
- Dynamics: Team vs. Team questions.

## 2. Architecture Stack

### 2.1. Frontend (The Client)

- **Tech:** Angular (v19+), TypeScript.
- **Role:** UI state management, client-side validation, API consumption.
- **Hosting:** Vercel / Netlify (CI/CD, CDN).

### 2.2. Backend (The Server)

- **Tech:** .NET 9 Web API, C# 13.
- **Architecture:** MVC (Model-View-Controller) for endpoints. RESTful API (JSON).
- **Hosting:** Render (Free Web Service) + UptimeRobot for 24/7 availability.

### 2.3. Data Layer (The Memory)

- **ORM:** Entity Framework Core (Code-First approach).
- **Database:** PostgreSQL.
- **Hosting:** Supabase (Europe region for low latency & data compliance).

## 3. Technical Specification: Question Types

### 3.1. The Superlative (Group Poll)

// Dynamic nature: Do not store user names in the DB when creating the question.

- **Description:** "Who is most likely to end up in jail?" -> 1 Person selection.
- **Data Requirements:**
  - `AllowNobody` (bool): If true, inject "Nobody" option.
  - `BlacklistedUserIds` (List<int>): Users excluded from being selected.
- **Constraints:** Server must validate `SelectedUserId` against the blacklist upon response submission.
- **Voting Payload:** Use `SelectedTargetUserId`, with `0` as the sentinel value for "Nobody" when `AllowNobody` is true.

### 3.2. The Deathmatch (2v2 / 1v1)

// Static nature: Participants chosen at creation time.

- **Description:** "Deathmatch: [Juan & Marta] vs [Luis & Ana]. Who wins?"
- **Data Requirements:**
  - `Metadata`: JSON field or relational table for teams.
  - _Suggested CreateQuestionDto Structure:_ `Teams: [[ID1, ID2], [ID3, ID4]]`.
- **Constraints:** `CreateQuestionDto` must validate that users are not repeated across opposing teams.
- **Voting Payload:** Use `SelectedTargetUserIds` and match exactly one of the configured teams.

### 3.3. The Scale (Subjective Rating)

- **Description:** "From 1 to 10, how crazy is Aguacate today?"
- **Logistics:** Numeric range applied to a text prompt.
- **Data Requirements:**
  - `TargetUserId` (int): User being evaluated.
  - `RangeMin` / `RangeMax`: Defaults 1 and 10.
- **Constraints:** Response is an integer `Value`, not an Option ID. The `Response` entity must be flexible.

### 3.4. The Secret Pairing (Matchmaking)

- **Description:** "Which two people in the group would make the best couple?"
- **Logistics:** Dynamic (all group members).
- **Data Requirements:** `MinSelections`: 2, `MaxSelections`: 2.
- **Constraints:** Response DTO must accept a list of IDs, unlike The Superlative.

### 3.5. The Custom Poll (Classic Poll)

- **Description:** "What time are we meeting for dinner?"
- **Logistics:** Static, defined by creator.
- **Data Requirements:** List of strings in `CreateAnswerDto.Text`.
- **Selection Limits:** `MinSelections` and `MaxSelections` define whether the poll is single- or multi-select.

## 4. Development Guidelines

// Do not implement the final business logic yet.
// Generate only the interfaces, DTOs, and entity structures first.
// Ensure strict typing for all API models and follow Clean Architecture.
// Write all code comments and XML documentation strictly in English.
