# **Architectural Implementation of the Model Context Protocol in Azure AI Foundry Using.NET**

## **The Context Assembly Paradigm and the Emergence of the Model Context Protocol**

The capabilities of Large Language Models (LLMs) are intrinsically bound by the quality, relevance, and timeliness of the context supplied during inference. As artificial intelligence systems transition from isolated conversational agents to autonomous, action-oriented systems, the traditional mechanisms for context assembly have proven brittle, inefficient, and unscalable.1 Historically, application logic required developers to manually fetch data from disparate enterprise databases, clean and format the information, and inject it into the model prompt prior to generation.1 This paradigm introduces significant latency, friction, and systemic fragility, while simultaneously obscuring the provenance of the data from the reasoning engine.1

While the advent of function calling mitigated some of these issues by allowing models to dictate tool invocation and dynamically request data, it inadvertently spawned a massive integration problem.1 Custom wrapper services, bespoke API connections, and unique authentication flows were required for every new tool introduced to the agent, leading to duplicated integration logic, increased maintenance overhead, and complex scaling challenges across enterprise environments.1 Developers were forced to build one-off tool integrations for every application, resulting in a fragmented ecosystem where tools were not easily discoverable or reusable across different AI clients.3

The Model Context Protocol (MCP) resolves these architectural bottlenecks by introducing an open, standardized specification that acts as a universal communication layer between AI models and external data sources or execution environments.4 Developed as an open standard by Anthropic and heavily supported by Microsoft, MCP functions analogously to a universal hardware adapter—often compared to a USB-C port for AI applications—standardizing how context is shared, tools are invoked, and workflows are composed.5 Operating on a client-server architecture utilizing JSON-RPC 2.0 messages, the protocol abstracts the underlying complexity of tool integration, allowing the AI model to act as a client that dynamically discovers and interacts with resources hosted behind a smart middle layer.2 This structured communication ensures that the AI model does not interact with external systems directly and chaotically, but rather through a controlled, discoverable, and secure interface.2

## **Core Architectural Primitives of the Specification**

The MCP specification defines three foundational primitives that dictate the capabilities a server can expose to an AI client.7 These primitives form the contract between the reasoning engine and the external environment, specifying the exact types of contextual information that can be shared and the range of actions that can be performed.7

| Primitive | Definition | Architectural Role | Example Implementations |
| :---- | :---- | :---- | :---- |
| **Tools** | Executable functions invoked by the AI application to perform state-mutating actions or complex data retrieval.7 | Enables agentic autonomy and interaction with external systems. Allows the model to delegate specific computational or transactional tasks. | File system operations, database SQL queries, API POST requests, external service triggers.3 |
| **Resources** | Data sources that provide contextual information to the AI, typically exposed via Unique Resource Identifiers (URIs).7 | Supplies read-only context without requiring explicit tool execution, functioning as the model's external memory or knowledge base. | File contents, live database schemas, static API JSON responses, log file streams.7 |
| **Prompts** | Reusable templates and predefined workflows surfaced to the user or model to standardize complex interactions.7 | Structures interactions and guides the LLM through complex cognitive workflows, providing explicit instructions or few-shot examples.7 | System instructions, diagnostic templates, code review guidelines, daily planner workflows.7 |

By decoupling the underlying data sources and execution logic from the AI client, MCP allows developers to build composable integrations.4 The AI model no longer needs to possess intrinsic knowledge of how to query a specific SQL dialect or authenticate against a proprietary REST API; it simply reads the tool descriptions provided by the MCP server and issues standardized JSON-RPC execution requests.3

## **Protocol Lifecycle and Transport Mechanisms**

The communication between an MCP client and server follows a strict lifecycle designed to establish trust, discover capabilities, and execute requests securely.4

When a client initiates a connection, it transmits an InitializeRequest to the server.10 This handshake phase allows both parties to negotiate protocol versions and establish the authentication context.10 Upon successful initialization, the client typically issues a ListToolsRequest, prompting the server to return a comprehensive inventory of its available tools, resources, and prompts, alongside their respective JSON schemas.10 These schemas serve as the structural contract, defining the exact parameters the LLM must provide when invoking a specific tool.12

The physical layer of this communication is governed by the transport mechanism. The MCP specification supports different transport layers depending on the deployment topology.13

| Transport Mechanism | Description | Architectural Use Case | Security and Networking Implications |
| :---- | :---- | :---- | :---- |
| **STDIO (Standard Input/Output)** | Communication occurs over the standard input and output streams of a local process.8 | Local deployments, sidecar containers, and IDE extensions (e.g., Visual Studio Code Copilot agent mode).8 | Requires strict isolation of logging. All protocol messages must route to stdout, while diagnostics and errors must strictly route to stderr to prevent JSON-RPC corruption.8 |
| **Streamable HTTP / Server-Sent Events (SSE)** | Communication utilizes standard HTTP requests for client-to-server messages and SSE for server-to-client events.5 | Distributed cloud deployments, enterprise-grade remote servers, and scalable microservices.8 | Enables servers to reside behind API gateways, supporting OAuth 2.1, IP whitelisting, rate limiting, and centralized JWT validation.18 |

For production-grade enterprise applications, the architecture must decouple the client from the server utilizing the Streamable HTTP transport.8 This decoupled approach enables the MCP server to scale horizontally as an independent microservice, running in isolated containers or serverless environments, rather than being bound to the lifecycle of the client application.8

