using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using System.Net.Http;

// Load environment variables from .env file
DotNetEnv.Env.Load("./.env");

//
// 1. Create MCP Toolbox client (SSE/HTTP)
//
Console.WriteLine("Connecting to nanoFramework MCP server...");
var mcpToolboxClient = await McpClientFactory.CreateAsync(
    new SseClientTransport(new SseClientTransportOptions()
    {
        Endpoint = new Uri("http:/<MCPSERVER>/mcp"),
        TransportMode = HttpTransportMode.StreamableHttp,
    }, new HttpClient()));
Console.WriteLine("Connected!");
// --

var kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(
                        DotNetEnv.Env.GetString("AZUREAI_DEPLOYMENT_NAME"),
                        DotNetEnv.Env.GetString("AZUREAI_DEPLOYMENT_ENDPOINT"),
                        DotNetEnv.Env.GetString("AZUREAI_DEPLOYMENT_API_KEY")
                    )
                    .Build();

//
// 2. Register MCP Toolbox client as a tool
//
var tools = await mcpToolboxClient.ListToolsAsync().ConfigureAwait(false);

// Print those tools
Console.WriteLine("// Available tools:");
foreach (var t in tools) Console.WriteLine($"{t.Name}: {t.Description}");
Console.WriteLine("// --");

// Load them as AI functions in the kernel
#pragma warning disable SKEXP0001
var kernelFunctions = new List<KernelFunction>();
foreach (var tool in tools)
{
    var kernelFunction = KernelFunctionFactory.CreateFromMethod(
        method: async (KernelArguments args) =>
        {
            var result = await mcpToolboxClient.CallToolAsync(tool.Name, args.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            return result.Content.FirstOrDefault()?.Text ?? string.Empty;
        },
        functionName: tool.Name,
        description: tool.Description
    );
    kernelFunctions.Add(kernelFunction);
}
kernel.Plugins.AddFromFunctions("nanoFramework", kernelFunctions);

// Check available prompts
Console.WriteLine("// Available prompts:");

try
{
    var prompts = await mcpToolboxClient.ListPromptsAsync().ConfigureAwait(false);

    List<KernelFunction> functionPrompts = new List<KernelFunction>();

    foreach (var p in prompts)
    {
        Console.WriteLine($"{p.Name}: {p.Description}");

        // compose parameters list, if any
        Dictionary<string, object?> promptArguments = new Dictionary<string, object?>();

        if (p.ProtocolPrompt.Arguments is not null)
        {
            foreach (ModelContextProtocol.Protocol.PromptArgument argument in p.ProtocolPrompt.Arguments)
            {
                if (argument.Required.HasValue && argument.Required.Value)
                {
                    // simplification here
                    // we assume that the only prompt argument from the list is the ageThreshold which we are hard coding to "65"
                    promptArguments.Add(argument.Name, "65");
                }
                else
                {
                    promptArguments.Add(argument.Name, string.Empty);
                }
            }
        }

        var promptResult = await mcpToolboxClient.GetPromptAsync(p.Name, promptArguments);

        var promptTemplate = string.Join("\n", promptResult.Messages.Select(m => m.Content));

        var semanticFunction = KernelFunctionFactory.CreateFromPrompt(
            promptTemplate: promptTemplate,
            executionSettings: (PromptExecutionSettings?)null, // Explicit cast to resolve ambiguity
            functionName: p.Name,
            description: promptResult.Description,
            templateFormat: "semantic-kernel"
        );

        functionPrompts.Add(semanticFunction);
    }

    if (functionPrompts.Any())
    {
        kernel.Plugins.AddFromFunctions("from_prompts", functionPrompts);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading prompts: {ex.Message}");
}
finally
{
    Console.WriteLine("// --");
}

var history = new ChatHistory();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

Console.Write("User > ");
string? userInput;

while ((userInput = Console.ReadLine()) is not null)
{
    // Add user input
    history.AddUserMessage(userInput);

    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,

    };

    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.WriteLine("Assistant > " + result);

    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);

    // Get user input again
    Console.Write("User > ");
}
