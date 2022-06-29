
# Temporal SDK for .NET <br /> High-level Specification


This is a high-level specification. Many details are left out on purpose.  
We are very keen on feedback. Please try to keep it as high-level, as this document.  
If you have feedback on more detailed aspects, please use another channel ([info](https://dotnet.temporal.io/Articles/Contribution_Guide.html#please-talk-to-us)).  
<big>🧡</big> **Thank you!**

## Contents

<small><small>
<!-- TOC -->

- [Contents](#contents)
- [Define an Activity Implementation](#define-an-activity-implementation)
- [Define a Workflow Implementation](#define-a-workflow-implementation)
    - [Workflow routines: Return types & Sync/Async requirements](#workflow-routines-return-types--syncasync-requirements)
    - [Interfaces for Workflow Implementations](#interfaces-for-workflow-implementations)
- [Data exchange types & Workflow Input/Output](#data-exchange-types--workflow-inputoutput)
- [Workflow and Activity method Signatures](#workflow-and-activity-method-signatures)
- [Invoke workflows from a client](#invoke-workflows-from-a-client)
    - [String-based aka not-strongly-typed API](#string-based-aka-not-strongly-typed-api)
    - [Strongly-typed API](#strongly-typed-api)
        - [Create a workflow stub and use it to execute the workflow from start to conclusion](#create-a-workflow-stub-and-use-it-to-execute-the-workflow-from-start-to-conclusion)
        - [Use a stub to interact with an existing workflow](#use-a-stub-to-interact-with-an-existing-workflow)
        - [Stub method overloads](#stub-method-overloads)
        - [Additional workflow stub features](#additional-workflow-stub-features)
- [Invoke activities from within a workflow](#invoke-activities-from-within-a-workflow)
    - [String-based aka not-strictly-typed API](#string-based-aka-not-strictly-typed-api)
    - [Strongly-typed API](#strongly-typed-api)
- [Invoke child workflows from a within a workflow](#invoke-child-workflows-from-a-within-a-workflow)
    - [String-based aka not-strongly-typed API](#string-based-aka-not-strongly-typed-api)
    - [Strongly-typed API](#strongly-typed-api)
- [Register a worker](#register-a-worker)
        - [Dealing with fatal worker errors](#dealing-with-fatal-worker-errors)

<!-- /TOC -->

</small></small>

## Define an Activity Implementation

An _Activity Implementation_ is any method (<small>some restrictions are discussed in a [latter section](#workflow-and-activity-method-signatures)</small>).  

Static and instance methods are permitted:
```cs
public static class Utterances
{
    public static void SayGoodBye(UtteranceInfo utterSpec)  // <- This is an activity
    {
        // ...
    }
}

public class CustomLogger
{
    public void LogEvent(EventInfo eventData)  // <- This is an activity
    {
        // ...
    }
}
```

Sync and Async methods are permitted:
```cs
public static class Utterances
{
    public static void SayGoodBye(UtteranceInfo utterSpec)  // <- This is an activity
    {
        // ...
    }

    public static async Task SayHelloAsync(UtteranceInfo utterSpec)   // <- This is an activity
    {
        // ...
    }
}
```

Activity methods can _optionally_ be decorated with an `[ActivityImplementation]`-attribute.  

* <small>Reasons for why decorating with `[ActivityImplementation]` is _optional_:</small>
  - Allow using existing methods as activities.
  - No need for unnecessary scaffolding and unnecessary wrapping.  

* <small>Scenarios addressed by `[ActivityImplementation]`-attributes:</small>
   - Activities can be auto-discovered (bulk-registered) at some scope (e.g. Type or Assembly).
   - Activities can be marked with a _default Activity Type Name_.  
   <small>(If an `[ActivityImplementation]`-attribute is not used, the _default Activity Type Name_ is generated based on the activity method name.)</small>

`[ActivityImplementation]`-examples:
```cs
public static class Utterances
{
    // The default Activity Type Name will be "SayGoodBye":
    [ActivityImplementation]
    public static void SayGoodBye(UtteranceInfo utterSpec)
    {
        // ...
    }

    // The default Activity Type Name will be "HowAreYou":
    [ActivityImplementation(ActivityTypeName="HowAreYou")]
    public static QuestionResponseInfo AskHowAreYouPolitely(UtteranceInfo utterSpec)
    {
        // ...
    }

    // The default Activity Type Name will be "HaveANiceDay":
    public static void HaveANiceDay(UtteranceInfo utterSpec)
    {
        // ...
    }
}
```


## Define a Workflow Implementation

A _Workflow Implementation_ is a non-static class that contains:
 * **One** method that defines the implementation of the _Main Workflow Routine_.
 * **Zero or more** methods that define implementations of the _Signal Handlers_.
 * **Zero or more** methods that define implementations of the _Query Handlers_.

The _Workflow Implementation_ class and the _Main Routine_ and _Handler_ implementations MUST be decorated with special attributes.

(<small>These "workflow implementation attributes have _optional_ properties that specify the _Temporal Workflow Type Name_, the _Signal Type Name_ and the _Query Type Name_ respectively (e.g. `WorkflowTypeName="..."`). If such type-name-properties are not specified, the corresponding type-names are based on the names of the decorated methods (for signals and queries) or the name of the workflow implementation class respectively.)</small>

```cs
[WorkflowImplementation]
public class SayHelloWorkflow
{
    [WorkflowMainRoutine]
    public async Task SayManyHellosAsync(GreetingInfo initialGreetingSpec)
    {
        // ...
    }

    [WorkflowSignalHandler]
    public void UpdateGreetingSpec(GreetingInfo greetingSpec)
    {
        // ...
    }

    [WorkflowSignalHandler(SignalTypeName="Update-Greeting-Kind")]
    public void UpdateGreetingKind(GreetingKind greetingKindSpec)
    {
        // ...
    }

    [WorkflowQueryHandler]
    public CompletedGreetingsInfo GetProgressStatus()
    {
        // ...
    }

    [WorkflowQueryHandler(QueryTypeName="Read-Greeting-Kind")]
    public GreetingKind GetCurrentGreetingKind()
    {
        // ...
    }
}

// Data exchange types used in this sample:
public record GreetingKind(string Utterance);
public record GreetingInfo(int GreetingsCountMax, string PersonName);
public record CompletedGreetingsInfo(int GreetingsCount);
```

### Workflow routines: Return types & Sync/Async requirements

* The Main Workflow routine MUST be async.  
<small>
Vast majority of workflows have at least one interaction with Temporal orchestration functionality (activities, timers, child workflows, etc.), which are all async. A Main Workflow Routine must return one of:
  - `Task` (the workflow does not have a logical result value)
  - `Task<T>` (the workflow has a logical result value of type `T`)
</small>

* Signal handlers may be sync or async.  
<small>
Signal handlers never have a logical result value. Signal handlers may interact with Temporal orchestration functionality, in which case they need to be async. However, if a signal handler does not interact with any async functionality, we do not burden the developer with complying with an async signature. Thus, a signal handler method must always return one of:
  - `Task` (not `Task<T>`)
  - `void`
</small>

* Query handlers must be sync.  
<small>
Query handlers always have a logical result value. Query handlers must not interact with Temporal orchestration functionality, and must complete synchronously. Thus, query signal handlers must return:
  - Any value _except_: `Task`, `Task<T>`, or other Task-like awaitable types
</small>

* <small>**Note**: Although no restrictions other then mentioned above are placed on the return types of Queries and on the type of `T` (in `Task<T>`) in Main Routine signatures, it is _strongly_ recommended that JSON-like objects with named properties are used. The reasons are related to successful versioning strategies. (A detailed discussion of versioning is not in scope here.)</small>

### Interfaces for Workflow Implementations

Workflow implementations are _not required_ to implement any interfaces.  
However, developers may opt into specifying and publishing workflow interfaces. This enables scenarios where such interfaces are published so that workflow consumers (clients) can perform automatically-strongly-typed calls to workflows (the client-side invocations are discussed later).  
Workflow interfaces use the same [attributes](#define-a-workflow-implementation) as workflow implementations. In class-hierarchies, the attributes are inherited as follows:

* Actual (most derived) workflow implementation must be decorated explicitly.  
  - <small>A class that is not explicitly decorated with `[WorkflowImplementation]`, is not considered to be a valid workflow implementation, even if it derives from another class or implements an interface that is decorated with that attribute.</small>
  - <small>A method that is not explicitly decorated with `[WorkflowMainRoutine]`/`[WorkflowSignalHandler]`/`[WorkflowQueryHandler]` is not considered to be a valid respective routine implementation, even if it overrides a base-class method or implements an interface method that is respectively decorated.</small>
* Temporal Type Names are inherited.  
<small>If the most derived workflow implementation class or method does not explicitly specify the corresponding _Temporal Workflow Type Name_, _Signal Type Name_ or _Query Type Name_, but the respective base-class, implemented interface or overridden method does specify the _Type Name_, than such specification is "inherited" and used.</small>
* Ambiguity is always an error.  
<small>Ambiguity or contradiction may arise from inheritance (e.g. different _Type Names_ for the same item, or a routine decorated as multiple handler kinds). In all such cases a fail-fast error with detailed diagnostic info is reported. This happens either at compile time (in scenarios where source generators are used), or at worker registration time.</small>


## Data exchange types & Workflow Input/Output

For workflow input / output, we strongly encourage developers to use data exchange types with _named_ properties.  
Conversely, we strongly discourage from using multiple _positional_ (aka non-named) input arguments. The key driver for this is leading developers into a success pit around workflow and activity _versioning_. (A deeper discussion of versioning in not in scope here; we will publish details at another occasion.)

As a result of this guidance, we only support workflow routine and activity implementations with one (or zero) data inputs/outputs. Multiple logical values should be modeled using a single input (output) with named properties.  
(Invoking (not implementing) workflows and activities with multiple inputs is supported to allow for polyglot scenarios.)

(<small>Legacy and other compatibility scenarios where multiple unnamed parameters in workflow/activity implementations are strictly required are still possible. Implementation techniques include data-containers, custom payload-converters, wrappers, etc. Such detailed techniques and are not in scope in this high-level overview.</small>)

Example for using named properties to marshal multiple logical values:
```cs
public static class UtteranceActivities
{
    // Discouraged (supported indirectly / with workarounds):
    // Avoid using MULTIPLE positional input arguments.
    [ActivityImplementation]
    public static void SayGreetingAsync(bool isGoodBye, string personName)
    {
        string greeting = isGoodBye ? "Good bye": "Hello";
        Console.WriteLine($"{greeting}, {personName}!");        
    }

    // Supported directly and encouraged:
    // Use ONE input argument with NAMED PROPERTIES to encode multiple logical input values.
    [ActivityImplementation]
    public static void SayGreetingAsync(UtteranceInfo utteranceSpec)
    {
        string greeting = utteranceSpec.IsGoodBye ? "Good bye": "Hello";
        Console.WriteLine($"{greeting}, {utteranceSpec.PersonName}!");        
    }
}

public record UtteranceInfo(bool IsGoodBye, string PersonName);
```


## Workflow and Activity method Signatures

Methods that implement _Workflow Routines_ (i.e. a _Workflow Main Routine_, a _Signal Handler_, or a _Query Handler_) or _Activities_ have **up to two optional parameters**:

* The 1st (optional) parameter is the workflow/activity _input_.  
The details of input (and output) types are discussed in the above section on [data exchange types](#data-exchange-types--workflow-inputoutput).

* The 2nd (optional) parameter is the `IWorkflowContext`/`IWorkflowActivityContext`.  
It is the context accessor for the workflow's (or activity's) execution environment. The exact API surface of this `IXyzContext` in beyond the scope of this discussion. On a high level, it contains APIs used to
  - access the details of the current workflow/activity,
  - interact with the Temporal orchestration functionality,
  - access deterministic equivalents for some system APIs,
  - and similar.

Examples of activity method signatures:  
<small>(Workflow routine signatures are equivalent and should be clear from the context.)</small>

```cs
// Examples for recommended data exchange types:
// (default serialization is JSON object with named properties)
public record SomeData(int Num, string Text);
public record OtherData(int Count);
public record MoreData(double Val);

// Valid signatures:
public static void ActivitySample01(SomeData input, IWorkflowActivityContext activityCtx) {/*...*/}
public static OtherData ActivitySample02(SomeData input) {/*...*/}
public static void ActivitySample03(IWorkflowActivityContext activityCtx) {/*...*/}
public static Task<MoreData> ActivitySample04Async() {/*...*/}
// ...

// Valid but NOT RECOMMENDED (inputs/outputs are not objects with named properties):
public static void ActivitySample11(int input, IWorkflowActivityContext activityCtx) {/*...*/}
public static Task<string> ActivitySample12Async() {/*...*/}
// ...

// INVALID signatures:

// Invalid because `activityCtx` must follow data input:
public static void ActivitySample21(IWorkflowActivityContext activityCtx, SomeData input) {/*...*/}

// Invalid because only 0 or 1 input args permitted:
public static void ActivitySample22(SomeData inputA, OtherData inputB) {/*...*/}  
```


## Invoke workflows (from a client)

### String-based (aka not-strongly-typed) API
<small>(Here, we use use a workflow signature used in the [earlier example](#define-a-workflow-implementation).)</small>

```cs
// Create a client:
ITemporalClient client = new TemporalClient(TemporalClientConfiguration.ForLocalHost());

// Start a workflow:
IWorkflowHandle workflow = await client.StartWorkflowAsync(
            "Sample-Workflow-Id",                               // workflow-id
            "Say-Hello",                                        // workflow-type-name
            "Sample-Task-Queue",                                // task queue
            new GreetingInfo(5, "John"));                       // workflow input

// ...

// Send signal for 5 additional (= 10 total) greetings:
await workflow.SignalAsync("Update-Greeting", new GreetingInfo(10, "John"));

// ...

// Query for progress so far:
CompletedGreetingsInfo completedGreets
            = await workflow.QueryAsync<CompletedGreetingsInfo>("Read-Progress-Status");


// ...

// Wait for the workflow to complete:
await workflow.GetResult();

//
// All the typical workflow interactions (cancel, describe, terminate, ...) work in a similar manner.
// ...

```

### Strongly-typed API

The SDK implements support for strongly-typed workflow invocation APIs using .NET Source Generators.  
The SDK will auto-generate a stub class for a workflow based on the workflow definition.
Current .NET tooling allows for such generation to occur as a part of the normal build process. No additional build-steps or tools (beyond a current .NET SDK) are necessary. The only requirement is that the project that uses the stub must reference our Source-Generator-Nuget (part of the SDK).

Code needed to initiate stub generation:

```cs
[WorkflowStub(typeof(SayHelloWorkflow))]
internal partial class SayHelloWorkflowStub : IWorkflowStub
{        
}
```

Here, `SayHelloWorkflow` may be any class or interface decorated with `[WorkflowImplementation]`.  
<small>(WLOG, we refer to the [earlier example](#define-a-workflow-implementation)).</small>

Next, build your project in the usual manner.  
<small>(E.g., press Ctrl+B in Visual Studio or enter whatever console command is normally used for the solution.)</small>

Subsequently, simply use the stub.  
<small>(The detailed Dev experience, incl. viewing the generated code and working with potential build warnings etc. is beyond the scope of this high-level overview and is discussed elsewhere.)</small>

#### Create a workflow stub and use it to execute the workflow from start to conclusion

<small>(<span style="color:red">__*__</span>) Note: The sub-method `SayManyHellosAsync(..)` (below) was auto-generated based on the `[WorkflowMainRoutine]`-method in the workflow definition [demoed earlier](#define-a-workflow-implementation). The first argument to the stub is auto-generated based on the data input into the main routine implementation. All the other arguments (task queue, etc.) correspond to the respective `StartWorkflow`-APIs on the non-strongly-typed workflow handle. The details are not yet finalized, and are not in scope of this discussion.</small>

```cs
// Create a client (same as above):
ITemporalClient client = new TemporalClient(TemporalClientConfiguration.ForLocalHost());

// Instantiate a stub:
SayHelloWorkflowStub sayHellosStub = new(client.CreateWorkflowHandle("Sample-Workflow-Id"));

// Execute the workflow from start to conclusion:
//  (Main Workflow Routine invocation stubs are generated by auto-discovering the
//   implementation method decorated with [WorkflowMainRoutine].
//   The stub is clean from the implementation-only aspects of the method signature,
//   e.g. a potential `IWorkflowContext`-argument is not part of the stub.
//   Conversely, the stub uses additional arguments required to start the workflow from
//   the client side: see the (*) note above.
//   The returned Task completes when the entire workflow chain completes.)

Task sayHelloCompletion = sayHellosStub.SayManyHellosAsync(new GreetingInfo(5, "John"),
                                                           "Sample-Task-Queue");
// ...
await sayHelloCompletion;
```

#### Use a stub to interact with an existing workflow

```cs
// ...
SayHelloWorkflowStub sayHellosStub = new(client.CreateWorkflowHandle("Sample-Workflow-Id"));
// ...

// Send a signal:
//  (Signal invocation stubs are generated by auto-discovering all implementation
//   methods decorated with [WorkflowSignalHandler].
//   Note that while the signature of the signal handler implementation is sync,
//   the client-side stub is async.)

await sayHellosStub.UpdateGreetingSpecAsync(new GreetingInfo(10, "John"));

// Execute a query:
//  (Query execution stubs are generated by auto-discovering all implementation
//   methods decorated with [WorkflowQueryHandler].
//   Note that while the signature of the query handler implementation is sync,
//   the client-side stub is async.
//   The stub is clean from implementation-specific signature components (not
//   shown here), e.g., potential `IWorkflowContext`-arguments.)

CompletedGreetingsInfo completedGreets
            = await sayHellosStub.GetProgressStatusAsync();

// NOTE: The Source Generator extracts the correct Signal Type Name (and Query Type
// Name) from the implementation and uses them when addressing the signal/query.
// The method name of the stub is based on the method name of the implementation,
// not on the Signal/Query Type Name.
// (The Signal/Query Type Name may contain characters not permitted in method names.)
```

#### Stub method overloads 

The generated stubs include overloads that allow for optional parameters.
E.g., the next sample uses an optional Cancellation Token.

```cs
CancellationTokenSource cancelControl = new();
// ...
await sayHellosStub.UpdateGreetingSpecAsync(new GreetingInfo(10, "John"),
                                            cancelControl.Token);
```

#### Additional workflow stub features

There are some common scenarios for which stub APIs cannot be generated fully automatically because there is no 1:1 correspondence with implementations. These scenarios are achieved by specifying the required additional information via named property-parameters to the `[WorkflowStub]`-attribute:

**(a)** Start a workflow represented by a stub without waiting for the workflow conclusion:  
Use the "`string StartMethod`" property to specify the name of the method to be generated.  
<small>(The stub signature will be equivalent to the main routine stub; see note (<span style="color:red">__*__</span>) in the above section on [executing a workflow from start to conclusion](#create-a-workflow-stub-and-use-it-to-execute-the-workflow-from-start-to-conclusion).)</small>

**(b)** Get the result of a workflow that was started elsewhere, without trying to start it again:  
Use the "`string GetResultMethod`" property to specify the name of the method to be generated.  
<small>(The return type of the stub is based on the return type of the main routine implementation.)</small>

**(c)** An atomic Signal-with-Start, as supported by the respective Temporal client API:  
Use the "`string[] SignalWithStartMethods`" property to specify the names of the methods to be generated, and the respective signal implementation methods. The specified string MUST have an _even_ number of elements denoting pairs `(StubMethodName, SignalHandlerImplementationMethodName)`.  
<small>(The stub signatures will be based on the signatures of the main routine and the specified signal handler method; see note (<span style="color:red">__*__</span>) in the above section on [executing a workflow from start to conclusion](#create-a-workflow-stub-and-use-it-to-execute-the-workflow-from-start-to-conclusion).)</small>

This sample uses all three of these features:
```cs
[WorkflowStub(typeof(SayHelloWorkflow),
              StartMethod="InitiateAsync",
              GetResultMethod="ConcludeAllGreetingsAsync"),
              SignalWithStartMethods=new[]{"InitiateAndUpdateGreeting", "UpdateGreetingSpec"}]
internal partial class SayHelloWorkflowStub : IWorkflowStub
{        
}
```

Corresponding usage:
```cs
// ...
SayHelloWorkflowStub sayHellosStub = new(client.CreateWorkflowHandle("Sample-Workflow-Id"));
// ...

// Start the workflow:
//  (The returned Task completes when the server persisted the request to start the workflow.
//   The stub uses arguments required to start the workflow from the client side:
//   see the (*) note above.)

await sayHellosStub.InitiateAsync(new GreetingInfo(5, "John"), "Sample-Task-Queue");

// Create another stub and signal with start:
//  (Signal-with-Start invocation semantics may not make sense for the particular very simple
//   workflow definition used here. However, the focus is usage syntax, not the business logic
//   of the example.)

SayHelloWorkflowStub sayHellosStub2 = new(client.CreateWorkflowHandle("Sample-Workflow-Id-2"));
// ...
await sayHellosStub2.InitiateAndUpdateGreeting(
            new GreetingInfo(5, "John"),        // workflow input
            new GreetingInfo(10, "Jake"),       // signal input
            "Sample-Task-Queue-2");             // additional info to start a workflow

// ...
// Await the conclusion of a workflow started earlier:

await sayHellosStub.ConcludeAllGreetingsAsync();
```

## Invoke activities (from within a workflow)

### String-based (aka not-strictly-typed) API

Sample activity:
```cs
public record UtteranceInfo(string PersonName);

public static class Utterances
{
    public static async Task SayHelloAsync(UtteranceInfo utteranceSpec)
    {
        // ...
    }
}
```

Sample invocations:
```cs
[WorkflowMainRoutine]
public async Task SayManyHellosAsync(GreetingInfo input, IWorkflowContext workflowCtx)
{
    // ...
    await workflowCtx.Activities.ExecuteAsync(
            "SayHello",
            new UtteranceInfo(_input.PersonName));
    // ...
}

// `ExecuteAsync(..)` has overloads so that additional arguments, such as Cancellation Tokens,
// Activity Invocation Options, etc. can be specified. E.g.:

[WorkflowSignalHandler]
public async Task ProcessSomeSignalAsync(GreetingInfo input, IWorkflowContext workflowCtx)
{
    // ...
    await workflowCtx.Activities.ExecuteAsync(
            "SayHello",
            new UtteranceInfo(_input.PersonName),
            new ActivityInvocationOptions()
            {
                ActivityCancelPolicy = ActivityCancellationPolicy.WaitCancelComplete,
                ScheduleToCloseTimeout = TimeSpan.FromSeconds(45),
                TaskQueue = "Some-Queue"
            });
    // ...
}
```


### Strongly-typed API

The SDK implements support for strongly-typed activity invocation APIs using .NET Source Generators, in a way similar to workflow invocations (see the [corresponding section ](#strongly-typed-api) for additional details).

To seed stub generation, declare a partial class decorated as an ActivityStub, while referencing the activity implementation. An invocation stub with the appropriate invocation signatures will be generated.
```cs
[ActivityStub(implementingType: typeof(Utterances), implementingMethod: "SayHelloAsync") ]
internal partial class UtterHelloStub : IActivityStub
{
}
```

In some cases, the activity implementation may not be accessible at development time.  
Also, activities may be wrapped into  lambda expressions; then, the implementing method is contained within a compiler-generated type that cannot be easily referenced by a `typeof()`-expression.  
To support such scenarios, the `[ActivityStub]`-attribute may be specified using an explicit activity signature:

```cs
[ActivityStub(implementationSignature: typeof(Func<UtteranceInfo, Task>), ActivityTypeName="SayHello") ]
internal partial class EquivalentUtterHelloStub : IActivityStub
{
}
```

Usage:
```cs
[WorkflowMainRoutine]
public async Task SayManyHellosAsync(GreetingInfo input, IWorkflowContext workflowCtx)
{
    // ...
    UtterHelloStub helloStub = new(workflowCtx);
    // ...
    await helloStub.SayHelloAsync(new UtteranceInfo(_input.PersonName));
    // ...
}
```

The generated stubs include overloads for common  usage scenarios, such as cancellation tokens, activity invocation options that can be applied either to the entire stub or to a particular invocation, and other additional items:

```cs
[WorkflowMainRoutine]
public async Task SayManyHellosAsync(GreetingInfo input, IWorkflowContext workflowCtx)
{
    // ...

    CancellationTokenSource cancelControl = new();

    // ...    
    // Create an activity stub while specifying activity options that apply to all
    // invocations performed via this stub:

    UtterHelloStub helloStub = new(workflowCtx,
                                   new ActivityInvocationOptions()
                                   {
                                       ActivityCancelPolicy = ActivityCancellationPolicy.WaitCancelComplete,
                                       ScheduleToCloseTimeout = TimeSpan.FromSeconds(45),
                                       TaskQueue = "Some-Queue"
                                   });
    
    // ...
    // Invoke the activity using a particular cancellation token:

    await helloStub.SayHelloAsync(new UtteranceInfo(_input.PersonName), cancelControl.Token);

    // ...
}

public async Task ProcessSomeSignalAsync(GreetingInfo input, IWorkflowContext workflowCtx)
{
    // ...
    CancellationTokenSource cancelControl = new();
    
    // ...
    // Create an activity stub using the default options applicable in the current workflow:

    UtterHelloStub helloStub = new(workflowCtx);

    // ...
    // Invoke an activity using default options and a particular cancellation token:

    await helloStub.SayHelloAsync(new UtteranceInfo(_input.PersonName), cancelControl.Token);

    // ...
    // Invoke an activity using specific invocation options and the same particular cancellation token:

    await helloStub.SayHelloAsync(new UtteranceInfo(_input.PersonName),
                                  new ActivityInvocationOptions()
                                  {
                                      ActivityCancelPolicy = ActivityCancellationPolicy.WaitCancelComplete,
                                      ScheduleToCloseTimeout = TimeSpan.FromSeconds(45),
                                      TaskQueue = "Some-Queue"
                                  },
                                  cancelControl.Token);
    // ...
}
```


## Invoke child workflows (from a within a workflow)

Consider a workflow definition interface published using the techniques discussed [earlier](#interfaces-for-workflow-implementations).  
This workflow will be used as child workflow in the subsequent samples.

```cs
[WorkflowImplementation(WorkflowTypeName="Perform-Some-Logic")]
public interface IPerformSomeLogicWorkflow
{
    [WorkflowMainRoutine]
    Task<ResultDataset> ApplyTheLogicAsync(DataRecord input);
    
    [WorkflowSignalHandler(SignalTypeName="Some-Signal-Type")]
    void NotifyStuffHappened(CustomSignalInput stuffInfo);
    
    // ...
}
```

### String-based (aka not-strongly-typed) API

`IWorkflowContext.ChildWorkflows` offers access to a client-style API with the same flavor as `ITemporalClient`, which is used to interact with workflows from an external client application (discussed [above](#invoke-workflows-from-a-client)). However, `ChildWorkflows` is customized for the child-workflow specific scenarios. E.g.:

```cs
[WorkflowMainRoutine]
public async Task ParentWorkflowMainRoutine(IWorkflowContext workflowCtx)
{
    // ...
    // Start a child workflow with default options:

    IChildWorkflowHandle child = await workflowCtx.ChildWorkflows.StartAsync(
            "Sample-Workflow-Id",                                       // workflow-id
            "Perform-Some-Logic",                                       // workflow-type-name            
            new DataRecord(42, "forty two"));                           // workflow input

    // Start a child workflow with additional options:

    IChildWorkflowHandle child2 = await workflowCtx.ChildWorkflows.StartAsync(
            "Sample-Workflow-Id",                                       // workflow-id
            "Perform-Some-Logic",                                       // workflow-type-name            
            new DataRecord(42, "forty two"),                            // workflow input
            new StartChildWorkflowConfiguration()
            {
                WorkflowExecutionTimeout = TimeSpan.FromMinutes(1),     // override a subset of
                TaskQueue = "Some-Queue",                               // child settings with 
                ParentClosePolicy = ParentClosePolicy.RequestCancel     // specific values
            });

    // Send a signal to a child:

    await child.SignalAsync("Some-Signal-Type", new CustomSignalInput("some data"));

    // Get the result of a child workflow:

    ResultDataset resDat = await child.GetResultAsync<ResultDataset>();

    // ...
}
```

### Strongly-typed API

The strongly-typed API for child workflow interactions is similar to [client-side workflow stubs](#strongly-typed-api). Some differences exist to account for the distinctiveness of child workflows.

```cs

[ChildWorkflowStub(typeof(IPerformSomeLogicWorkflow))]
internal partial class PerformSomeLogicWorkflowStub : IWorkflowStub
{        
}

// . . .

[WorkflowMainRoutine]
public async Task ParentWorkflowMainRoutine(IWorkflowContext workflowCtx)
{
    // ...
    // Instantiate a stub:

    PerformSomeLogicWorkflowStub someLogicStub = new(workflowCtx, "Sample-Workflow-Id");

    // Start a child workflow with default options, wait for it to conclude and get the result:

    ResultDataset resDat = await someLogicStub.ApplyTheLogicAsync(new DataRecord(42, "forty two"));
    
    // Do the same using additional child workflow options:

    PerformSomeLogicWorkflowStub someLogicStub2 = new(workflowCtx, "Sample-Workflow-Id2");

    ResultDataset resDat2 = await someLogicStub2.ApplyTheLogicAsync(
                new DataRecord(42, "forty two"),
                new StartChildWorkflowConfiguration()
                {
                    WorkflowExecutionTimeout = TimeSpan.FromMinutes(1),
                    TaskQueue = "Some-Queue",
                    ParentClosePolicy = ParentClosePolicy.RequestCancel
                });
    // ...
}
```

Note that a .NET workflow does not communicate commands to the server until the earliest encountered await point. Thus, be aware of the following pitfall when you try to start a child workflow, then, after the start has been initiated, to perform some work, and finally, to get the child workflow result:

```cs
[WorkflowMainRoutine]
public async Task ParentWorkflowMainRoutine(IWorkflowContext workflowCtx)
{
    // ...
    PerformSomeLogicWorkflowStub someLogicStub = new(workflowCtx, "Sample-Workflow-Id");

    Task<ResultDataset> someLogicConclusion = someLogicStub.ApplyTheLogicAsync(new DataRecord(42, "forty two"));

    // At this point the start of `someLogicStub` is NOT yet initiated (aka communicated to the server),
    // because nothing was awaited after the call to `ApplyTheLogicAsync(..)`.
    // The next command will both, actually initiate the start of `someLogicStub` AND await its completion:

    ResultDataset resDat = await someLogicConclusion;
    // ...
}
```

The `IWorkflowContext.YieldAsync()` API can be used to yield the workflow code execution and to allow the SDK to send commands.  
(As an alternative approach, `ChildWorkflowStub` exposes `StartMethod` and `GetResultMethod` properties which are equivalent to the same-named properties on `WorkflowStub` discussed [above](#additional-workflow-stub-features).)

```cs
[WorkflowMainRoutine]
public async Task ParentWorkflowMainRoutine(IWorkflowContext workflowCtx)
{
    // ...

    PerformSomeLogicWorkflowStub someLogicStub = new(workflowCtx, "Sample-Workflow-Id");

    Task<ResultDataset> someLogicConclusion = someLogicStub.ApplyTheLogicAsync(new DataRecord(42, "forty two"));

    // The start of `someLogicStub` is NOT yet initiated (communicated to the server).

    await workflowCtx.YieldAsync();

    // Now the command to start `someLogicStub` was communicated to the Temporal server.
    // The actual execution of the child workflow occurs concurrently and depends on worker
    // availability and performance.

    // Send a signal to the child workflow:

    await someLogicStub.NotifyStuffHappenedAsync(new CustomSignalInput("some data"));

    // Await the conclusion of the child workflow and get its result:

    ResultDataset resDat = await someLogicConclusion;

    // ...
}
```

## Register a worker

.NET server applications overwhelmingly use a well-known [host abstraction](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host) to configure, execute, and control an application. We presume that .NET developers will expect that Temporal workers fully integrate with [`HostBuilder`](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host) and all related functionality. The details of such integration are not in scope here. Instead, we focus on a minimalist worker functionality without any dependency injection.

A bare-bones worker host application may look like this:

```cs
public static async Task ExecuteWorkerHostAsync()
{
    // Instantiate a worker:

    TemporalWorkerConfiguration workerConfig = TemporalWorkerConfiguration.ForLocalHost()
    using TemporalWorker worker = new(workerConfig);

    // Register some activities (static methods) that were used in the above samples:

    worker.RegisterActivity(Utterances.SayHelloAsync)
          .RegisterActivity(Utterances.SayGoodBye);

    // Register more activities (instance methods) from the above samples:

    CustomLogger logActivityProvider = new();
    worker.RegisterActivity(logActivityProvider.LogEvent);

    // Register the workflow implementations used in the above samples:

    worker.RegisterWorkflow<SayHelloWorkflow>()
          .RegisterWorkflow<PerformSomeLogicWorkflow>();

    // Start the worker:
    // (The returned task completes when the worker is is up and running.)

    await worker.StartAsync();

    // The app runs and handles Workflow and Activity Tasks until the user decides to shut it down.
    Console.WriteLine("Worker started. Press enter to terminate.");
    Console.ReadLine();

    // Initiate the shutdown of the worker.
    // (The task completes when the shutdown sequence has been initiated by the underlying Core engine.)

    await worker.RequestShutdownAsync();

    // Wait for the worker to completely shut down.
    // If any abnormal errors occurred while the worker was running, they will be embedded
    // into the Task returned by `RunToCompletion()`.

    await worker.RunToCompletion();
}
```

#### Dealing with fatal worker errors

Some critical errors during worker executions may lead to its immediate shutdown. Such errors are surfaced by awaiting the `Task` returned from the worker's `RunToCompletion()`-method:

The method `StartAsync()` in the above sample is used when the developer needs to start the worker and to know that the worker has completed the initialization and started processing messages. Conversely, the `Task` returned by the worker's `RunToCompletion()`-method completes when the worker has terminated. Either normally (started, run, finished processing all messages and completed the shutdown sequence) OR abnormally (started, encountered a fatal error anywhere during the execution, terminated).  
If `RunToCompletion()` is called before `StartAsync()`, it will initiate the worker's start-up. Conversely, if the worker is already running, `RunToCompletion()` will not affect the worker's running state, but will return the Task representing the completion of the ongoing worker execution.

The following example is similar to the previous, however, fatal errors are printed to the console:

```cs
public static async Task ExecuteWorkerHostAsync()
{
    // Instantiate a worker:

    TemporalWorkerConfiguration workerConfig = TemporalWorkerConfiguration.ForLocalHost()
    using TemporalWorker worker = new(workerConfig);

    // Register activities and workflows ...

    // Start the worker and get the task that represents its eventual completion:

    Task workerCompletion = worker.RunToCompletion();

    // Use a logical thread fork for user interactivity:

    _ = Task.Run(async () => 
                 {
                     Console.WriteLine("Worker started. Press enter to terminate.");
                     Console.ReadLine();                     
                     await worker.RequestShutdownAsync();
                 })

    // Wait for the worker to shut down. 
    // That will happen either when the user presses Enter and `RequestShutdownAsync()` is
    // invoked on the other thread, or when a critical error is encountered.
    // If errors occur, print them to the console:
    try
    {
        await workerCompletion;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Critical error during worker execution: {ex}");
    }
}
```
