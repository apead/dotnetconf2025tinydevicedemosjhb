# � McpAgriAi - Agricultural IoT with Model Context Protocol

This sample demonstrates how to create an MCP (Model Context Protocol) server on a nanoFramework device that can be controlled by AI agents through natural language commands. The solution includes two projects that work together to create an AI-powered agricultural monitoring system:

1. **McpAgriAi** - A nanoFramework MCP server running on ESP32 that exposes moisture sensor and light control capabilities
2. **McpClientConsole** - A .NET console application using Semantic Kernel to chat with the MCP server via Azure OpenAI

## Projects Overview

### 1. McpAgriAi - nanoFramework MCP Server

The **McpAgriAi** project is a nanoFramework application that runs on ESP32 devices and creates an MCP server exposing agricultural monitoring and control capabilities. It demonstrates how embedded devices can integrate with AI systems through the Model Context Protocol.

#### How It Works

The server exposes two main hardware interfaces:

1. **Moisture Sensor Control** (`Moisture.cs`)
   - Uses a DFRobot analog moisture sensor connected to ADC channel 7
   - Provides real-time soil moisture readings as percentages and qualitative levels (Dry, Wet, VeryWet)
   - Tracks sensor location for context-aware monitoring

2. **Light Control** (`Light.cs`)
   - Controls an LED connected to GPIO pin 2
   - Enables remote on/off switching via AI commands
   - Maintains location context for intelligent control decisions

#### MCP Server Setup

The `Program.cs` orchestrates the entire MCP server lifecycle:

```csharp
// 1. Connect to WiFi network
var connected = WifiNetworkHelper.ConnectDhcp(WiFi.Ssid, WiFi.Password, 
    requiresDateTime: true, token: new CancellationTokenSource(60_000).Token);

// 2. Discover and register MCP tools from decorated classes
McpToolRegistry.DiscoverTools(new Type[] { typeof(Light), typeof(Moisture) });

// 3. Start HTTP server with MCP endpoint at /mcp
using (var server = new WebServer(80, HttpProtocol.Http, 
    new Type[] { typeof(McpServerController) }))
{
    // Customize server metadata
    McpServerController.ServerName = "MyIoTDevice";
    McpServerController.ServerVersion = "2.1.0";
    McpServerController.Instructions = "Agricultural IoT device with moisture sensing and light control.";
    
    server.Start();
    Thread.Sleep(Timeout.Infinite);
}
```

#### Available MCP Tools

The server automatically exposes the following tools to AI agents:

**Moisture Sensor Tools:**
- `get_moisture` - Returns current moisture percentage and qualitative reading
- `get_moisture_location` - Gets the sensor's current location
- `set_moisture_location` - Updates the sensor's location context

**Light Control Tools:**
- `turn_on` - Activates the light (GPIO HIGH)
- `turn_off` - Deactivates the light (GPIO LOW)
- `get_location` - Returns the light's current location
- `set_location` - Changes the light's location context

#### WiFi Configuration

Edit `WiFi.cs` to connect your device to your network:

```csharp
public static string Ssid = "<SSID>";      // Your WiFi network name
public static string Password = "<PASSWORD>"; // Your WiFi password
```

**Important:** Replace `<SSID>` and `<PASSWORD>` with your actual WiFi credentials before deploying.

### 2. McpClientConsole - Semantic Kernel MCP Client

The **McpClientConsole** project is a .NET console application that demonstrates how to connect to the nanoFramework MCP server using Semantic Kernel and Azure OpenAI. It creates an intelligent chat interface that can invoke hardware functions through natural language.

#### How It Works

The client application follows this workflow:

1. **Load Configuration** - Reads Azure OpenAI credentials from `.env` file
2. **Connect to MCP Server** - Establishes SSE/HTTP connection to the nanoFramework device
3. **Discover Tools** - Retrieves available tools from the MCP server
4. **Register with Kernel** - Converts MCP tools to Semantic Kernel functions
5. **Start Chat Loop** - Enables interactive conversation with automatic tool invocation

#### Key Implementation Details

**MCP Client Connection:**
```csharp
var mcpToolboxClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(new SseClientTransportOptions()
    {
        Endpoint = new Uri("http://<MCPSERVER>/mcp"),  // Your ESP32 IP address
        TransportMode = HttpTransportMode.StreamableHttp,
    }, new HttpClient()));
```

