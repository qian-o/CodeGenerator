using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGenerator
{
    [Generator]
    public class AutoNotifyGenerator : ISourceGenerator
    {
        private const string attributeNamespace = "SourceGenerator";
        private const string attributeClass = "AutoNotifyAttribute";
        private const string attributeText = @"
using System;
namespace SourceGenerator
{
    public class AutoNotifyAttribute : Attribute
    {
        public AutoNotifyAttribute()
        {
        }

        public AutoNotifyAttribute(string propertyName)
        {
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) => i.AddSource($"{attributeClass}.g.cs", attributeText));

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
            {
                INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName($"{attributeNamespace}.{attributeClass}");

                foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in receiver.Fields.GroupBy<IFieldSymbol, INamedTypeSymbol>(f => f.ContainingType, SymbolEqualityComparer.Default))
                {
                    string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol);
                    context.AddSource($"{group.Key.Name}_autoNotify.g.cs", classSource);
                }
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            StringBuilder source = new StringBuilder();
            source.AppendLine($@"namespace {namespaceName}");
            source.AppendLine($@"{{");
            source.AppendLine($@"   public partial class {classSymbol.Name}");
            source.AppendLine($@"   {{");

            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
                source.AppendLine();
            }

            source.AppendLine($@"   }}");
            source.AppendLine($@"}}");

            return source.ToString();
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant typedConstant = attributeData.ConstructorArguments.SingleOrDefault(kvp => kvp.Kind == TypedConstantKind.Primitive);

            string propertyName = string.Empty;
            if (!typedConstant.IsNull)
            {
                propertyName = typedConstant.Value.ToString();
            }
            else
            {
                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length != 0)
                {
                    if (fieldName.Length == 1)
                    {
                        propertyName = fieldName.ToUpper();
                    }
                    else
                    {
                        propertyName = fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
                    }
                }
            }

            if (propertyName.Length > 0 && propertyName != fieldName)
            {
                source.AppendLine($@"       public {fieldType} {propertyName}");
                source.AppendLine($@"       {{");
                source.AppendLine($@"           get");
                source.AppendLine($@"           {{");
                source.AppendLine($@"               return {fieldName};");
                source.AppendLine($@"           }}");
                source.AppendLine($@"           set");
                source.AppendLine($@"           {{");
                source.AppendLine($@"               {fieldName} = value;");
                source.AppendLine($@"               PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({propertyName})));");
                source.AppendLine($@"           }}");
                source.AppendLine($@"       }}");
            }
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == $"{attributeNamespace}.{attributeClass}"))
                        {
                            Fields.Add(fieldSymbol);
                        }
                    }
                }
            }
        }
    }
}
