using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGenerator
{
    [Generator]
    public class RelayCommandGenerator : ISourceGenerator
    {
        private const string attributeNamespace = "SourceGenerator";
        private const string attributeClass = "RelayCommandAttribute";
        private const string attributeText = @"
using System;
namespace SourceGenerator
{
    public class RelayCommandAttribute : Attribute
    {
        public RelayCommandAttribute()
        {
        }
    }
}
";

        private const string commandNamespace = "SourceGenerator";
        private const string commandClass = "RelayCommand";
        private const string commandText = @"
using System;
using System.Windows.Input;
namespace SourceGenerator
{
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter = null)
        {
            return true;
        }

        public void Execute(object parameter = null)
        {
            _execute?.Invoke();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly Action<T> _execute;

        public RelayCommand(Action<T> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter = null)
        {
            return true;
        }

        public void Execute(object parameter = null)
        {
            T param = default;
            if (parameter is T)
            {
                param = (T)parameter;
            }
            
            _execute?.Invoke(param);
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization((i) =>
            {
                i.AddSource($"{attributeClass}.g.cs", attributeText);
                i.AddSource($"{commandClass}.g.cs", commandText);
            });

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is SyntaxReceiver receiver)
            {
                foreach (IGrouping<INamedTypeSymbol, IMethodSymbol> group in receiver.Methods.GroupBy<IMethodSymbol, INamedTypeSymbol>(m => m.ContainingType, SymbolEqualityComparer.Default))
                {
                    string classSource = ProcessClass(group.Key, group.ToList());
                    context.AddSource($"{group.Key.Name}_relayCommand.g.cs", classSource);
                }
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IMethodSymbol> methods)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            StringBuilder source = new StringBuilder();
            source.AppendLine($@"using System.Windows.Input;");
            source.AppendLine($@"namespace {namespaceName}");
            source.AppendLine($@"{{");
            source.AppendLine($@"   public partial class {classSymbol.Name}");
            source.AppendLine($@"   {{");

            foreach (IMethodSymbol methodSymbol in methods)
            {
                ProcessMethod(source, methodSymbol);
                source.AppendLine();
            }

            source.AppendLine($@"   }}");
            source.AppendLine($@"}}");

            return source.ToString();
        }

        private void ProcessMethod(StringBuilder source, IMethodSymbol methodSymbol)
        {
            string fieldName = $"{methodSymbol.Name.Substring(0, 1).ToLower() + methodSymbol.Name.Substring(1)}Command";
            string propertyName = $"{methodSymbol.Name.Substring(0, 1).ToUpper() + methodSymbol.Name.Substring(1)}Command";

            source.AppendLine($@"       private ICommand {fieldName};");
            if (methodSymbol.Parameters.Any())
            {
                source.AppendLine($@"       public ICommand {propertyName} => {fieldName} ??= new {commandNamespace}.{commandClass}<{methodSymbol.Parameters[0].Type}>(new System.Action<{methodSymbol.Parameters[0].Type}>({methodSymbol.Name}));");
            }
            else
            {
                source.AppendLine($@"       public ICommand {propertyName} => {fieldName} ??= new {commandNamespace}.{commandClass}(new System.Action({methodSymbol.Name}));");
            }
        }

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<IMethodSymbol> Methods { get; } = new List<IMethodSymbol>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is MethodDeclarationSyntax methodDeclarationSyntax && methodDeclarationSyntax.AttributeLists.Count > 0)
                {
                    IMethodSymbol methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax) as IMethodSymbol;
                    if (methodSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == $"{attributeNamespace}.{attributeClass}"))
                    {
                        Methods.Add(methodSymbol);
                    }
                }
            }
        }
    }
}
