# Phase 3 — University Gameplay

Brief: student profile, timetable, lecture attendance, course registration, assignments, examinations, GPA calculations and first-semester progression. Lectures happen physically in lecture halls, where you find a seat, attend, take notes and answer short challenges. Attendance helps preparation but does not guarantee good grades.

## What is implemented

### Curriculum (`Core/Content/Phase3Content.cs`)
- **24 Year 1, Semester 1 courses** (3 credits each):
  - Two university-wide required courses: AMU 101 Academic Writing I and AMU 103 Introduction to African Studies.
  - Two electives: AMU 105 Critical Thinking and AMU 107 Introduction to French.
  - Two core courses for each of the ten programmes, for example CSC 101 Introduction to Computing and CSC 103 Programming Fundamentals, or NUR 101 Human Anatomy I and NUR 103 Foundations of Nursing.
- **Structure:** every course has a lecturer and four weekly topics. Each topic has two key points, and each key point has a multiple-choice question (8 per course, 192 in total).
- **Prerequisites:** the data model and rules support them; they're tested with sample courses, but no Semester 1 course needs one.

### Academic rules (`Core/Academics`)
- **Grading:** a configurable scale (A 80+, B+ 75, B 70, C+ 65, C 60, D+ 55, D 50, F below 50, on a 4.0 scale) with a pass mark of 50.
- **GPA and CGPA:** credit-weighted, with class of degree. Academic probation applies if your CGPA falls below 1.50, and failed courses are listed as resits.
- **Course mark:** attendance and participation 10%, assignment 20%, mid-semester quiz 20%, final exam 50%.
- **Calendar:** orientation and registration week, four teaching weeks, an exam week, then results on day 42.
- **Registration:**
  - Open until the end of teaching week 1, once per semester.
  - All required courses must be included; 15–18 credits; only courses offered to your programme; no duplicates; prerequisites checked.
- **Timetable:** clash-free and deterministic. Each course meets once a week for two hours in LT1 or LT2. One exam per course in exam week.
- **Attendance:**
  - On time up to 10 minutes after the start, late up to 30 minutes (half credit), and you can sit down and wait from an hour before.
  - Participation is the lecture challenge answered correctly.
- **Notes:** taken during lectures, or copied from the course material by studying a topic in the library (an hour of game time).
- **Preparation:** studying builds preparation (up to 8). Every 2 points give one 50/50 hint in that course's exam.
- **Assignments:** set from teaching week 1 and due at the end of week 3. Late work loses 10 marks per day and is accepted up to 5 days late.
- **Quiz:** four questions in the week-3 lecture.
- **Exam:** all eight questions. No answers are revealed until the results are out.
- **Saving:** academic progress is part of the save file (format version 2). Version 1 saves are migrated automatically.

### In the world
- **The Lecture Theatre Complex can now be entered.** It has a lobby with a timetable board that shows your own timetable, and two tiered lecture halls (LT1, LT2). You enter each hall at the back; rows of desks step down to the stage, the board and the podium.
- **During a session**, the board shows the course, week and topic, or "EXAMINATION" during exams, and the lecturer stands at the podium.
- **Sitting down** in the right hall around a session's time gives you these options:
  - Wait for it to start (time skips to the start).
  - Attend the lecture: introduction, two key points with "Take notes", the lecturer's question with feedback, and in week 3 the quiz. Time then moves to the end of the lecture.
  - Sit the exam, with hints.
  - Sitting anywhere else tells you what's on and when your next class is.
- **In Nkabom Library,** any seat lets you study a topic (1 hour) or work on an open assignment (2 hours, three questions).
- **Unity Hall door:** "Go to your room and sleep until morning" from 20:00 to 05:00, otherwise "rest for two hours". It fades out, skips the time and saves.
- **Residents with lectures** now walk into the complex and fill the hall seats. You'll find them there when you go in.

### Interface
- **Academics tab** (Tab → Academics):
  - Registration with course toggles, a credit counter and clear error messages.
  - Your weekly timetable and exam timetable.
  - Per-course progress: lectures attended, participation, assignment status and deadline, quiz, exam, preparation and hints.
  - All your notes, grouped by course.
  - Past results.
- **HUD objective:** after orientation, it follows your academic life — register, then the next lecture or exam with day, time and room (with a guide arrow to the right hall), then "results are out".
- **Semester results screen:** per-course components, total and grade, GPA, CGPA, class standing, probation warning and resits.

## Verification
- **Core tests:** 96 pass (24 new in Phase 3). They cover the grading scale and GPA, curriculum completeness and question well-formedness, deterministic option shuffling and safe 50/50 hints, and clash-free timetables for every programme. They also cover the calendar phases, every registration rule including prerequisites, attendance timing, challenges, study, preparation and hints, assignment late penalties, quiz and exam rules, the full results calculation (including that perfect attendance alone does not pass), next-task guidance, semester offsets, academic save and load, v1 to v2 save migration, rejecting unknown courses in saves, and rest timing.
- **Unity scripts and PlayMode tests:** compile against the UnityEngine reference assemblies. The new PlayMode tests (written, not yet run in Unity) cover attending a lecture in the hall end to end, studying in the library, and resting at Unity Hall until morning.

## Not in this phase
- **Semester 2 and later years:** the data model has year, semester and semester start day, but moving on to the next semester arrives with Phase 6.
- **Group projects:** they arrive with the narrative events in Phase 6 ("The Missing Assignment").
- **Tuition fees:** finances (Phase 4).
