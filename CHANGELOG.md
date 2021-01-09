# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Change solution template to Mini-Scaffold

## 2.1.7 - Nov 8, 2020
### Changed
- Bump Typeshape dependency

## 2.1.6 - Apr 26, 2020
### Changed
- Ignore case during Enum & DU serialization. Credits [Piaste](https://github.com/piaste)

## 2.1.5 - Nov 11, 2019
### Changed
- Bump TypeShape version. Credits [Rajivhost](https://github.com/rajivhost)

## 2.1.4 - Oct 18, 2019
### Changed
- Restrict TypeShape dependency. Credits [Piaste](https://github.com/piaste)

## 2.1.3 - Jun 9, 2019
### Changed
- Update TypeShape dependency. Credits [Rajiv](https://github.com/Rajivhost)

## 2.1.2 - May 24, 2019
### Changed
- Update dependencies

## 2.0.2 - Aug 12, 2018
### Added
- Adds Default Value Attribute.

## 2.0.1 - July 27, 2018
### Added
- Adds support for list of enums & option of enums.

## 2.0.0 - June 26, 2018
### Added
- Adds support for AppConfig (JSON, XML & INI) files in dotnet core

### Changed
- Upgrades TypeShape

## 2.0.0-beta1 - June 6, 2018
### Added
- Adds initial support for AppConfig

## 1.2.1 - May 30, 2018
### Fixed
- Fixes #7 - Adds support for list of Discriminated Union

## 1.1.2 - May 29, 2018
### Fixed
- Fixes #6 - treating empty string as none for string option type

## 1.1.1 - May 10, 2018
### Added
- Adds support for Discriminated Union

## 1.1.0 - May 7, 2018
### Changed
- Makes parse logic public to use it in other contexts. 

## 1.0.0 - April 28, 2018
### Added
- Adds Dot Net Core 2.0 Support

### Changed
-  **Breaking Change** - AppConfig is no longer supported due to new dot core app config file format change.

## 1.0.0-beta1 - April 28, 2018
### Added
- Adds Dot Net Core 2.0 Support

### Changed
- **Breaking Change** - AppConfig is no longer supported.

## 0.0.6 - April 27, 2018
### Changed
- Relax FSharp Core Version Constraint

## 0.0.5 - February 23 2018
### Added
- Adds field level list separator character attribute contributed by @mtnrbq

## 0.0.4 - February 5 2018
### Changed
- Improves NotSupported error message

## 0.0.3 - February 3 2018
### Fixed
- Fixes custom prefix issue in nested record.

## 0.0.2 - February 3 2018
### Changed
- Updates TypeShape version to 4.0

## 0.0.1 - February 1 2018
### Added
- Initial release