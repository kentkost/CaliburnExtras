using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaliburnExtras
{
    [Generator]
    public class CaliburnNotifyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            (var compilation, var attributeSymbol) = GenerateAttribute(context);

            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            List<IFieldSymbol> fieldSymbols = GetFieldSymbols(compilation, attributeSymbol, receiver);

            foreach (var group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol);

#if WRITESOURCE
                File.WriteAllText($"GeneratedCode/{group.Key.Name}_propertyCaliburnNotify.txt", classSource);
#endif
                context.AddSource($"{group.Key.Name}_propertyCaliburnNotify.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private List<IFieldSymbol> GetFieldSymbols(Compilation compilation, INamedTypeSymbol attributeSymbol, SyntaxReceiver receiver)
        {
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (FieldDeclarationSyntax field in receiver.CandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    var att = fieldSymbol.GetAttributes()[0];
                    var attClass = att.AttributeClass;
                    var attComp = attributeSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }
            return fieldSymbols;
        }

        private (Compilation compilation, INamedTypeSymbol attributeSymbol) GenerateAttribute(GeneratorExecutionContext context)
        {
            string code = @"
using System;

namespace CaliburnExtras
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class CaliburnNotifyAttribute : Attribute
    {
        public CaliburnNotifyAttribute() { }
        public CaliburnNotifyAttribute(params string[] notifyingFields ) { }
    }
}
";

#if WRITESOURCE
            File.WriteAllText("GeneratedCode/PropertyNotifyAttribute.txt", code);
#endif
            context.AddSource("CaliburnNotifyAttribute.g", SourceText.From(code, Encoding.UTF8));

            //From the example code:
            //"we should allow source generators to provide source during initialize, so that this step isn't required."
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(code, Encoding.UTF8), options));

            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("CaliburnExtras.CaliburnNotifyAttribute");

            return (compilation, attributeSymbol);
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            StringBuilder source = new StringBuilder($@"
using Caliburn.Micro;
namespace {namespaceName}
{{
    public partial class {classSymbol.Name}
    {{
        ");

            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
            }

            source.Append("} }");
            return source.ToString();
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            var fieldsToNotify = ProcessAttributeArguments(attributeData);

            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }
            source.Append($@"
    public {fieldType} {propertyName} 
    {{
        get 
        {{
            return this.{fieldName};
        }}

        set
        {{
            // Don't want to emit notify of change when value hasn't changed.
            if(this.{fieldName} == value)
            {{
                return;
            }}

            this.{fieldName} = value;
            NotifyOfPropertyChange(() => {propertyName});{fieldsToNotify}
        }}
    }}
");

            static string chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }

        }

        private string ProcessAttributeArguments(AttributeData attributeData)
        {
            var attributeArguments = attributeData.ConstructorArguments.ToArray();

            if (attributeArguments.Length == 0)
            {
                return string.Empty;
            }

            var fieldsToNotify = attributeArguments.Last().Values;
            string res = "";
            foreach (var field in fieldsToNotify)
            {
                if(field.Value is string) 
                {
                    res += "\n";
                    res +=
$@"            NotifyOfPropertyChange(() => {field.Value});";
                }
            }

            return res;
        }
    }
}
