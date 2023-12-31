﻿# Introduction
GodotSharpKit offers 4 powerful generators to enhance your Godot game development:

1. OnReady Generator: Simplifies node initialization with automatically generated OnReady functions.
2. Resource Generator: Automates resource file management, generating code to access resources in specific directories. Customize class names and specify resource types for efficient resource handling.
3. Signal Generator: Streamlines signal emission by automatically generating EmitSignal functions based on delegate definitions. This ensures correct parameter types and error-free signal handling in your Godot and C# projects.
4. Proxy Generator: Streamlines the creation of Godot C# class proxies from interfaces. This tool simplifies the process of defining interfaces for Godot objects by automatically generating corresponding C# classes.

Below are an overview of the features, and you can also find examples in the `MainProject` folder on GitHub.

# OnReady Generator

## OnReady
This attribute is used to mark a class that should be processed by the OnReady generator.

Given 

```csharp
[OnReady]
public partial class LaunchScreen : Node2D 
{
    /* class body */
}
```

Will generate

```csharp
public partial class LaunchScreen 
{ 
    private void OnReady()
    {
    } 
}
```


You can write
```csharp
[OnReady]
public partial class LaunchScreen : Node2D
{ 
    public override void _Ready()
    {
        base._Ready();
        this.OnReady();
    }
}

```


## OnReadyGet

This attribute is used to mark a field that will be initialized with a reference to a Node or a derived type during the OnReady() method.

### Use Case 1: Default Unique Path

When [OnReadyGet] is applied without a custom path, the attribute will use the Pascal Case of the field as the unique name path.

Given
```csharp
[OnReadyGet]
private Node _node1 = null!;

[OnReadyGet]
public Node Node2 = null!;
```

Will generate

```csharp
_node1 = GetNode<Godot.Node>("%Node1");
Node2 = GetNode<Godot.Node>("%Node2");
```

### Use Case 2: Custom Path Provided

When [OnReadyGet] is applied with a custom path, it will use the specified path to retrieve the node reference.

Given
```csharp
[OnReadyGet("haha")] 
private Node _node3 = null!;
```

Will generate

```csharp
_node3 = GetNode<Godot.Node>("haha");
```


## OnReadyConnect:

This attribute is used to mark a method that will be connected to a signal during the OnReady() method.
The generated code will connect the method to the specified signal and handle its invocation when the signal is emitted.

- It provides two parameters: the first parameter is the node that emits the signal, and the second parameter is the name of the signal.
- If the first parameter is an empty string (""), it signifies that the signal is emitted by the current instance (this) of the class.

Given 
```csharp
[OnReadyConnect("", nameof(MySignal))]
private void OnMySignal() { }  

[OnReadyConnect(nameof(_timer), nameof(Timer.Timeout))]
private void OnTimeout() { }
```

Will generate
```csharp
MySignal += OnMySignal;
_timer.Timeout += OnTimeout;
```

## OnReadyRun:

This attribute is used to mark a method that will be called in a specific order during the OnReady() method.
The generated code will invoke these methods in ascending order based on the specified priority (default is 0). 
It's can also help to handle disposables.

### Basic Usage

Given
```csharp
[OnReadyRun(2)] private void Run2() { /* method body */ } 
[OnReadyRun(1)] private void Run1() { /* method body */ } 
```

Will generate

```csharp
Run1();
Run2();
```

### Handling Disposable
The methods that return `IDisposable` or `List<IDisposable>` will result in additional `OnDispose` methods.

Given
```csharp
[OnReadyRun(2)]
private void Run2() {/* method body */ }

[OnReadyRun(1)]
private IDisposable Run1() { /* method body */ }

[OnReadyRun(3)]
private List<IDisposable> Run3() { /* method body */ }
```

Will generate
```csharp
private void OnReady()
{
    _disposables.Add(Run1());
    Run2();
    _disposables.AddRange(Run3());
} 

private List<IDisposable> _disposables = new List<IDisposable>();

private void OnDispose()
{
    foreach (var disposable in _disposables)
    {
        disposable.Dispose();
    }
    _disposables.Clear();
}

```

Note: You need to explicitly call OnDispose in the Dispose method.
```csharp
protected override void Dispose(bool disposing)
{
    base.Dispose(disposing);
    OnDispose();
}
```


