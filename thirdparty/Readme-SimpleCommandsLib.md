# SimpleCommandLib

SimpleCommandLib is a lightweight C# library designed to facilitate the creation and management of command-line interfaces (CLI) in applications. It provides an object-based structure for parsing and executing commands, making it easy to integrate into CLI tools or applications that require user input handling.

SimpleCommandLib will perform in any scenario where user input or strings need to be mapped to code execution.

## Features

- **Object-Based Command Structure**: Easily define and manage commands using objects.
- **Command Parsing**: Efficiently parse command-line input or user input.
- **Extensible**: Add custom commands with minimal effort.
- **Simple**: Designed to be human-readable and easy to understand

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Install from [Nuget](https://www.nuget.org/packages/SimpleCommandLib/1.1.0)

### Usage

To use SimpleCommandLib in your project, you need to implement the `ICommandRunner` interface for each command you want to support. Then, create a class that extends `CommandDispatcher` to manage these commands.

#### Example

See [ExampleProject](src/SimpleCommandLib.ExampleProgram/ExampleProgram.cs)

### Handling Exceptions

The library includes a custom exception, `CommandFailedException`, which is thrown when a command fails to execute. You can catch this exception to handle errors gracefully in your application. Commands are expected to throw an exception that extends this class.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

This README provides an overview of SimpleCommandLib, its features, and instructions on how to integrate it into your projects. For more detailed examples and documentation, please refer to the source code and comments within the [repository](src/SimpleCommandLib/) or the [ExampleProgram](src/SimpleCommandLib.ExampleProgram/)
