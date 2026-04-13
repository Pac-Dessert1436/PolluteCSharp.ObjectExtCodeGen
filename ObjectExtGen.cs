using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using IGIC = Microsoft.CodeAnalysis.IncrementalGeneratorInitializationContext;

namespace PolluteCSharp.ObjectExtCodeGen;

/// <summary>
/// A source generator that abuses .NET 10's extension(...) syntax to inject static members into 
/// the type system (classes, structs, interfaces, etc.).
/// </summary>
/// <remarks>
/// This generator collects all methods and properties marked with [ObjectExtension] attribute
/// and generates an <c>extension(object)</c> block containing them. This makes those members available
/// on EVERY type in the codebase, effectively polluting the entire type system.
/// 
/// WARNING: This is a joke/educational project. Do not use in production code!
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ObjectExtGen : IIncrementalGenerator
{
    private const string AttributeCode = @"/// <summary>
/// Marks a static method or property to be injected into the global object extension.
/// When applied, the static member becomes available on ALL types in the codebase.
/// </summary>
/// <remarks>
/// This attribute is used by the source generator to collect members that should be
/// included in the generated <c>extension(object)</c> block. Use with extreme caution!
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
public sealed class ObjectExtensionAttribute : Attribute
{
}";

    private class MethodWithUsings
    {
        public MethodDeclarationSyntax Method { get; }
        public HashSet<string> Usings { get; }
        public HashSet<string> TypeNamespaces { get; }

        public MethodWithUsings(MethodDeclarationSyntax method, HashSet<string> usings, HashSet<string> typeNamespaces)
        {
            Method = method;
            Usings = usings;
            TypeNamespaces = typeNamespaces;
        }
    }

    private class PropertyWithUsings
    {
        public PropertyDeclarationSyntax Property { get; }
        public HashSet<string> Usings { get; }
        public HashSet<string> TypeNamespaces { get; }

        public PropertyWithUsings(PropertyDeclarationSyntax property, HashSet<string> usings, HashSet<string> typeNamespaces)
        {
            Property = property;
            Usings = usings;
            TypeNamespaces = typeNamespaces;
        }
    }

    /// <summary>
    /// Initializes the incremental generator pipeline.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    /// <remarks>
    /// This method is called by the Roslyn compiler to set up the generator.
    /// It registers syntax providers to find members marked with [ObjectExtension]
    /// and generates the extension(object) block containing those members.
    /// </remarks>
    public void Initialize(IGIC context)
    {
        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsMethodWithObjectExtension(node),
                transform: static (ctx, _) => GetMethodDeclarationWithUsings(ctx))
            .Where(static m => m is not null);

        var propertyDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsPropertyWithObjectExtension(node),
                transform: static (ctx, _) => GetPropertyDeclarationWithUsings(ctx))
            .Where(static p => p is not null);

        var combined = methodDeclarations.Collect().Combine(propertyDeclarations.Collect());

        context.RegisterSourceOutput(
            combined,
            static (spc, source) => Execute(source.Left, source.Right, spc)
        );
    }

    /// <summary>
    /// Determines if a syntax node is a static method with the [ObjectExtension] attribute.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <returns>True if the node is a static method with attributes; otherwise, false.</returns>
    private static bool IsMethodWithObjectExtension(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax methodDeclaration &&
               methodDeclaration.AttributeLists.Count > 0 &&
               methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
    }

    /// <summary>
    /// Determines if a syntax node is a static property with the [ObjectExtension] attribute.
    /// </summary>
    /// <param name="node">The syntax node to check.</param>
    /// <returns>True if the node is a static property with attributes; otherwise, false.</returns>
    private static bool IsPropertyWithObjectExtension(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax propertyDeclaration &&
               propertyDeclaration.AttributeLists.Count > 0 &&
               propertyDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
    }

    /// <summary>
    /// Extracts the method declaration if it has the [ObjectExtension] attribute, along with all using directives and type namespaces.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <returns>A tuple containing the method declaration, using directives, and type namespaces if marked with [ObjectExtension]; otherwise, null.</returns>
    private static MethodWithUsings? GetMethodDeclarationWithUsings(GeneratorSyntaxContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName == "ObjectExtension" || attributeName == "ObjectExtensionAttribute")
                {
                    var usings = CollectUsings(context);
                    var typeNamespaces = CollectTypeNamespaces(context, methodDeclaration);
                    return new MethodWithUsings(methodDeclaration, usings, typeNamespaces);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the property declaration if it has the [ObjectExtension] attribute, along with all using directives and type namespaces.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <returns>A tuple containing the property declaration, using directives, and type namespaces if marked with [ObjectExtension]; otherwise, null.</returns>
    private static PropertyWithUsings? GetPropertyDeclarationWithUsings(GeneratorSyntaxContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        foreach (var attributeList in propertyDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName == "ObjectExtension" || attributeName == "ObjectExtensionAttribute")
                {
                    var usings = CollectUsings(context);
                    var typeNamespaces = CollectTypeNamespaces(context, propertyDeclaration);
                    return new PropertyWithUsings(propertyDeclaration, usings, typeNamespaces);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Collects all using directives from the compilation unit containing the syntax node.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <returns>A set of all using directive names found in the file.</returns>
    private static HashSet<string> CollectUsings(GeneratorSyntaxContext context)
    {
        var usings = new HashSet<string>();
        var root = context.Node.SyntaxTree.GetRoot();

        if (root is CompilationUnitSyntax compilationUnit)
        {
            foreach (var usingDirective in compilationUnit.Usings)
            {
                var usingName = usingDirective.Name?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(usingName))
                {
                    usings.Add(usingName);
                }
            }
        }

        return usings;
    }

    /// <summary>
    /// Collects all namespaces of types used in the method or property.
    /// </summary>
    /// <param name="context">The generator syntax context.</param>
    /// <param name="node">The syntax node (method or property) to analyze.</param>
    /// <returns>A set of all namespace names for types used in the node.</returns>
    private static HashSet<string> CollectTypeNamespaces(GeneratorSyntaxContext context, SyntaxNode node)
    {
        var namespaces = new HashSet<string>();
        var semanticModel = context.SemanticModel;

        var typeNodes = node.DescendantNodes()
            .Where(n => n is TypeSyntax);

        foreach (var typeNode in typeNodes)
        {
            var typeSymbol = semanticModel.GetSymbolInfo(typeNode).Symbol as INamedTypeSymbol;
            if (typeSymbol is not null)
            {
                var namespaceName = typeSymbol.ContainingNamespace?.ToString();
                if (!string.IsNullOrEmpty(namespaceName) && namespaceName != "<global namespace>")
                {
                    namespaces.Add(namespaceName ?? string.Empty);
                }
            }
        }

        return namespaces;
    }

    /// <summary>
    /// Generates the source code for the ObjectExtensions class containing all marked members.
    /// </summary>
    /// <param name="methods">Collection of method declarations with their using directives to include.</param>
    /// <param name="properties">Collection of property declarations with their using directives to include.</param>
    /// <param name="context">The source production context for adding generated files.</param>
    /// <remarks>
    /// This method builds the extension(object) block by copying all methods and properties
    /// marked with [ObjectExtension], removing the static modifier and preserving their
    /// implementation. It also includes all necessary using directives from the source files
    /// and namespaces of all referenced types.
    /// </remarks>
    private static void Execute(
        ImmutableArray<MethodWithUsings?> methods,
        ImmutableArray<PropertyWithUsings?> properties,
        SourceProductionContext context)
    {
        var code = new System.Text.StringBuilder();

        var allUsings = new HashSet<string>();
        var allNamespaces = new HashSet<string>();

        foreach (var method in methods)
        {
            if (method is not null)
            {
                foreach (var usingDirective in method.Usings)
                {
                    allUsings.Add(usingDirective);
                }
                foreach (var namespaceName in method.TypeNamespaces)
                {
                    allNamespaces.Add(namespaceName);
                }
            }
        }
        foreach (var property in properties)
        {
            if (property is not null)
            {
                foreach (var usingDirective in property.Usings)
                {
                    allUsings.Add(usingDirective);
                }
                foreach (var namespaceName in property.TypeNamespaces)
                {
                    allNamespaces.Add(namespaceName);
                }
            }
        }

        foreach (var usingDirective in allUsings.OrderBy(u => u))
        {
            code.AppendLine($"using {usingDirective};");
        }

        foreach (var namespaceName in allNamespaces.OrderBy(n => n))
        {
            code.AppendLine($"using {namespaceName};");
        }

        code.AppendLine();
        code.AppendLine(AttributeCode);
        code.AppendLine();
        code.AppendLine("public static class ObjectExtensions");
        code.AppendLine("{");
        code.AppendLine("    extension(object)");
        code.AppendLine("    {");

        foreach (var method in methods)
        {
            if (method is null) continue;

            var modifiers = method.Method.Modifiers
                .Where(m => !m.IsKind(SyntaxKind.StaticKeyword))
                .Select(m => m.ToString());

            string methodBody = 
                method.Method.Body?.ToString() ?? 
                new string(' ', 4) + method.Method.ExpressionBody?.ToString() + ';' ?? 
                throw new NotImplementedException();
            code.AppendLine($"        {string.Join(" ", modifiers)} static {method.Method.ReturnType} {method.Method.Identifier}{method.Method.ParameterList}");
            code.AppendLine($"        {methodBody}");
            code.AppendLine();
        }

        foreach (var property in properties)
        {
            if (property is null) continue;

            var modifiers = property.Property.Modifiers
                .Where(m => !m.IsKind(SyntaxKind.StaticKeyword))
                .Select(m => m.ToString());

            code.AppendLine($"        {string.Join(" ", modifiers)} static {property.Property.Type} {property.Property.Identifier}");
            code.AppendLine("        {");
            if (property.Property.AccessorList is not null)
            {
                foreach (var accessor in property.Property.AccessorList.Accessors)
                {
                    code.AppendLine($"            {accessor}");
                }
            }
            else if (property.Property.ExpressionBody is not null)
            {
                code.AppendLine($"            get => {property.Property.ExpressionBody.Expression};");
            }
            string defaultValueExpr = 
                property.Property.Initializer is not null
                ? $" = {property.Property.Initializer.Value};" : string.Empty;
            code.AppendLine($"        }}{defaultValueExpr}");
        }

        code.AppendLine("    }");
        code.AppendLine("}");

        context.AddSource("ObjectExtensions.g.cs", code.ToString());
    }
}