## Full example
```csharp
using System;
using System.Collections.Generic;
using Godot;
using Godot4Demo.OnReadyDemo.Inner;
using GodotSharpKit.Misc;

namespace Godot4Demo.OnReadyDemo;

[OnReady]
public partial class OnReadyDemoScreen : Node2D
{
    [OnReadyGet]
    private CustomNode _node1 = null!;

    [OnReadyGet]
    public Node Node2 = null!;

    [OnReadyGet("haha")]
    private Node _node3 = null!;

    [OnReadyGet]
    private Timer _timer = null!;

    public override void _Ready()
    {
        base._Ready();
        OnReady();
    }

    [OnReadyConnect(nameof(_timer), nameof(Timer.Timeout))]
    private void OnTimeout()
    {
        GD.Print("Timeout!!!");
    }

    [OnReadyRun(2)]
    private void Run2()
    {
        GD.Print("Two!");
    }

    [OnReadyRun(1)]
    private IDisposable Run1()
    {
        GD.Print("One!");
        return new MyDisposable();
    }

    [OnReadyRun(3)]
    private List<IDisposable> Run3()
    {
        GD.Print("Three!");
        return new List<IDisposable>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        OnDispose();
    }

    class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            GD.Print("MyDisposable");
        }
    }
}

```

Will generate

```csharp
namespace Godot4Demo.OnReadyDemo;
using System;
using System.Collections.Generic;

public partial class OnReadyDemoScreen 
{ 
    private void OnReady()
    {
        _node1 = GetNode<Godot4Demo.OnReadyDemo.Inner.CustomNode>("%Node1");
        Node2 = GetNode<Godot.Node>("%Node2");
        _node3 = GetNode<Godot.Node>("haha");
        _timer = GetNode<Godot.Timer>("%Timer");
        _timer.Timeout += OnTimeout;
        _disposables.Add(Run1());
        Run2();
        _disposables.AddRange(Run3());
    } 
    
    private List<IDisposable> _disposables = new List<IDisposable>();

    private void OnDispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }
}

```

# Resource Generator

The ResourceGenerator is a tool designed to automate the generation of code for accessing resources defined in a specific directory within the project.

## First try 

To utilize the ResourceGenerator, you need to define the AdditionalFiles item group in your .csproj file.
```xml
<ItemGroup>
    <AdditionalFiles Include="*.tscn" />
</ItemGroup>
```
This configuration specifies that all .tscn files in the defined directory will be considered as additional files for resource generation.

After defining the AdditionalFiles in the .csproj file, the ResourceGenerator will automatically generate code to include the paths of the .tscn files under the AutoRes class.

```csharp
using GodotSharpKit.Misc;
using Godot;

namespace Godot4Demo;

public static class AutoRes
{
    public static readonly Res<Resource> LaunchScreen = new("res://LaunchScreen.tscn");
}
```

usage
```csharp
string launchScreenPath = AutoRes.LaunchScreen.Path;
Resource launchScreenRes = AutoRes.LaunchScreen.Load();
```


## Change the default class name
To customize the class name generated by the ResourceGenerator, you can specify a container name in the AdditionalFiles item group of your .csproj file.

```xml
<ItemGroup>
    <AdditionalFiles Include="*.tscn" Container="A" />
</ItemGroup>
```

By adding Container="A" to the configuration, you indicate that the generated code should be placed within a class named A.

```csharp
public static class A
{
    public static readonly Res<Resource> LaunchScreen = new("res://LaunchScreen.tscn");
}
```

ResourceGenerator supports multiple containers or merging different folders into the same container.

Given
```xml
<ItemGroup>
    <AdditionalFiles Include="A1\*.tscn" Container="A" />
    <AdditionalFiles Include="A2\*.tscn" Container="A" />
    <AdditionalFiles Include="B\*.tscn" Container="B" />
</ItemGroup>
```

Will generate

```csharp
public static class A
{
    /* Contains all resources from the A1 and A2 folders */
}

public static class B
{
    /* Contains all resources from the B folder */
}
```

## Specifying Type in AdditionalFiles
The ResourceGenerator allows you to specify the resource type by adding a Type attribute in the AdditionalFiles configuration of your .csproj file.

Given

```xml
<ItemGroup>
    <AdditionalFiles Include="Fonts\*.tres" Container="Fonts" Type="Font" />
</ItemGroup>
```

Will generate
```csharp
public static class Fonts
{
    public static readonly Res<Font> MySystemFont = new("res://Fonts/MySystemFont.tres");
}
```

Usage
```csharp
Font font = Fonts.MySystemFont.Load();
```

In this case, the Load() method automatically converts the loaded resource to the specified type Font, so you don't need to manually perform type casting.

## Special case: PackedScene
With PackedScene resources, the generated code provides a SceneFactory property that allows for easy instantiation of the scene.

Given

```xml
<ItemGroup>
    <AdditionalFiles Include="Inner\*.tscn" Container="A" Type="PackedScene" />
</ItemGroup>
```

Will generate 

```csharp
public static class A
{
    public static readonly SceneRes<Inner.CustomNode> CustomNode = new("res://Inner/CustomNode.tscn");
}
```

Usage
```csharp
SceneFactory factory = A.CustomNode.Factory;
CustomNode customNode = factory.Instantiate();
```
In this code snippet, the Factory property of A.CustomNode provides a SceneFactory object. You can use the Instantiate() method of the factory to create an instance of the CustomNode scene without type casting.