**Tool Registration:**
```csharp
// Fetch available tools from nanoFramework device
var tools = await mcpToolboxClient.ListToolsAsync();

// Convert each MCP tool to a Kernel function
foreach (var tool in tools)
{
    var kernelFunction = KernelFunctionFactory.CreateFromMethod(
        method: async (KernelArguments args) =>
        {
            var result = await mcpToolboxClient.CallToolAsync(tool.Name, args);
            return result.Content.FirstOrDefault()?.Text ?? string.Empty;
        },
        functionName: tool.Name,
        description: tool.Description
    );
    kernelFunctions.Add(kernelFunction);
}

// Register all functions as a plugin
kernel.Plugins.AddFromFunctions("nanoFramework", kernelFunctions);
```

**Chat Interface:**
```csharp
OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
};

var result = await chatCompletionService.GetChatMessageContentAsync(
    history,
    executionSettings: settings,
    kernel: kernel);
```

#### Environment Configuration

Create a `.env` file in the `McpClientConsole` project directory with your Azure OpenAI credentials:

```dotenv
AZUREAI_DEPLOYMENT_API_KEY=<APIKEY>
AZUREAI_DEPLOYMENT_ENDPOINT=<ENDPOINT>
AZUREAI_DEPLOYMENT_NAME=<DEPLOYMENTNAME>
```

**Configuration Details:**
- `AZUREAI_DEPLOYMENT_API_KEY` - Your Azure OpenAI API key
- `AZUREAI_DEPLOYMENT_ENDPOINT` - Your Azure OpenAI endpoint URL (e.g., `https://your-resource.openai.azure.com/`)
- `AZUREAI_DEPLOYMENT_NAME` - Your deployment name (e.g., `gpt-4`, `gpt-35-turbo`)

#### MCP Server Connection

In `Program.cs`, update the MCP server endpoint to match your nanoFramework device's IP address:

```csharp
Endpoint = new Uri("http://192.168.1.139/mcp"),  // Replace with your ESP32's IP
```

**How to find your device IP:**
1. Deploy the McpAgriAi project to your ESP32
2. Open the Output window in Visual Studio
3. Look for the debug output showing the assigned IP address
4. Update the endpoint URI in the client code

## How the Sample Works

### Overview

The complete system creates an AI-powered agricultural monitoring solution where users can interact with physical IoT hardware using natural language:

1. **MCP Server (nanoFramework device)**: Runs on ESP32 and exposes moisture sensor and light control via HTTP/MCP
2. **MCP Client (Full .NET application)**: Connects AI agents to your device via Azure OpenAI and Semantic Kernel
3. **AI Agent**: Interprets natural language commands and invokes appropriate hardware functions

### Hardware Control Implementation

The `Light.cs` class demonstrates how to create MCP-enabled hardware controls:

```csharp
[McpServerTool("turn_on", "Turn on the light. Check the location to make sure it's the proper location first.")]
public static void TurnOn()
{
    Debug.WriteLine($"Turning on the light at location: {_location}");
    _lightPin.Write(PinValue.Low);  // GPIO control
}
```

Each method decorated with `[McpServerTool]` becomes available to AI agents with:

- **Function name**: Used by AI to identify the tool
- **Description**: Guides AI on when and how to use the tool
- Optional **description of output**: You can also specify the description of the output
- **Parameters**: Automatically parsed from method signatures

### Server Setup and Tool Discovery

The `Program.cs` shows the complete setup process:

