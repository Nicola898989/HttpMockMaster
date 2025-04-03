# HTTP Interceptor & Proxy

A cross-platform HTTP interceptor, proxy and mock server application built with .NET 8 and Vue.js/Electron.

## Features

- **HTTP Interception** - Capture local HTTP requests (GET, POST, PUT, etc.), including headers, body, and path details
- **Rule-Based Response** - Configure automatic responses to specific requests based on custom rules
- **Request Proxying** - Forward requests to a specified domain while maintaining the original request details
- **Request Logging** - Record both incoming and outgoing proxy requests
- **Mock Server** - Create mock responses for API testing and development

## Installation

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v14 or later)
- [npm](https://www.npmjs.com/get-npm) (v6 or later)

### Building from Source

#### Backend

1. Navigate to the project directory
2. Build the .NET backend:

```bash
dotnet build
