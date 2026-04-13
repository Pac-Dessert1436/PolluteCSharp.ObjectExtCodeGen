# `PolluteCSharp.ObjectExtCodeGen` - Injects Static Methods to Every Type in Your Codebase

> **⚠️ WARNING**: This is a joke project. DO NOT use it in production code!
>
> Besides, this project is not planned to be released as a NuGet package. See the **[Installation](#installation)** section for details.

With great power comes great responsibility... and the ability to pollute your entire codebase. This project abuses .NET 10's new `extension(...)` syntax more than you can imagine. **Use with extreme caution**.

## 📋 Requirements

- **.NET 10 SDK** or later (for `extension(...)` syntax support)
- **C# Source Generator** enabled project
- **A sense of humor** and **willingness to experiment**

## 🎭 What is this?

This is a **Source Generator** that abuses .NET 10's new `extension(...)` syntax to inject static methods and properties into **the entire type system** (classes, structs, interfaces, etc.) in your C# codebase. It's a creative exploration of how far we can push the new extension syntax - and a reminder that just because you *can* do something doesn't mean you *should*!

Inspired by VB.NET's bare method calls and C#'s brand new `extension(...)` syntax in .NET 10 SDK, this package allows you to add properties or methods to objects - be careful with adding extra static members to the type system in your codebase!

## 🔧 How it works

1. Add the `[ObjectExtension]` attribute to any static method or property in your code
2. The source generator scans your codebase and collects all marked members
3. It generates an `extension(object)` block containing all those members
4. Suddenly, **the entire type system** (classes, structs, interfaces, etc.) in your codebase has those methods/properties available

## 💻 Usage

### Basic Example

```csharp
using PolluteCSharp.ObjectExtCodeGen;

public static class MyExtensions
{
    [ObjectExtension]
    public static void SayHello()
    {
        Console.WriteLine($"Hello, nice to meet you!");
    }

    [ObjectExtension]
    public static int SpecialValue => 42;
}
```

### Now you can call these on ANY type:

```csharp
string.SayHello(); // Works! Output: Hello, nice to meet you!
int.SayHello(); // Also works!
int special = int.SpecialValue; // Returns 42

// Even collections and plain objects work!
List<int>.SayHello(); 
object.SayHello();
```

## An even more ridiculous example:

```csharp
public static class MoreExtensions
{
    [ObjectExtension]
    public static string AsString(object obj)
    {
        return obj?.ToString() ?? "null";
    }

    [ObjectExtension]
    public static int MagicNumber => 7;
}

Console.WriteLine(object.AsString(42)); // "42"
Console.WriteLine(float.MagicNumber); // 7
```

## 🚫 Why you shouldn't use this

| Reason | Description |
|--------|-------------|
| **Global Pollution** | Extensions apply to the type system, including types you don't own |
| **Confusion** | Other developers will be confused where these methods come from |
| **Maintenance Nightmare** | Hard to track down where extensions are defined |
| **Performance** | Potential runtime overhead from method resolution |
| **Breaking Changes** | Can conflict with future framework members |
| **Code Reviews** | Software engineers will hate you |
| **Career Risk** | Using this might get you fired (just kidding... or am I?) |

## 🛠️ Installation
This source generator is not planned to be released as a NuGet package. Follow these steps to install it.

1. Clone the repository and navigate to the project directory:
```bash
git clone https://github.com/Pac-Dessert1436/PolluteCSharp.ObjectExtCodeGen.git
cd PolluteCSharp.ObjectExtCodeGen
```

2. Build the source generator project:
```bash
dotnet build
```

3. Reference the source generator project in your target C# project:
```xml
<ItemGroup>
    <ProjectReference Include="..\PolluteCSharp.ObjectExtCodeGen\PolluteCSharp.ObjectExtCodeGen.csproj"
        OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## 🎓 Educational Value

While this package is a joke, it demonstrates several important concepts:

- **Source Generators**: How to use Roslyn's incremental source generation API
- **Syntax Analysis**: How to parse and analyze C# code at compile time
- **Extension Methods**: Understanding the new `extension(...)` syntax in .NET 10
- **Code Generation**: How to generate code dynamically based on analysis

## 🤝 Contributing

Feel free to submit issues and pull requests! But remember: this is a joke project, so keep it fun and educational.

## 📄 License

This is a joke/educational project. Use at your own risk! [MIT License](LICENSE).

---

**Remember**: Just because you can pollute your codebase doesn't mean you should! 😄