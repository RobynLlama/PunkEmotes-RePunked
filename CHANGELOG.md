# Changelog

## 2.1.3

- Updated to work with the 72025.a8 update, yay!
  - Complete rework of animation loading to support 72025.a8
  - Minor rewrite of chat handling to support 72025.a8

## 2.1.2

- Removes accidentally bundled in dependency.
- Fixes networking error where clients could receive packets while loading in before _mainPlayer was set
- Fixes patch count to reflect that 2 were removed, this error was harmless but it is now fixed

## 2.1.1

- Fixes networking error introduced by 2.1.0 that allowed clients to receive packets too early, oops

## Version 2.1.0

- Replaces networking with CodeTalker network, this is a breaking change
- Networking will no longer show up in Channel 2 for clients without PunkEmotes
- Clients using version 2.0.2 or lower will not be able to communicate with clients running 2.1.0 or higher!

## Version 2.0.2

- Adds: command `em list` to list all available animations as detected by RePunked

## Version 2.0.1

- Fixed: An error with improperly trying to call an RPC without authority (should reduce log spam)
- Refactored: Cleaned up the entire main loop and implemented SimpleCommandLib for cleaner state management

## Version 2.0.0

- Fixed: Issue with color chat
- Added: Filter for markup in chat to fix any other chat mods breaking the networking
- Refactored: Overwhelming majority of code to be more null safe, this may solve other minor errors users were experiencing
