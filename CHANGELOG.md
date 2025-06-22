# Changelog

## Version 2.0.1

- Fixed: An error with improperly trying to call an RPC without authority (should reduce log spam)
- Refactored: Cleaned up the entire main loop and implemented SimpleCommandLib for cleaner state management

## Version 2.0.0

- Fixed: Issue with color chat
- Added: Filter for markup in chat to fix any other chat mods breaking the networking
- Refactored: Overwhelming majority of code to be more null safe, this may solve other minor errors users were experiencing