## **Microsoft Azure AI Foundry and Agent Service Integration**

Microsoft has deeply integrated the MCP standard into its Azure cloud ecosystem, positioning it as the primary mechanism for building tool-aware, autonomous agents.6 The Azure AI Agent Service utilizes MCP to securely access enterprise knowledge, internal data stores such as Azure AI Search and SharePoint, and real-time web data via Microsoft Bing.21 This integration transforms AI agents from isolated text generators into highly capable orchestrators that can act upon enterprise infrastructure.20

Deploying and connecting an MCP server within Azure AI Foundry involves a structured provisioning and registration topology.22 The ecosystem supports multiple pathways for binding an AI agent to an MCP server, ensuring architectural flexibility across diverse enterprise governance and compliance requirements.22

### **Registration and Discovery Topologies**

When a remote MCP server is deployed to Azure—typically hosted on Azure Functions or Azure App Service—it must be logically connected to the Foundry Agent Service to be discoverable by the organization's LLMs.19

The most robust approach for large organizations is utilizing the Organizational Tool Catalog powered by Azure API Center.22 Registering the MCP server as an asset within the API Center creates a centralized, private catalog that enables widespread discovery and governance.22 Administrators define environments, deployments, and security schemes—such as OAuth 2.0 or API Key requirements—directly within the API Center governance interface.22 When developers build agents within the Foundry portal, they can browse this catalog, confident that the tools they are integrating comply with enterprise security standards and are actively managed.22

