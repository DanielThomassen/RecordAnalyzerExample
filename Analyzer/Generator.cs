using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Foo;

[Generator]
public class Generator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        //if (!Debugger.IsAttached)
        //{
        //    Debugger.Launch();
        //}
        context.RegisterForSyntaxNotifications(() => new RecordSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var contextSyntaxReceiver = (RecordSyntaxReceiver)context.SyntaxReceiver;

        foreach (var recordDeclaration in contextSyntaxReceiver!.Records)
        {
            if (recordDeclaration.ParameterList != null)
            {
                foreach (var param in recordDeclaration.ParameterList.Parameters)
                {
                    CheckNode(param.Type,param,contextSyntaxReceiver,context);

                }
            }
            

            foreach (var memberDeclaration in recordDeclaration.Members)
            {
                if (memberDeclaration is not PropertyDeclarationSyntax propertyDeclaration)
                {
                    continue;
                }
                CheckNode(propertyDeclaration.Type,propertyDeclaration,contextSyntaxReceiver,context);
            }
            
        }
    }

    private void CheckNode(TypeSyntax valueType, SyntaxNode node, RecordSyntaxReceiver contextSyntaxReceiver, GeneratorExecutionContext context)
    {
        if (valueType is not IdentifierNameSyntax type)
        {
            return;
        }

        var classDeclaration = contextSyntaxReceiver.Classes.FirstOrDefault(x => x.Identifier.Value == type.Identifier.Value);

        if (classDeclaration != null)
        {
            var d = Diagnostic.Create(new DiagnosticDescriptor(
                "FOO123456",
                "Possible reference equality",
                "Record has class member '{0}', possibly breaking value equality",
                "category",
                DiagnosticSeverity.Warning, true), node.GetLocation(), type.Identifier.Value);
            context.ReportDiagnostic(d);
        }
    }
}

public class RecordSyntaxReceiver : ISyntaxReceiver
{
    public List<RecordDeclarationSyntax> Records = new();
    public List<ClassDeclarationSyntax> Classes { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is RecordDeclarationSyntax rs) // Check for record declarations and add them to process on execute
        {
            Records.Add(rs);
        } else if (syntaxNode is ClassDeclarationSyntax cs) // Save classes, we'll check against these later
        {
            Classes.Add(cs);
        }

    }
}