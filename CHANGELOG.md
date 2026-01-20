# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

<!-- Headers should be listed in this order: Added, Changed, Deprecated, Removed, Fixed, Security -->

## [1.1.1] - 20.01.2026
### Fixed
 - Various undo/redo fixes
 - Zooming fixes
 - Edge editing fixes

## [1.1.0] - 22.05.2025
### Added
 - Partial support for localization (for diagram code only for now)
### Changed
 - Refactored layering, dimming and hotkey management

## [1.0.2] - 20.03.2025
### Added
 - Edge duplication action
 - Duplication hotkey (ctrl+D) for nodes and edges
### Changed
 - Updated action's parameter view
 - Updated selection dropdowns scrolling
 - Edge lines going into nested graph from its parent and back are included in parent layout now
 - Various small UX/UI tweaks
 - New nodes are spawning in view center now
### Fixed
 - Node layout now accounts nested graph edges
 - Proper default names for nodes
 - Various clicking-through scenarios

## [1.0.1] - 27.02.2025
## First public release
### Changed
 - Dropdowns scrolling adjusted
 - Float numbers support in icons and timer module
 - Various layout changes
### Fixed
 - Terminology fixes
 - Undo fix for node and edge deletion

## [1.0.0] - 13.02.2025
### Added
 - Initial release of editor used in Arena as separate package