Alternatively, for rapid prototyping or project-specific implementations, developers can utilize Custom MCP Tool Integration.22 This method bypasses the centralized catalog, allowing developers to directly bind the remote MCP server endpoint (e.g., https://{function\_app\_name}.azurewebsites.net/runtime/webhooks/mcp) to their specific Foundry project.22 Authentication in this localized topology is typically managed via project-level credentials, such as directly providing the x-functions-key for Azure Functions authentication.22 While this accelerates development, it lacks the cross-project visibility and centralized governance provided by the API Center.22

### **The Agent Invocation Lifecycle**

The interaction flow within Azure AI Foundry begins when an agent—initialized via the PersistentAgentsClient in.NET or AIProjectClient in Python—receives a user instruction through a conversational thread.22 The Foundry Agent Service evaluates the prompt against the agent's system instructions and the available tools defined during the project configuration.22

If the underlying reasoning engine (e.g., a GPT-4o model deployment) determines that an MCP tool is required to formulate an accurate response or execute a requested action, the model outputs a tool invocation command.22 The Foundry Agent Service intercepts this command, orchestrates the secure JSON-RPC call over HTTP to the remote MCP server, passes the dynamically generated arguments, and awaits the response.5 Upon receiving the structured output from the MCP server, the Agent Service injects this data back into the model's context window, allowing the LLM to synthesize the final response or determine if subsequent tool calls are necessary.22 This entire execution loop abstracts the complexities of network transport and authentication from both the developer and the model, ensuring seamless operational flow.5

## **Developing Production-Grade MCP Servers in.NET**

Recognizing the widespread adoption of C\# in enterprise environments, Microsoft collaborated with Anthropic to develop an official, Tier-1 C\# SDK for the Model Context Protocol.6 Hosted within the modelcontextprotocol/csharp-sdk repository and distributed via the ModelContextProtocol NuGet package, this library provides the foundational abstractions necessary for building robust clients and servers.6 The SDK leverages the massive performance improvements intrinsic to modern.NET, offering excellent speed, memory efficiency, and asynchronous execution capabilities critical for high-throughput AI applications.6

### **The Generic Host Pattern and Server Initialization**

To align with modern.NET architectural standards, production-grade MCP servers should eschew monolithic program structures in favor of the.NET Generic Host pattern.8 This approach ensures that the server benefits from built-in Dependency Injection (DI), robust configuration management, structured logging, and managed application lifecycles.8

The server pipeline is configured via intuitive IServiceCollection extensions provided by the SDK.24 The AddMcpServer() method initializes the server core, while subsequent chain calls define the transport mechanism and capability discovery.3 A standard initialization sequence specifies the transport—such as WithHttpTransport() for distributed deployments or WithStdioServerTransport() for local execution—and utilizes reflection to automatically map the tools and prompts distributed across the application's assemblies.3 Furthermore, incorporating the IHttpContextAccessor into the service bindings allows tools to access the underlying HTTP request details, enabling advanced scenarios such as parsing custom headers or validating configuration contexts dynamically during tool execution.25

### **Exposing Capabilities via Attribute-Based Routing**

The.NET SDK utilizes an elegant, attribute-driven design to map standard C\# methods to MCP JSON-RPC endpoints. This metadata-rich approach shifts the burden of protocol serialization away from the developer; the SDK automatically generates the necessary JSON schemas required by the LLM for tool invocation using reflection.3

To define an executable tool, the hosting class is decorated with the attribute, while the specific execution method is marked with.3 The \`\` attribute plays a paramount role in this architecture.3 It serves as the explicit instructional prompt for the LLM, dictating precisely when the tool should be utilized, the exact nature of the operation it performs, and the semantic meaning of each parameter.3 Without highly precise descriptions, the reasoning engine will fail to invoke the tool correctly, or worse, hallucinate parameters.8

When building complex backend integrations, tools often require access to external services, such as a database context or an HTTP client. The.NET SDK resolves these dependencies seamlessly by allowing developers to inject them directly into the tool method's signature.3 When the LLM requests a tool execution, the SDK's internal router parses the JSON arguments, resolves the required backend services from the Dependency Injection container, and invokes the method asynchronously.8 Prompts follow a parallel architectural pattern, utilizing and attributes to expose predefined workflows or cognitive strategies, allowing the client to retrieve standardized instructions.3

### **Semantic Kernel Integration and Agent Orchestration**

For organizations already deeply invested in Microsoft's Semantic Kernel framework, MCP tools can be natively adapted into the kernel's execution pipeline, bridging the gap between local semantic agents and remote enterprise tools.27 The integration is facilitated by the Microsoft.SemanticKernel.Core package, which provides mechanisms to wrap external MCP tools as native KernelFunction abstractions.28

By establishing a connection to the remote MCP server using the McpClientFactory and iterating over the retrieved tools, developers can invoke kernel.Plugins.AddFromFunctions().28 This operation seamlessly merges the MCP server's capabilities into the local semantic agent's toolset.28 Consequently, when the Semantic Kernel orchestrator evaluates a user prompt, it can automatically determine whether to invoke a local plugin or route the execution to the remote MCP server.27 The framework handles the HTTP call, awaits the MCP server's response, and continues the prompt execution pipeline without the developer writing bespoke networking code.27

## **Advanced Protocol Capabilities: Sampling and Elicitation**

Beyond simple, unidirectional request-response tool execution, the Model Context Protocol defines advanced, bi-directional interaction paradigms that enable inversion of control and sophisticated human-in-the-loop workflows.8 These capabilities transition the MCP server from a passive data repository to an active participant in the AI reasoning process.8

### **Inversion of Control via Sampling**

Sampling is a powerful mechanism by which the MCP server requests the connected AI client to perform reasoning, summarization, or generation on its behalf.8 This architectural inversion is particularly vital when a server-side tool extracts unstructured data—such as scraping a massive web page or retrieving a raw text document—and requires the LLM's natural language processing capabilities to distill the information before continuing its internal execution logic.8

In the.NET SDK implementation, sampling is achieved by injecting the IMcpServer interface directly into the tool method's parameters.23 The server utilizes the AsSamplingChatClient() extension method to construct and transmit a ChatMessage payload back to the client over the established transport layer.23

When the tool executes the asynchronous call to GetResponseAsync(messages, options, cancellationToken), the local execution thread suspends.23 A JSON-RPC sampling request is dispatched to the client, instructing the client's underlying LLM to process the sub-task according to the provided temperature and token parameters.23 Once the model generates the response, the resulting string is returned across the protocol boundary to the waiting.NET method, which resumes execution and utilizes the summarized data to complete the primary tool operation.23 This intricate pattern maintains deterministic, secure execution on the server side while dynamically leveraging the cognitive reasoning power of the client.8

### **Human-in-the-Loop via Elicitation**

Elicitation addresses a critical vulnerability in autonomous AI systems: the handling of missing information and the absolute necessity for explicit human consent before executing highly sensitive or irreversible actions.12 When an MCP server determines that a user's prompt lacked required parameters, or when a tool represents a "destructive" scope—such as executing a database mutation, modifying file systems, or triggering a financial transaction—it initiates an elicitation/create request rather than returning a failure state.8

The elicitation protocol transmits a strongly typed JSON Schema outlining the missing fields or the pending action to the client.12 The server's execution halts, returning an awaited Task in.NET, while the client application intercepts the request and renders a user interface prompt to the human operator.12 The client subsequently responds with a specific action payload containing an accept, decline, or cancel directive, alongside any requested data.12

| Elicitation Action State | Client Behavior | Server Processing Logic |
| :---- | :---- | :---- |
| **Accept** | The human explicitly approves the request and submits the required schema data.12 | The server resumes execution, utilizing the provided data to complete the tool invocation safely.31 |
| **Decline** | The human explicitly rejects the request.12 | The server aborts the current tool execution and returns a structured failure or cancellation message to the LLM, prompting it to alter its strategy.35 |
| **Cancel** | The human dismisses the UI prompt without making an explicit choice.12 | The server treats the operation as timed out or aborted, ensuring no destructive actions proceed without affirmative consent.35 |

In the.NET SDK, this human-in-the-loop workflow is managed via the ElicitAsync extension method on the IMcpServer interface.36 The server defines the required primitive types (such as strings, numbers, or booleans) and awaits the user's input before proceeding with the operation.36 This paradigm ensures that safety-critical guardrails are strictly enforced at the server level, effectively preventing rogue agentic behavior and maintaining human oversight over enterprise automation.8

## **High-Performance Asynchronous Architecture in.NET**

Because MCP servers act as the central integration layer between high-throughput AI swarm agents and legacy backend systems, the performance characteristics of the.NET implementation are of paramount importance. Inefficient thread management, blocking I/O calls, or excessive garbage collection can introduce unacceptable latency into the LLM's response loop, severely degrading the user experience.37 Optimizing the asynchronous code paths is essential for building production-ready architectures.37

### **ValueTask and Memory Allocation Optimization**

In scenarios where an MCP tool retrieves data from an external API or database, the operation may frequently complete synchronously if the requested data is already present in a distributed cache, such as Azure Managed Redis. Returning a standard Task\<T\> in such synchronous paths requires the Common Language Runtime (CLR) to allocate a new object on the managed heap.37 Under the high load generated by autonomous agents repeatedly polling tools, this leads to significant Garbage Collector (GC) pressure, causing application pauses.37

The application of ValueTask\<T\> entirely eliminates this allocation overhead for synchronous completion paths.38 By returning a lightweight value type struct that encapsulates either a successfully completed result or a pending Task, the.NET runtime avoids unnecessary heap allocations.38 This optimization is vital for the high-frequency tool executions typical of multi-agent architectures, ensuring scalable and highly responsive software.37

### **Streaming Context via IAsyncEnumerable**

When an MCP tool is tasked with returning massive datasets—such as multi-page database queries, comprehensive log aggregations, or large document analyses—loading the entire payload into the server's memory before transmission violates fundamental scalability principles.37 The implementation of the IAsyncEnumerable\<T\> interface facilitates highly efficient, asynchronous data streaming.37

By yielding records iteratively utilizing the yield return keyword within an async stream, the MCP server processes and dispatches data chunks individually without blocking the executing thread.14 This is particularly advantageous when utilizing Streamable HTTP transports, as it allows the downstream AI client to begin parsing and reasoning over the context stream immediately, long before the complete dataset has been materialized or fetched from the backend database.14

### **Concurrency Throttling and Deadlock Prevention**

Integrating legacy synchronous code, complex CPU-bound operations, or external I/O operations into an asynchronous MCP server requires robust concurrency control.37 The utilization of SemaphoreSlim is highly recommended to throttle simultaneous I/O operations.37 This limits the maximum number of concurrent executions, preventing backend databases or external rate-limited APIs from being overwhelmed by an AI agent that decides to execute a tool concurrently in a loop.37

Furthermore, all library-level await calls within the MCP tool implementations must append .ConfigureAwait(false).37 This practice explicitly instructs the.NET runtime that the asynchronous continuation does not need to marshal back to the original synchronization context.37 In environments with constrained thread pools or complex synchronization contexts, this aggressively prevents deadlocks and significantly improves overall request throughput by freeing threads faster.37

## **Security, Authentication, and Governance**

Securing an MCP server involves a multi-layered approach to mitigate unauthorized tool execution, prevent context poisoning, and ensure that AI agents operate strictly within their cryptographically assigned permissions. Given that the MCP server directly interacts with sensitive enterprise systems, robust identity and access management (IAM) is not optional; it is the cornerstone of the architecture.19

### **Authentication Topologies for MCP Servers**

Azure AI Foundry supports multiple authentication schemas for MCP servers, each catering to different persistence requirements, zero-trust models, and user context scenarios.22

| Authentication Method | Implementation Profile | User Context Persistence | Optimal Architectural Scenario |
| :---- | :---- | :---- | :---- |
| **Key-Based Authentication** | API keys or Personal Access Tokens (PAT) transmitted via headers (e.g., x-functions-key or Authorization: Bearer).22 | None. A shared identity is utilized across all agents interacting with the server.22 | Internal microservices, strictly scoped Azure Functions where agents act as automated system processes rather than human proxies.22 |
| **Microsoft Entra ID (Managed Identity)** | Role-Based Access Control (RBAC) leveraging an Agent Identity or a Project Managed Identity.11 | None. The identity and its associated permissions are scoped entirely to the agent instance or the overarching project.22 | Cloud-native deployments where the MCP server runs on Azure infrastructure (e.g., App Service) requiring seamless, secretless token rotation.22 |
| **OAuth 2.1 Identity Passthrough** | Delegated access tokens fetched via the OAuth 2.0 authorization code flow, enforcing user-specific scopes.22 | Yes. The agent operates strictly on behalf of the specific authenticated human user, utilizing their exact permissions.22 | Environments demanding strict data compartmentalization, compliance auditability, and Zero Trust architectures where an agent must not access data the human cannot.19 |

### **Protected Resource Metadata (PRM) and Authorization Server Metadata (ASM)**

To implement dynamic, secure OAuth 2.1 connections, the MCP server must broadcast its authentication requirements to the client before any sensitive data requests are made. This pre-flight security handshake is handled via Protected Resource Metadata (PRM), a standard defined in OAuth 2.0 drafts (RFC 9728).11

When an Azure Foundry agent attempts to initialize an unauthenticated session with an MCP server utilizing OAuth passthrough, the server actively rejects the request and returns an error payload containing a pointer to its PRM.11 The PRM is a standardized JSON payload that informs the client which specific Authorization Server the MCP server trusts, which token formats it accepts, and what specific cryptographic scopes are required to execute the tools.43 In.NET applications hosted on Azure App Service or Azure Functions, PRM is configured by defining the required scopes within the WEBSITE\_AUTH\_PRM\_DEFAULT\_WITH\_SCOPES application environment variable.44

Upon receiving the PRM pointer, the client retrieves the Authorization Server Metadata (ASM) from the OpenID Connect well-known endpoint (e.g., /.well-known/oauth-authorization-server).11 The agent then initiates an On-Behalf-Of (OBO) flow, rendering a UI to prompt the human user to sign in and explicitly consent to the requested scopes.11 Once consent is granted by the user, the authorization server issues a delegated access token.42 The client then retries the InitializeRequest to the MCP server, this time including the access token, ensuring that the MCP server processes all subsequent tool executions strictly under the user's specific geographic and organizational permissions.11 This design prevents agents from exploiting application-level identities to bypass user-level security constraints.42

### **Boundary Enforcement and the Principle of Least Privilege**

Beyond authentication, MCP servers must be designed as strictly bounded contexts.49 The server should adhere to the principle of least privilege, ensuring that the service account or Managed Identity under which it operates possesses only the exact granular permissions necessary to execute its exposed tools, and nothing more.22

For servers interacting with local file systems or storage blobs, the MCP specification includes the Roots primitive.8 Roots define absolute, immutable boundaries on the host machine or virtual file system.8 Implementing Roots ensures that an autonomous agent cannot perform malicious directory traversal attacks or accidentally wander into sensitive system folders to access configuration files outside of its strictly designated workspace.8

## **Observability and Telemetry via OpenTelemetry**

The architectural complexity of tracing a request from an initial user prompt, through an LLM reasoning and planning cycle, across an MCP client-server transport boundary, and finally down to a backend database transaction necessitates highly advanced distributed tracing capabilities.50 Relying solely on standard output logging or basic application logs is vastly insufficient for debugging or maintaining production MCP systems.51

The modern industry standard for comprehensive observability in.NET MCP servers is OpenTelemetry (OTel).50 OpenTelemetry provides a vendor-neutral instrumentation framework that generates and correlates telemetry data—metrics, logs, and traces—across complex distributed boundaries.50

Within the.NET Generic Host architecture, OpenTelemetry is configured by importing the Azure.Monitor.OpenTelemetry.AspNetCore NuGet package and injecting it into the service collection.53

C\#

if (\!string.IsNullOrEmpty(builder.Configuration))   
{   
    builder.Services.AddOpenTelemetry().UseAzureMonitor();   
}

This configuration automatically provisions the necessary instrumentation libraries, transparently converting standard.NET ActivitySource and Meter events into highly structured distributed spans and metrics.53

### **Azure Monitor Application Insights and Agent Details View**

Azure Monitor elegantly maps these OpenTelemetry primitives to its Application Insights features. Server Spans are recorded as Requests, Other Span Types become Dependencies, and Trace IDs ensure that an error occurring in a downstream database query can be deterministically linked back to the exact JSON-RPC payload sent by the LLM during the tool execution phase.53

Furthermore, Azure AI Foundry integrates directly with Application Insights to provide a specialized Agent Details view.55 Based explicitly on OpenTelemetry Generative AI Semantics, this view consolidates complex metrics such as token consumption, reasoning phase duration, and tool invocation latency into a unified dashboard.55 To fully leverage this, deploying a centralized MCP gateway between the agents and the various backend servers is highly recommended.51 This centralized architecture ensures that all audit logs possess deep contextual metadata rather than mere stack traces, granting administrators true visibility into agentic behavior and performance bottlenecks.51

## **Deployment Strategies and Production Best Practices**

Deploying a.NET MCP server into a production environment requires selecting a hosting architecture that carefully balances rapid scalability, robust security, and seamless lifecycle management. Azure Functions provides an optimal, serverless architecture specifically tailored for hosting remote MCP servers.17

### **Azure Functions Hosting Models**

There are two primary paradigms for hosting MCP servers on Azure Functions, each catering to different development workflows and architectural needs:

1. **The MCP Extension (Bindings-Based):** This approach utilizes the native Azure Functions MCP extension.22 Developers leverage an \`\` attribute to bind tool executions directly to specific function executions.22 This method is deeply integrated into the Azure Functions bindings-based programming model and abstracts the underlying transport protocol entirely, making it highly ideal for teams already heavily invested in the traditional Azure Functions ecosystem.22  
2. **Self-Hosted SDK Servers (Custom Handlers):** For teams utilizing the official C\# SDK to build cross-platform MCP servers, the server is compiled as a standalone.NET web host.22 It is then deployed onto the Azure Functions Flex Consumption plan utilizing the Custom Handler architecture.22 In this model, the.NET application acts as a lightweight web server intercepting events directly from the Functions host.22 This model allows the use of standard Streamable HTTP transports, providing decoupled, container-like behavior while still maintaining the powerful serverless scale-to-zero economics and burst scaling capabilities of Azure Functions.17

Automated deployment pipelines for these models are typically managed using the Azure Developer CLI (azd), utilizing infrastructure-as-code files (like Bicep or Terraform) to provision the necessary compute resources, establish virtual networking, and deploy the compiled.NET binaries securely.22

### **Engineering for Reliability: Statelessness and Idempotency**

To ensure the unwavering reliability of MCP servers in production environments, several fundamental architectural best practices must be strictly enforced.49

Foremost, MCP servers must be designed to be strictly stateless.8 Because the MCP server acts merely as a communication bridge, any requisite state—such as complex multi-turn workflow steps, pagination cursors, or intermediate processing IDs—must not be stored in the server's local memory.8 Instead, this state must be returned to the client and explicitly passed back as arguments in subsequent tool calls.8 This architectural mandate guarantees that the server can scale horizontally under load and maintain horizontal fault tolerance without complex state synchronization.8

Furthermore, tool operations must be rigorously idempotent.8 The non-deterministic nature of LLM generation occasionally results in duplicated tool invocations, unexpected retry loops, or malformed requests.49 If a tool mutates a database, triggers an email, or provisions a cloud resource, it must implement internal logic to verify whether the action has already occurred for a given context.49 This prevents catastrophic infrastructure drift, data corruption, or redundant transactions.49

Finally, the API layer exposed by the MCP server should mirror the exact engineering rigor applied to traditional enterprise microservices.49 Tools should enforce eventual consistency where appropriate, implement circuit breakers to gracefully degrade upon downstream dependency failures, and return highly structured, semantic error messages.49 Returning simple stack traces to an LLM is ineffective; semantic error messages allow the reasoning engine to accurately interpret the failure mechanism and dynamically attempt an alternative reasoning path or tool invocation strategy.49

By strictly adhering to the Model Context Protocol specification, aggressively utilizing the high-performance features of modern.NET, and securing the deployment via Azure Entra ID and the API Center, organizations can establish a highly robust, scalable, and secure foundation for deploying their next generation of tool-aware, autonomous AI systems.

#### **Works cited**

1. MCP (Model Context Protocol): The Missing Layer in AI Tool Integration | by Partha Das | Jan, 2026, accessed February 23, 2026, [https://medium.com/@apartha77/mcp-model-context-protocol-the-missing-layer-in-ai-tool-integration-8d764119f23a](https://medium.com/@apartha77/mcp-model-context-protocol-the-missing-layer-in-ai-tool-integration-8d764119f23a)  
2. MCP Server Explained: A Beginner-Friendly Guide to Model Context Protocol | by Sachin | Feb, 2026, accessed February 23, 2026, [https://medium.com/@sachintechnossus/mcp-server-explained-a-beginner-friendly-guide-to-model-context-protocol-f90cd38c34ef](https://medium.com/@sachintechnossus/mcp-server-explained-a-beginner-friendly-guide-to-model-context-protocol-f90cd38c34ef)  
3. How to Build Your Own MCP Server in .NET for AI Tools | by Diego ..., accessed February 23, 2026, [https://medium.com/@dianper/how-to-connect-your-apis-to-llms-a-deep-dive-into-model-context-protocol-mcp-4a596fea343c](https://medium.com/@dianper/how-to-connect-your-apis-to-llms-a-deep-dive-into-model-context-protocol-mcp-4a596fea343c)  
4. Specification \- Model Context Protocol, accessed February 23, 2026, [https://modelcontextprotocol.io/specification/2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25)  
5. MCP Server Integration with Foundry Agents | by Badr Kacimi | Feb, 2026, accessed February 23, 2026, [https://medium.com/@badrvkacimi/mcp-server-integration-with-foundry-agents-d140729dcfec](https://medium.com/@badrvkacimi/mcp-server-integration-with-foundry-agents-d140729dcfec)  
6. Microsoft partners with Anthropic to create official C\# SDK for Model Context Protocol, accessed February 23, 2026, [https://developer.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol](https://developer.microsoft.com/blog/microsoft-partners-with-anthropic-to-create-official-c-sdk-for-model-context-protocol)  
7. Architecture overview \- Model Context Protocol, accessed February 23, 2026, [https://modelcontextprotocol.io/docs/learn/architecture](https://modelcontextprotocol.io/docs/learn/architecture)  
8. MCP Deep Dive (Part 1): Building the Hands and Eyes of an AI Agent in C\# | by Alon Fliess, accessed February 23, 2026, [https://medium.com/@alonfliess/mcp-deep-dive-part-1-building-the-hands-and-eyes-of-an-ai-agent-in-c-b26e71ebe102](https://medium.com/@alonfliess/mcp-deep-dive-part-1-building-the-hands-and-eyes-of-an-ai-agent-in-c-b26e71ebe102)  
9. Prompts \- Model Context Protocol （MCP）, accessed February 23, 2026, [https://modelcontextprotocol.info/docs/concepts/prompts/](https://modelcontextprotocol.info/docs/concepts/prompts/)  
10. Get started with .NET AI and MCP \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)  
11. Tutorial: Host an MCP server on Azure Functions \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/azure-functions/functions-mcp-tutorial](https://learn.microsoft.com/en-us/azure/azure-functions/functions-mcp-tutorial)  
12. MCP Elicitation: Human-in-the-Loop for MCP Servers \- DZone, accessed February 23, 2026, [https://dzone.com/articles/mcp-elicitation-human-in-the-loop-for-mcp-servers](https://dzone.com/articles/mcp-elicitation-human-in-the-loop-for-mcp-servers)  
13. SDKs \- Model Context Protocol, accessed February 23, 2026, [https://modelcontextprotocol.io/docs/sdk](https://modelcontextprotocol.io/docs/sdk)  
14. model-context-protocol-resources/guides/mcp-server-development-guide.md at main, accessed February 23, 2026, [https://github.com/cyanheads/model-context-protocol-resources/blob/main/guides/mcp-server-development-guide.md](https://github.com/cyanheads/model-context-protocol-resources/blob/main/guides/mcp-server-development-guide.md)  
15. Azure MCP Server \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/overview](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/overview)  
16. Building a Model Context Protocol Server with .NET and Semantic Kernel Integration, accessed February 23, 2026, [https://systenics.ai/blog/2025-04-10-building-a-model-context-protocol-server-with-net-and-semantic-kernel-integration/](https://systenics.ai/blog/2025-04-10-building-a-model-context-protocol-server-with-net-and-semantic-kernel-integration/)  
17. Host remote MCP servers on Azure Functions, accessed February 23, 2026, [https://www.youtube.com/watch?v=4Nc0q0mRK1c](https://www.youtube.com/watch?v=4Nc0q0mRK1c)  
18. Secure access to MCP servers in Azure API Management | Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/api-management/secure-mcp-servers](https://learn.microsoft.com/en-us/azure/api-management/secure-mcp-servers)  
19. Building an authenticated MCP server with Microsoft Entra and .NET, accessed February 23, 2026, [https://www.developerscantina.com/p/mcp-entra-dotnet/](https://www.developerscantina.com/p/mcp-entra-dotnet/)  
20. Driving agentic innovation w/ MCP as the backbone of tool-aware AI, accessed February 23, 2026, [https://www.youtube.com/watch?v=Wt1-u8wD\_Xs](https://www.youtube.com/watch?v=Wt1-u8wD_Xs)  
21. Introducing Model Context Protocol (MCP) in Azure AI Foundry: Create an MCP Server with Azure AI Agent Service \- Microsoft Dev Blogs, accessed February 23, 2026, [https://devblogs.microsoft.com/foundry/integrating-azure-ai-agents-mcp/](https://devblogs.microsoft.com/foundry/integrating-azure-ai-agents-mcp/)  
22. Build and register a Model Context Protocol (MCP) server \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/ai-foundry/mcp/build-your-own-mcp-server?view=foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/mcp/build-your-own-mcp-server?view=foundry)  
23. modelcontextprotocol/csharp-sdk: The official C\# SDK for Model Context Protocol servers and clients. Maintained in collaboration with Microsoft. \- GitHub, accessed February 23, 2026, [https://github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)  
24. Build & Leverage MCP Servers in C\# for AI-Driven Development \- Uno Platform, accessed February 23, 2026, [https://platform.uno/blog/build-leverage-mcp-servers-in-c-for-ai-driven-development/](https://platform.uno/blog/build-leverage-mcp-servers-in-c-for-ai-driven-development/)  
25. How to Access Custom HTTP Headers in Your C\# MCP Server \- DEV Community, accessed February 23, 2026, [https://dev.to/\_neronotte/how-to-access-custom-http-headers-in-your-c-mcp-server-heb](https://dev.to/_neronotte/how-to-access-custom-http-headers-in-your-c-mcp-server-heb)  
26. MCPServer Tool Failing with no logging : r/dotnet \- Reddit, accessed February 23, 2026, [https://www.reddit.com/r/dotnet/comments/1ld9gih/mcpserver\_tool\_failing\_with\_no\_logging/](https://www.reddit.com/r/dotnet/comments/1ld9gih/mcpserver_tool_failing_with_no_logging/)  
27. Building a Model Context Protocol Server with Semantic Kernel \- Microsoft Dev Blogs, accessed February 23, 2026, [https://devblogs.microsoft.com/semantic-kernel/building-a-model-context-protocol-server-with-semantic-kernel/](https://devblogs.microsoft.com/semantic-kernel/building-a-model-context-protocol-server-with-semantic-kernel/)  
28. Integrating Model Context Protocol Tools with Semantic Kernel: A Step-by-Step Guide, accessed February 23, 2026, [https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/](https://devblogs.microsoft.com/semantic-kernel/integrating-model-context-protocol-tools-with-semantic-kernel-a-step-by-step-guide/)  
29. Building Your First MCP Server with C\# SDK: A Step-by-Step Guid \- Medium, accessed February 23, 2026, [https://medium.com/data-science-collective/i-created-my-first-mcp-server-with-net-c-mcp-sdk-step-by-step-guide-c28aefe5fc94](https://medium.com/data-science-collective/i-created-my-first-mcp-server-with-net-c-mcp-sdk-step-by-step-guide-c28aefe5fc94)  
30. How to use .NET Semantic Kernel agent with MCP server/client \- YouTube, accessed February 23, 2026, [https://www.youtube.com/watch?v=51zEUa0qQR0](https://www.youtube.com/watch?v=51zEUa0qQR0)  
31. How Elicitation in MCP Brings Human-in-the-Loop to AI Tools \- The New Stack, accessed February 23, 2026, [https://thenewstack.io/how-elicitation-in-mcp-brings-human-in-the-loop-to-ai-tools/](https://thenewstack.io/how-elicitation-in-mcp-brings-human-in-the-loop-to-ai-tools/)  
32. aaronpowell/modelcontextprotocol-csharp-sdk: The official C\# SDK for Model Context Protocol servers and clients, maintained by Microsoft \- GitHub, accessed February 23, 2026, [https://github.com/aaronpowell/modelcontextprotocol-csharp-sdk](https://github.com/aaronpowell/modelcontextprotocol-csharp-sdk)  
33. MCP server prompts not showing up in copilot agent mode \#157618 \- GitHub, accessed February 23, 2026, [https://github.com/orgs/community/discussions/157618](https://github.com/orgs/community/discussions/157618)  
34. Model Context Protocol: TypeScript SDKs for the Agentic AI ecosystem | Speakeasy, accessed February 23, 2026, [https://www.speakeasy.com/blog/release-model-context-protocol](https://www.speakeasy.com/blog/release-model-context-protocol)  
35. MCP Elicitation: Human-in-the-Loop for MCP Servers \- DEV Community, accessed February 23, 2026, [https://dev.to/kachurun/mcp-elicitation-human-in-the-loop-for-mcp-servers-m6a](https://dev.to/kachurun/mcp-elicitation-human-in-the-loop-for-mcp-servers-m6a)  
36. MCP C\# SDK Gets Major Update: Support for Protocol Version 2025-06-18 \- Dev Blogs, accessed February 23, 2026, [https://devblogs.microsoft.com/dotnet/mcp-csharp-sdk-2025-06-18-update/](https://devblogs.microsoft.com/dotnet/mcp-csharp-sdk-2025-06-18-update/)  
37. .NET Async Operations \- Claude Code Skill for C\# Devs \- MCP Market, accessed February 23, 2026, [https://mcpmarket.com/tools/skills/net-async-operations-expert](https://mcpmarket.com/tools/skills/net-async-operations-expert)  
38. C\# Async Patterns Claude Code Skill | AI Coding Assistant \- MCP Market, accessed February 23, 2026, [https://mcpmarket.com/tools/skills/c-async-patterns-1](https://mcpmarket.com/tools/skills/c-async-patterns-1)  
39. Tutorial: Generate and consume async streams using C\# and .NET \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream)  
40. On .NET Live \- Supercharge .NET with IAsyncEnumerables: Efficient Data Streams, accessed February 23, 2026, [https://www.youtube.com/watch?v=FbzZJ0pgobg](https://www.youtube.com/watch?v=FbzZJ0pgobg)  
41. Set up MCP server authentication \- Microsoft Foundry, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/mcp-authentication?view=foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/agents/how-to/mcp-authentication?view=foundry)  
42. Implement a secure MCP server using OAuth and Entra ID \- Software Engineering, accessed February 23, 2026, [https://damienbod.com/2025/09/23/implement-a-secure-mcp-server-using-oauth-and-entra-id/](https://damienbod.com/2025/09/23/implement-a-secure-mcp-server-using-oauth-and-entra-id/)  
43. Protected Resource Metadata for MCP Servers | by Mandar Kulkarni | Jan, 2026 \- Medium, accessed February 23, 2026, [https://medium.com/@mjkool/protected-resource-metadata-for-mcp-servers-eccddbe99b44](https://medium.com/@mjkool/protected-resource-metadata-for-mcp-servers-eccddbe99b44)  
44. Secure MCP servers with Microsoft Entra authentication \- Azure App Service, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-mcp-server-vscode](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-mcp-server-vscode)  
45. Configure MCP server authorization \- Azure App Service \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-mcp](https://learn.microsoft.com/en-us/azure/app-service/configure-authentication-mcp)  
46. Resolving Authorization server metadata URL omits the base path of the Authorization server URL · Issue \#545 · modelcontextprotocol/typescript-sdk \- GitHub, accessed February 23, 2026, [https://github.com/modelcontextprotocol/typescript-sdk/issues/545](https://github.com/modelcontextprotocol/typescript-sdk/issues/545)  
47. Get started with Foundry MCP Server (preview) using Visual Studio Code \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/ai-foundry/mcp/get-started?view=foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/mcp/get-started?view=foundry)  
48. Understanding OAuth2 and implementing identity-aware MCP servers | by Heeki Park, accessed February 23, 2026, [https://heeki.medium.com/understanding-oauth2-and-implementing-identity-aware-mcp-servers-221a06b1a6cf](https://heeki.medium.com/understanding-oauth2-and-implementing-identity-aware-mcp-servers-221a06b1a6cf)  
49. 15 Best Practices for Building MCP Servers in Production \- The New Stack, accessed February 23, 2026, [https://thenewstack.io/15-best-practices-for-building-mcp-servers-in-production/](https://thenewstack.io/15-best-practices-for-building-mcp-servers-in-production/)  
50. MCP Observability with OpenTelemetry \- Reddit, accessed February 23, 2026, [https://www.reddit.com/r/mcp/comments/1ltsq0y/mcp\_observability\_with\_opentelemetry/](https://www.reddit.com/r/mcp/comments/1ltsq0y/mcp_observability_with_opentelemetry/)  
51. What are some of your MCP deployment best practices? \- Reddit, accessed February 23, 2026, [https://www.reddit.com/r/mcp/comments/1o6iwip/what\_are\_some\_of\_your\_mcp\_deployment\_best/](https://www.reddit.com/r/mcp/comments/1o6iwip/what_are_some_of_your_mcp_deployment_best/)  
52. Application Insights OpenTelemetry observability overview \- Azure Monitor | Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)  
53. Example: Use OpenTelemetry with Azure Monitor and Application Insights \- .NET, accessed February 23, 2026, [https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-applicationinsights](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-applicationinsights)  
54. Application Insights OpenTelemetry data collection \- Azure Monitor | Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-overview](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-overview)  
55. Monitor AI Agents with Application Insights \- Azure \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/azure-monitor/app/agents-view](https://learn.microsoft.com/en-us/azure/azure-monitor/app/agents-view)  
56. Quickstart: Host servers built with MCP SDKs on Azure Functions \- Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/azure-functions/scenario-host-mcp-server-sdks](https://learn.microsoft.com/en-us/azure/azure-functions/scenario-host-mcp-server-sdks)  
57. Deploy Tools \- Azure MCP Server | Microsoft Learn, accessed February 23, 2026, [https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/azure-deploy](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/tools/azure-deploy)