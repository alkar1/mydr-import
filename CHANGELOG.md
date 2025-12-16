# Changelog

All notable changes to MyDr Import project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for Etap 2
- CSV export functionality
- Batch processing
- Data validation
- Error logging

### Planned for Etap 3
- SQL database integration
- Data transformations
- Advanced filtering

## [1.0.0] - 2025-12-16

### Added - Etap 1: Analiza Struktury
- ? Streaming XML parser using XmlReader
- ? Structure analysis for Django fixtures format
- ? Real-time progress reporting with ETA
- ? Multi-format output (CSV, TXT, JSON)
- ? Support for 60+ Django model types
- ? Field metadata collection (types, relations, statistics)
- ? Memory-efficient processing for large files (8+ GB)
- ? UTF-8 encoding support for Polish characters
- ? Primary key range tracking
- ? NULL value statistics
- ? Sample value collection
- ? Max length tracking for text fields

### Performance
- Processes ~17,456 objects/second
- Handles 8.5 GB file in ~7 minutes 45 seconds
- Constant memory usage ~150-200 MB regardless of file size

### Documentation
- Comprehensive README.md with usage examples
- ARCHITECTURE.md with technical details
- Inline code documentation
- Example outputs and reports

### Models Supported
- gabinet.patient (22,397 records)
- gabinet.visit (696,727 records)
- gabinet.visitnotes (913,366 records)
- gabinet.patientnote (1,593,968 records)
- gabinet.recognition (992,503 records)
- gabinet.icd10 (992,503 records)
- gabinet.icd9 (48,803 records)
- gabinet.recipe (350,551 records)
- gabinet.sickleave (45,299 records)
- ... and 51 more types

### Technical Details
- Target framework: .NET 8.0
- Language: C# 12
- Dependencies: CsvHelper 30.0.1
- Architecture: Layered Architecture
- Design patterns: Strategy, Builder, Observer (progress)

### Initial Commit
- Project structure setup
- Core models: XmlObjectInfo, FieldInfo
- Services: XmlStructureAnalyzer, ProgressReporter
- Console application entry point
- Configuration files (.csproj, .gitignore)

---

## Version History Summary

- **v1.0.0** (2025-12-16) - Initial release with structure analysis
- **v0.1.0** (2025-12-16) - Project initialization

---

## Contributors

- Initial development team

---

## Notes

This project was created to facilitate medical data import from large Django XML exports
into structured CSV format for further analysis and processing.
