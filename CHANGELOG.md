# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
### Added
- Added connection test prior to login
- Added Elite display events on user tasking (prior to completed)
- Added better Elite error messages
- Added forked version of ReadLine, with better tab-complete
- Added change user password
- Added shellcmd task
- Added sharpdpapi task
- Added sharpup task
- Added sharpdump task
- Added sharpwmi task
- Added safetykatz task
- Added seatbelt task
- Added killdate option

### Changed
- Event printing moved to task
- Event creation moved to Covenant controllers
- TaskMenu now displays full task description, detailed parameter info
- Limited API refereshes and calls to improve speed

### Fixed
- Fixed bad command causing following commands to fail
- Fixed Create user error message

## [v0.1.3] - 2019-03-14
### Added
- Added credentials menu
- Added warning for connecting over localhost to Covenant using docker

### Changed
- Trim UserInput
- Split wmi, dcom, and bypassuac tasks to wmicommand, wmigrunt, dcomcommand, dcomgrunt, bypassuaccommand, bypassuacgrunt tasks

### Fixed
- Fixed dcsync parameter error
- Fixed download task

## [v0.1.2] - 2019-02-14
### Added
- Added AssemblyReflect task shortcut

### Changed
- Updated API
- Updated README
- Moved CHANGELOG.md to root directory

## [v0.1.1] - 2019-02-09
### Added
- Added CHANGELOG.md

### Fixed
- Fixed WscriptLauncher Write command

## v0.1 - 2019-02-07
- Initial release

[v0.1.1]: https://github.com/cobbr/Elite/compare/v0.1...v0.1.1
[v0.1.2]: https://github.com/cobbr/Elite/compare/v0.1.1...v0.1.2
[v0.1.3]: https://github.com/cobbr/Elite/compare/v0.1.2...v0.1.3
