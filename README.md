# PunkEmotes Re-Punked Edition

PunkEmotes by [Punkalyn](https://github.com/Punkalyn/PunkEmotes) is a mod that allows you to use animations from all the ATLYSS races.

## WARNING

Clients using version 2.0.2 or lower will not be able to communicate with clients running 2.1.0 or higher!

PunkEmotes has finally been updated to the 72025.a8 release, hurray!

## Usage

You can use the command `/em help` for the in-game help message, for explanation but here's a quick overview:

Use `/em {animation} {race}` in the in-game chatbox to use an emote. Replace {animation} with "sit" or "dance", and {race} can be "chang", "byrdle", "imp", "poon", and "kubold". The command `/em chang_sitinit02` is used for the chang /sit2 animation.

### New in 2.0.2

Use `/em list` to output a complete list of every animation punk emotes has found

## Maintainers

**Original Author**: [Punkalyn](https://github.com/Punkalyn/PunkEmotes)

**Current Maintainer(s)**: [Robyn](https://github.com/RobynLlama)

## Project goals

The Re-Punked project's goals are

- [ ] Eliminate Existing Mod Conflicts
  - [x] Color Chat Issue (this appears to be fixed)
  - [ ] Any other mods that modify chat (please report)
- [ ] Integrate the PunkEmotes-Hotkeys mod natively
  - [ ] Add a slick UI for user-configurable fast emotes

## Why is there so much logging now?

I removed the class and flags that were previously being used to control logging internally because BepInEx already does this externally. Please edit your BepInEx.cfg file by opening it in your mod manager's config editor and finding the section that looks like this

```toml
## Which log levels to show in the console output.
# Setting type: LogLevel
# Default value: Fatal, Error, Warning, Message, Info
# Acceptable values: None, Fatal, Error, Warning, Message, Info, Debug, All
# Multiple values can be set at the same time by separating them with , (e.g. Debug, Warning)
LogLevels = Fatal, Error, Warning, Message, Info
```

And removing `Message` and `Info` from the output. Those two channels are for modders to output debug information about the state of their mod and its best to not have them in the console. There is another section further down that controls what is output to disk, you may also modify it to remove those channels as well if it bothers you. Note, when reporting bugs to modders please include those channels in your logs, they help a lot!

## License Information

This mod and all associated project files are released under the GNU GPLv3. See the LICENSE file that came with your copy or visit [GNU Website](https://www.gnu.org/licenses/gpl-3.0.en.html#license-text) for more information