## Special case: PackedScene (details)
There are some conditions that need to be met to ensure that SceneRes generates the correct type.

### 1. Matching TSCN and CS Files in the Same Directory:

The corresponding .tscn and .cs files should be located in the same directory and share the same name.
For example, if you have a DeepNode.tscn file, the corresponding DeepNode.cs file should be present in the same directory.

### 2. Namespace Convention in the CS File:

The CS file (DeepNode.cs in the example) should adhere to the C# namespace convention.
If the CS file's path is Deep\Deep2\CustomNode.cs, and the root namespace is Godot4Demo, the namespace for CustomNode should be Godot4Demo.Deep.Deep2.

# Signal Generator
The Signal Generator in GodotSharpKit automatically creates two essential components:

1. EmitSignal Function: It generates functions like EmitMySignalParam that emit signals with correct parameters. Ensures type-safe and accurate signal emission.

2. SignalAwaiter Function: Functions like ToSignalMySignal are generated for easy asynchronous signal handling.

Given
```csharp
using Godot;

namespace Godot4Demo;

public partial class LaunchScreen : Node2D
{
    [Signal]
    public delegate void MySignalParamEventHandler(int a, Node b);

}
```

Will generate
```csharp
namespace Godot4Demo;
public partial class LaunchScreen 
{ 
    public void EmitMySignalParam(System.Int32 a,Godot.Node b)
    {
        EmitSignal(SignalName.MySignalParam,a,b);
    } 

    public Godot.SignalAwaiter ToSignalMySignalParam(Godot.GodotObject user)
    {
        return user.ToSignal(this, SignalName.MySignalParam);
    } 
}
```

# Proxy Generator
The Godot Proxy Generator is a utility designed to streamline the creation of Godot C# class proxies from interfaces. This tool simplifies the process of defining interfaces for Godot objects by automatically generating corresponding C# classes.

## Simple example

Given
```csharp
[GodotProxy]
public interface IMyGodotObject
{
    int MyProperty { get; set; }
}
```

Will generate
```csharp
public partial class MyGodotObject : IMyGodotObject {
{
    public MyGodotObject(Godot.GodotObject obj)
    {
        GodotObject = obj;
    }

    public Godot.GodotObject GodotObject;

    public System.Int32 MyProperty
    {
        get => (System.Int32) GodotObject.Get("my_property");
        set => GodotObject.Set("my_property", value);
    }
}
```

Interfaces marked with the `GodotProxy` attribute are identified as blueprints for generating proxy classes. The name of the generated class is derived by removing the initial 'I' from the interface name.

By default, the generator automatically converts C# property names from Pascal case to snake case during class generation.To override this automatic naming behavior, use the attribute `GodotProxy(false)` within the interface declaration. 

Usage
```csharp
var proxy = new MyGodotObject(obj); // obj is gdscript object
proxy.MyProperty = 3310;
```

## Customize property name

The GodotProxyName attribute enables users to explicitly define the corresponding Godot property names for C# properties in the generated proxy class.

Given
```csharp
[GodotProxy]
public interface IMyGodotObject
{
    [GodotProxyName("HelloWorld")]
    int MyProperty { get; set; }
}
```

Will generate
```csharp
public System.Int32 MyProperty
{
    get => (System.Int32) GodotObject.Get("HelloWorld");
    set => GodotObject.Set("HelloWorld", value);
}
```

## Generate for method and signal
You can also generate method and signal.

### Method

Given
```csharp
[GodotProxy]
public interface IMyGodotObject
{
    [GodotProxyName("MyMethodLa")]
    string MyMethod(int a, Node b);
}
```

Will generate
```csharp
public System.String MyMethod(System.Int32 a,Godot.Node b)
{
    return (System.String) GodotObject.Call("MyMethodLa", a, b);
}

```

### Signal
Given
```csharp
[GodotProxy]
public interface IMyGodotObject
{
    [GodotProxyName("my_signal_la")]
    delegate void MySignalEventHandler(int a, Node b);
}
```

Will generate

```csharp
public event System.Action<System.Int32, Godot.Node> MySignal
{
    add => GodotObject.Connect(SignalName.MySignal, Godot.Callable.From(value));
    remove => GodotObject.Disconnect(SignalName.MySignal, Godot.Callable.From(value));
}
public Godot.SignalAwaiter ToSignalMySignal(Godot.GodotObject user)
{
    return user.ToSignal(GodotObject, SignalName.MySignal);
}
public void EmitSignalMySignal(System.Int32 a, Godot.Node b)
{
    GodotObject.EmitSignal(SignalName.MySignal, a, b);
}

public class SignalName
{
    public static readonly Godot.StringName MySignal = (Godot.StringName) "my_signal_la";
}
```
