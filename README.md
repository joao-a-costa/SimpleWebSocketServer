# SimpleWebSocketServer

SimpleWebSocketServer is a C# implementation of a basic WebSocket server and client. It allows for bidirectional communication between clients and the server over the WebSocket protocol.

## Features

- **WebSocket Server**: Provides a simple WebSocket server that can accept client connections and exchange messages.
- **WebSocket Client**: Includes a client implementation that can connect to the server and exchange messages.
- **Event Handling**: Supports events for server start, client connection, disconnection, and message reception.

## Requirements

- .NET Core 3.1 or higher

## Usage

1. Clone the repository:

    ```bash
    git clone https://github.com/yourusername/SimpleWebSocketServer.git
    ```

2. Build the solution using Visual Studio or the .NET CLI:

    ```bash
    cd SimpleWebSocketServer
    dotnet build
    ```

3. Run the server:

    ```bash
    cd SimpleWebSockerServer.Console.Server
    dotnet run
    ```

4. Run the client:

    ```bash
    cd SimpleWebSockerServer.Console.Client
    dotnet run
    ```

## Configuration

- **Server Configuration**: The server listens on `http://localhost:20005/` by default. You can change the server address in the `Program.cs` file of the server project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

- This project is based on [Microsoft's WebSocket sample](https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocket).
- Special thanks to contributors and maintainers.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bug fixes, improvements, or new features.

## Support

For support, questions, or suggestions, please [open an issue](https://github.com/yourusername/SimpleWebSocketServer/issues).
