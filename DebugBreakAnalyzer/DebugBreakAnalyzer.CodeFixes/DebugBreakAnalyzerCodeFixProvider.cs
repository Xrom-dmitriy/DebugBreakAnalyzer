using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace DebugBreakAnalyzer.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DebugBreakAnalyzerCodeFixProvider))]
    public class DebugBreakAnalyzerCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DebugBreakAnalyzerAnalyzer.BreakDebuggerDiagnosticId,
            DebugBreakAnalyzerAnalyzer.PropertyDiagnosticId,
            DebugBreakAnalyzerAnalyzer.EventDiagnosticId,
            DebugBreakAnalyzerAnalyzer.ConstantDiagnosticId,
            DebugBreakAnalyzerAnalyzer.ClassDiagnosticId,
            DebugBreakAnalyzerAnalyzer.LocalVariableDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var document = context.Document;
            var root = context.Document.GetSyntaxRootAsync(context.CancellationToken).Result;

            switch (diagnostic.Id)
            {
                case DebugBreakAnalyzerAnalyzer.BreakDebuggerDiagnosticId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Исправить вызов BreakDebugger", c => FixBreakDebuggerInvocation(document, root, c)),
                        diagnostic);
                    break;
                case DebugBreakAnalyzerAnalyzer.PropertyDiagnosticId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Исправить имя свойства", c => FixPropertyName(document, root, c)),
                        diagnostic);
                    break;
                case DebugBreakAnalyzerAnalyzer.EventDiagnosticId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Исправить имя события", c => FixEventName(document, root, c)),
                        diagnostic);
                    break;
                case DebugBreakAnalyzerAnalyzer.ConstantDiagnosticId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Исправить имя константы", c => FixConstantName(document, root, c)),
                        diagnostic);
                    break;
                case DebugBreakAnalyzerAnalyzer.ClassDiagnosticId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Исправить имя класса", c => FixClassName(document, root, c)),
                        diagnostic);
                    break;
                case DebugBreakAnalyzerAnalyzer.LocalVariableDiagnosticId:
                    context.RegisterCodeFix(
                        CodeAction.Create("Исправить имя локальной переменной", c => FixLocalVariableName(document, root, c)),
                        diagnostic);
                    break;
            }

            return Task.CompletedTask;
        }

        // Исправление вызова BreakDebugger
        private async Task<Document> FixBreakDebuggerInvocation(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var invocation = (InvocationExpressionSyntax)root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(inv => inv.Expression.ToString().Contains("BreakDebugger"));

            if (invocation != null)
            {
                var newInvocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList());
                var newRoot = root.ReplaceNode(invocation, newInvocation);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

// Исправление имени приватного свойства
        private async Task<Document> FixPropertyName(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)root.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)));

            if (propertyDeclaration != null && char.IsUpper(propertyDeclaration.Identifier.Text[0]))
            {
                var correctedName = char.ToLower(propertyDeclaration.Identifier.Text[0]) + propertyDeclaration.Identifier.Text.Substring(1);
                var newPropertyDeclaration = propertyDeclaration.WithIdentifier(SyntaxFactory.Identifier(correctedName));
                var newRoot = root.ReplaceNode(propertyDeclaration, newPropertyDeclaration);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        // Исправление имени события
        private async Task<Document> FixEventName(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var eventDeclaration = (EventDeclarationSyntax)root.DescendantNodes()
                .OfType<EventDeclarationSyntax>()
                .FirstOrDefault(e => char.IsUpper(e.Identifier.Text[0]));

            if (eventDeclaration != null)
            {
                var correctedName = char.ToLower(eventDeclaration.Identifier.Text[0]) + eventDeclaration.Identifier.Text.Substring(1);
                var newEventDeclaration = eventDeclaration.WithIdentifier(SyntaxFactory.Identifier(correctedName));
                var newRoot = root.ReplaceNode(eventDeclaration, newEventDeclaration);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        // Исправление имени константы
        private async Task<Document> FixConstantName(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)root.DescendantNodes()
                .OfType<FieldDeclarationSyntax>()
                .FirstOrDefault(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)));

            if (fieldDeclaration != null)
            {
                var constantName = fieldDeclaration.Declaration.Variables.First().Identifier.Text;
                if (char.IsUpper(constantName[0]))
                {
                    var correctedName = char.ToLower(constantName[0]) + constantName.Substring(1);
                    var newFieldDeclaration = fieldDeclaration.WithDeclaration(fieldDeclaration.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(fieldDeclaration.Declaration.Variables.First().WithIdentifier(SyntaxFactory.Identifier(correctedName)))));
                    var newRoot = root.ReplaceNode(fieldDeclaration, newFieldDeclaration);
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return document;
        }

        // Исправление имени класса
        private async Task<Document> FixClassName(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => char.IsLower(c.Identifier.Text[0]));

            if (classDeclaration != null)
            {
                var correctedName = char.ToUpper(classDeclaration.Identifier.Text[0]) + classDeclaration.Identifier.Text.Substring(1);
                var newClassDeclaration = classDeclaration.WithIdentifier(SyntaxFactory.Identifier(correctedName));
                var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
// Исправление имени локальной переменной
        private async Task<Document> FixLocalVariableName(Document document, SyntaxNode root, CancellationToken cancellationToken)
        {
            var variableDeclaration = (VariableDeclarationSyntax)root.DescendantNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault(v => v.Variables.Any(var => !char.IsLower(var.Identifier.Text[0])));

            if (variableDeclaration != null)
            {
                var correctedName = char.ToLower(variableDeclaration.Variables.First().Identifier.Text[0]) + variableDeclaration.Variables.First().Identifier.Text.Substring(1);
                var newVariableDeclaration = variableDeclaration.WithVariables(SyntaxFactory.SeparatedList(variableDeclaration.Variables.Select(v => v.WithIdentifier(SyntaxFactory.Identifier(correctedName)))));
                var newRoot = root.ReplaceNode(variableDeclaration, newVariableDeclaration);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
    }
}