1. **WiFi Connection**: Establishes network connectivity (don't forget to replace your wifi credentials)
2. **Tool Discovery**: Automatically finds all MCP-decorated methods
3. **Web Server**: Starts HTTP server with MCP endpoint at `/mcp`

```csharp
// Discover tools from classes
McpToolRegistry.DiscoverTools(new Type[] { typeof(Light) });

// Start web server with MCP support
using (var server = new WebServer(80, HttpProtocol.Http, new Type[] { typeof(McpServerController) }))
{
    server.Start();
    // Device is now ready to receive MCP requests
}
```

### Real-World Interaction Examples

#### Example 1: Checking Soil Moisture

**User**: "What's the moisture level at the dot net conf joburg location?"

**AI Agent Process**:
1. Gets sensor location: `get_moisture_location()` → "dot net conf joburg"
2. Confirms location matches
3. Reads moisture: `get_moisture()` → "Moisture at dot net conf joburg: 45% (Wet)"

**Device Trace**:
```text
Getting the location of the moisture sensor: dot net conf joburg
Getting moisture at location: dot net conf joburg, Percentage: 45%, Reading: Wet
```

**Assistant Response**: "The soil moisture at the dot net conf joburg location is 45%, which indicates wet conditions. The plants should have adequate water."

#### Example 2: Context-Aware Light Control

**User**: "turn on the light"

**AI Agent Process**:
1. Gets current location: `get_location()` → "dot net conf joburg"
2. Calls `turn_on()` to activate the light

**Device Trace**:
```text
Getting the location of the light: dot net conf joburg
Turning on the light at location: dot net conf joburg
```

**User**: "I moved the light to the greenhouse"

**AI Agent Process**:
1. Calls `set_location("greenhouse")` to update device state

**Device Trace**:
```text
Setting the location of the light to: greenhouse
```

#### Example 3: Multi-Step Agricultural Query

**User**: "Check if my plants need watering"

**AI Agent Process**:
1. Gets moisture location: `get_moisture_location()`
2. Reads current moisture: `get_moisture()` → "25% (Dry)"
3. Analyzes result and provides recommendation

**Assistant Response**: "The soil moisture at your location is 25%, which is quite dry. Your plants would benefit from watering soon."

### Complete MCP Tools Reference

The agricultural IoT system exposes these tools to AI agents:

| Tool | Description | Parameters | Return Type |
|------|-------------|------------|-------------|
| `get_moisture` | Returns soil moisture reading | None | String with percentage and level |
| `get_moisture_location` | Returns moisture sensor location | None | String |
| `set_moisture_location` | Updates sensor location | location (string) | Void |
| `get_location` | Returns light location | None | String |
| `turn_on` | Activates the light | None | Void |
| `turn_off` | Deactivates the light | None | Void |
| `set_location` | Updates light location | location (string) | Void |

### Key Features

- **Context Awareness**: AI agents understand location context and ask for clarification
- **State Management**: Device maintains location state across requests
- **Natural Language**: Users can interact using conversational commands
- **Safety Checks**: AI verifies location before performing actions
- **Real-time Communication**: Immediate hardware response to AI commands

## Architecture Overview

### Communication Flow

```text
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│                 │         │                  │         │                 │
│  User via Chat  │ ◄─────► │  MCP Client      │ ◄─────► │  ESP32 Device   │
│  (Natural Lang) │         │  (Semantic       │  HTTP   │  (nanoFramework)│
│                 │         │   Kernel + AI)   │  /mcp   │                 │
└─────────────────┘         └──────────────────┘         └─────────────────┘
                                     │                            │
                                     │                            │
                                     ▼                            ▼
                            ┌─────────────────┐         ┌──────────────────┐
                            │  Azure OpenAI   │         │  Hardware:       │
                            │  (GPT-4)        │         │  - Moisture      │
                            │                 │         │  - LED/Light     │
                            └─────────────────┘         └──────────────────┘
```

### Key Technologies

**MCP Server (McpAgriAi):**
- **nanoFramework** - .NET for embedded devices
- **WebServer.Mcp** - MCP protocol implementation
- **ADC/GPIO** - Hardware sensor/actuator interfaces
- **WiFi networking** - HTTP endpoint hosting

**MCP Client (McpClientConsole):**
- **.NET 8+** - Modern .NET runtime
- **Semantic Kernel** - AI orchestration framework
- **Azure OpenAI** - Large language model integration
- **ModelContextProtocol SDK** - MCP client library
- **DotNetEnv** - Environment configuration

## Hardware Requirements

A hardware device with networking capabilities running a nanoFramework image. This sample has been tested with ESP32-S3 and ESP32-C3 devices.

### Required Components

1. **ESP32 Development Board** (ESP32-S3 or ESP32-C3 recommended)
2. **DFRobot Analog Capacitive Soil Moisture Sensor** - Connected to ADC Channel 7
3. **LED** - Connected to GPIO pin 2 (or use the onboard LED)
4. **220Ω Resistor** (if using external LED)
5. **WiFi Network** with internet access

### Moisture Sensor Details

The DFRobot moisture sensor provides:
- **Analog output** via ADC reading
- **Percentage calculation** (0-100%) with calibrated air/water values
- **Qualitative readings**: Dry, Wet, VeryWet, Unknown
- **Calibration values**:
  - Air Value: 2900 (completely dry)
  - Water Value: 1300 (fully submerged)

## Wiring Diagram

```text
ESP32 GPIO 2 ─── 220Ω Resistor ─── LED Anode
                                    │
ESP32 GND ──────────────────────── LED Cathode

DFRobot Moisture Sensor:
    VCC ─────────── ESP32 3.3V
    GND ─────────── ESP32 GND
    AOUT ────────── ESP32 ADC Channel 7 (GPIO pin varies by board)
```

**Note:** GPIO pin 2 is used for the LED in this implementation. Most ESP32 boards have an onboard LED you can use without external components.

## Configuration Reference

### WiFi Settings (McpAgriAi Project)

**File:** `WiFi.cs`

```csharp
public static string Ssid = "<SSID>";         // Your WiFi network name
public static string Password = "<PASSWORD>";  // Your WiFi password
```

**Important Notes:**
- WiFi credentials are stored in plain text - suitable for development only
- The device requires DHCP support on your network
- Connection timeout is set to 60 seconds
- Requires internet connectivity for time synchronization

### Environment Variables (McpClientConsole Project)

**File:** `.env`

```dotenv
AZUREAI_DEPLOYMENT_API_KEY=<APIKEY>
AZUREAI_DEPLOYMENT_ENDPOINT=<ENDPOINT>
AZUREAI_DEPLOYMENT_NAME=<DEPLOYMENTNAME>
```

**Configuration Details:**

| Variable | Description | Example |
|----------|-------------|---------|
| `AZUREAI_DEPLOYMENT_API_KEY` | Your Azure OpenAI API key | `abc123def456...` |
| `AZUREAI_DEPLOYMENT_ENDPOINT` | Azure OpenAI resource endpoint | `https://myresource.openai.azure.com/` |
| `AZUREAI_DEPLOYMENT_NAME` | Deployment/model name | `gpt-4`, `gpt-35-turbo` |

**How to obtain Azure OpenAI credentials:**
1. Navigate to [Azure Portal](https://portal.azure.com)
2. Create or open your Azure OpenAI resource
3. Go to "Keys and Endpoint" section
4. Copy the key and endpoint
5. Note your deployment name from the "Deployments" section

### MCP Server Connection (McpClientConsole)

**File:** `Program.cs` (Line ~13)

```csharp
Endpoint = new Uri("http://<MCPSERVER>/mcp"),
```

Replace `<MCPSERVER>` with your ESP32's IP address. The complete URI should look like:
- `http://192.168.1.139/mcp` (example)
- Format: `http://[IP_ADDRESS]/mcp`
- Protocol: HTTP (not HTTPS in this sample)
- Port: 80 (default, not specified in URI)
- Path: `/mcp` (MCP endpoint)

### MCP Server Metadata Customization

**File:** `Program.cs` (McpAgriAi project)

```csharp
McpServerController.ServerName = "MyIoTDevice";        // Identifies your device
McpServerController.ServerVersion = "2.1.0";           // Version number
McpServerController.Instructions = "Agricultural IoT device..."; // AI instructions
```

These values help AI agents understand your device's capabilities and how to interact with it.

## Troubleshooting

### Common Issues

**Problem:** Device won't connect to WiFi
- Verify SSID and password are correct in `WiFi.cs`
- Check that your WiFi network uses 2.4GHz (ESP32 doesn't support 5GHz)
- Ensure WiFi network has DHCP enabled
- Check that ESP32 is within range of WiFi access point

**Problem:** MCP Client can't connect to server
- Verify the ESP32 IP address in the client's `Program.cs`
- Ensure both devices are on the same network
- Check firewall settings on your computer
- Verify the MCP server is running (check Visual Studio Output window)
- Try pinging the ESP32 IP address from your computer

**Problem:** Azure OpenAI authentication fails
- Double-check your `.env` file exists in the correct directory
- Verify API key, endpoint, and deployment name are correct
- Ensure your Azure OpenAI resource is active and has quota available
- Check that the deployment name matches exactly (case-sensitive)

**Problem:** Tools not discovered by AI agent
- Verify the MCP server started successfully
- Check that `McpToolRegistry.DiscoverTools()` was called
- Ensure classes with tools are passed to `DiscoverTools()`
- Review the client console output for tool listing
- Confirm `[McpServerTool]` attributes are correctly applied

**Problem:** Moisture sensor returns incorrect readings
- Check ADC channel number matches your wiring (currently channel 7)
- Verify sensor power (3.3V) and ground connections
- Calibrate air/water values if needed (see `DfRobotMoistureSensor.cs`)
- Ensure sensor is properly inserted into soil

## What Makes This Special?

This sample demonstrates the convergence of three powerful technologies:

1. **Embedded IoT** - nanoFramework brings .NET to resource-constrained devices
2. **Model Context Protocol** - Standardized way for AI to interact with external systems
3. **AI Agents** - Natural language interface powered by Azure OpenAI and Semantic Kernel

Together, they enable scenarios where users can:
- Control physical hardware using conversational language
- Query sensor data through AI-powered chat
- Build context-aware automation that understands location and state
- Integrate embedded devices seamlessly into AI-driven workflows

### Real-World Applications

This architectural pattern can be extended to:
- **Smart Agriculture** - Monitor multiple sensors, control irrigation, track plant health
- **Home Automation** - Control lights, HVAC, security systems via natural language
- **Industrial IoT** - Monitor equipment, trigger maintenance, analyze production data
- **Environmental Monitoring** - Track temperature, humidity, air quality with AI analysis
- **Smart Buildings** - Energy management, occupancy detection, climate control

## Related topics

### Reference

- [nanoFramework WebServer API](https://github.com/nanoframework/nanoFramework.WebServer)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [nanoFramework Documentation](https://docs.nanoframework.net/)

### Additional Resources

- [Model Context Protocol GitHub](https://github.com/modelcontextprotocol)
- [nanoFramework Samples](https://github.com/nanoframework/Samples)
- [Semantic Kernel GitHub](https://github.com/microsoft/semantic-kernel)
- [DFRobot Moisture Sensor Documentation](https://wiki.dfrobot.com/Capacitive_Soil_Moisture_Sensor_SKU_SEN0193)

---

**Built with ❤️ for .NET Conf 2025 Johannesburg**

## Getting Started

### Step 1: Configure WiFi Credentials

Edit the `WiFi.cs` file in the **McpAgriAi** project:

```csharp
public static string Ssid = "YourNetworkName";     // Replace with your WiFi SSID
public static string Password = "YourPassword";     // Replace with your WiFi password
```

### Step 2: Build and Deploy MCP Server (McpAgriAi)

1. Open `McpAgriAi.sln` in Visual Studio 2022 (or Visual Studio 2019)
2. Connect your ESP32 device via USB
3. Ensure the device is visible in Device Explorer (`View > Other Windows > Device Explorer`)
4. Press `F5` or select `Debug > Start Debugging`
5. Note the IP address displayed in the Output window (e.g., `IP Address: 192.168.1.139`)

### Step 3: Configure MCP Client Environment

Create a `.env` file in the `McpClientConsole` directory:

```dotenv
AZUREAI_DEPLOYMENT_API_KEY=your-azure-openai-api-key
AZUREAI_DEPLOYMENT_ENDPOINT=https://your-resource.openai.azure.com/
AZUREAI_DEPLOYMENT_NAME=gpt-4
```

### Step 4: Update MCP Server Endpoint

Edit `Program.cs` in the **McpClientConsole** project and update the IP address to match your ESP32:

```csharp
Endpoint = new Uri("http://192.168.1.139/mcp"),  // Replace with your device's IP
```

### Step 5: Run the MCP Client

1. Open a terminal in the `McpClientConsole` directory
2. Run `dotnet restore` to restore dependencies
3. Run `dotnet run` to start the client
4. Wait for connection confirmation: "Connected!"
5. Start chatting with your agricultural IoT device!

## Testing the System

### Example Conversation Flow

```text
User > What's the current soil moisture?
Assistant > The soil moisture at dot net conf joburg is 45%, which indicates wet conditions.

User > Turn on the light
Assistant > I've turned on the light at the dot net conf joburg location.

User > Is the soil too dry?
Assistant > The soil moisture is at 45% (Wet), so it's not too dry. The plants have adequate water.
```

### Debugging Tips

**MCP Server (nanoFramework):**
- Monitor the Output window in Visual Studio for device traces
- Check WiFi connection status in debug output
- Verify the IP address is assigned correctly
- Ensure the device stays powered and connected

**MCP Client (Console):**
- Verify `.env` file exists with correct Azure OpenAI credentials
- Check that the MCP server endpoint URL is correct
- Ensure network connectivity between client and ESP32
- Review tool discovery output to confirm available functions
