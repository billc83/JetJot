# Introduction

Change Log

JetJot follows an iterative, stability-first development approach.  

This Change Log was written and exported using JetJot.

Version numbers reflect internal milestones rather than strict semantic versioning.

"It's somewhat based on my vibes at the time." - William S. Coolman

---

# v0.43

Persistence Improvement

- Fixed an issue where opening/importing a project or switching to a recently opened project would not save data
- Fixed an issue where File Menu did not close after selecting and loading a recent project

---

# v0.42

Sidebar Tweaks

- Fixed an issue where user set sidebar width did not persist.
- Fixed an issue where user set sidebar too large messed with UI scaling

---

# v0.41

Small Bug Patch

- Fixed an issue where Drag to Delete in Sidebar was not visible due to positioning
- Fixed an issue where hiding sidebar, focus mode, and super focus mode did not expand the editor horizontally

---

# v0.40

Nearing Completion

- Added ability for user to change sidebar width
- Implemented ability for user to shorten or widen margin width
- Implemented ability to right click Sidebar to Add Documents
- Various Bug Fixes and UI Tweaks

---

# v0.31

 Find and Replace

- Implemented Find & Replace

---

# v0.30

Additional Keyboard Controls, Themed Cursor

- Implemented and displayed more keyboard controls
- Added About>Display Keyboard Controls
- Added a toggle for Themed Cursor
- Fixed an issue preventing Find Next button from working
- Fixed an issue causing editor to change colors when mouse hovered

---

# v0.28

Exports Added

- Added ability to export manuscripts/projects as .txt, .md, or .html files
- Removed "Spelling errors" when displaying spell check caution sign
- Fixed an issue where Find feature did not highlight words properly
- Changed instances of "Manuscript" to "Project"

---

# v0.26.5

Spell Check, Vol. 2

- Added visual indication of spelling errors at bottom right corner
- Added toggle for visual indication on/off
- Added ability to click visual indicator to cycle thru misspelled words for quick access
- Added ability for users to right click misspelled words and Ignore (if intentional) or Add to Dictionary (if intentional and frequently used)

---

# v0.25

Spell Check, Vol. 1

- Added basic spell check functionality

---

# v0.24

Small bug patch

- Fixed an issue where new documents were not automatically highlighted
- Attempted to fix an issue where pop-down menus in top bar, when app maximized on the bottom screen of a 2 screen setup, would show in the wrong screen (known Avalonia bug)

---

# v0.23

Visual Update, Small Bug Fixes and Easter Egg

- ADDED A SECRET*
- Fixed an issue where documents could be deleted by dragging and dropping to delete
- Fixed an issue where cursor and word length/goal couldn't be seen using theme color Editor's Ebony
- Added ability to toggle the color of the top bar on and off (some users thought it was too much, I think it is beautiful!  -Bill)
- Removed Segoe UI font due to glitch
- Removed unnecessary words in the ToolBar, and magnifying glass emoji
- Retooled the UI for a more streamlined look
- Made QOL change: when sidebar is not active, Manuscript name replaces JetJot tagline at the top of the app
- Fixed an issue where Super Focus Mode reverted ToolBar back to its previous iteration

* SUPER SECRET EASTER EGG: DON'T TELL ANYONE

---

# v0.22

Bug Fixes Quick Pass

- Fixed an issue where typewriter mode did not work correctly at start and end of documents
- Fixed an issue where sidebar and editor wouldn't expand vertically when progress/goal bar was disabled
- Fixed an issue where exiting super focus mode wouldn't allow aforementioned vertical expansion
- Fixed an issue where button icons were aligned to the top left, not center
- Fixed an issue where button texts were aligned to the left, not center
- Ensured locked files are unable to be deleted without being unlocked first
- Ensured locked files persist thru sessions
- Fixed an issue causing Bluebook Blue theme to load and ignoring user's preferred theme

---

# v0.21

Document Locks

- Enabled Document Locking / Unlocking

---

# v0.20

Huge Polish Pass (Stable)

- Made IBM Plex Sans default font
- Implemented Logo.ico file as toolbar app visual
- Implemented more cohesive visual theme
- Implemented customizable color themes
- Implemented various changes to pop ups for theme cohesion
- Implemented keyboard shortcuts for new document and saving document
- Replaced placeholder document text with watermark text for user ease

---

# v0.17

Enhanced Toolbar

- ATTEMPTED CHANGE TO RICH TEXT FORMAT, which absolutely broke the build.  Reverted to v0.15.1, stable.
- Implemented Find Feature in Toolbar
- Implemented Font Selection in Toolbar
- Made FontFamily and FontSize persist thru sessions
- Applied margins to text editor

---

# v0.15.1

STABLE WITHOUT RICH TEXT

-Stabilized Typewriter Mode; Still not working for top or bottom of document
- Attempted to implement Active Line mode for paragraph / sentence but had to roll back.

We MIGHT implement a Rich Text Format but we will see...

---

# v0.15

Drag and Drop, and Focus Modes

- Implemented drag and drop to reorder documents
- Implemented scrolling in sidebar when document count exceeds available space!
- Added Visual Indicator where documents were being dropped to
- Added Focus mode, which automatically deselects ALL toolbars leaving only the canvas
- Added SUPER FOCUS mode, which makes the editor full screen.
- Added Typewriter Mode, allowing user to keep the cursor in the center of the screen

---

# v0.12

Cut/Copy/Paste and Small Header Change

- Added support for non-keyboard Cut/Copy/Paste
- Moved Header Logo, Name, and Tagline to System Bar (up top) and removed the Header for more writing space.

---

# v0.11

QOL Additions

- Fixed a bug where removing a project from "Recent Projects" deleted the project
- Added user preferences for toolbars to show/hide on start of app
- Added import functionality to add projects from outside of Documents/JetJot
- Implemented manuscript naming and renaming
- Added “Edit Project Name” and “Edit Document Name” actions to the Edit menu
- Projects are now saved to Documents/JetJot/<ProjectName>
- Implemented New Manuscript and Open Manuscript workflows
- Added Recent Projects tracking via JSON for quick access to previously opened manuscripts

---

# v0.10

Persistence

- Added manuscript persistence and restore last session when closing and reopening app

---

# v0.07

Many small fixes and tweaks

- Fully working text editor
- Remade top bar with FILE | EDIT | VIEW | ABOUT drop down menus
- Made sidebar, writing goal, and format bar toggleable
- Made size changes when any item is removed or added
- Fixed TAB not formatting indentation and changing focus
- Many other small tweaks and fixes

---

# v0.06

Word Goal and Formatting Header Added

- Added Word Goal and Progress Bar
- Added Placeholder Text Formatting header

---

# v0.05

Basic but functional

Added:
- Rename documents feature
- Move up/down to restructure manuscript
- Small paper airplane logo
- New Document button to create new docs

*All functional.  Rollback to here if we mess it up!

---

# v0.01

Initial Commit

Working on Codename: JetJot, a word processor inspired by Ulysses, Scrivener, and iA Writer.  Written in C# using Avalonia, mostly as a learning experience

---

# Outro

Thank you for reading! 

---

