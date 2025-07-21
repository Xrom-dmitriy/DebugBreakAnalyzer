using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace DebugBreakAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DebugBreakAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string BreakDebuggerDiagnosticId = "DBA001";
        public const string PropertyDiagnosticId = "DBA002";
        public const string EventDiagnosticId = "DBA003";
        public const string ConstantDiagnosticId = "DBA004";
        public const string ClassDiagnosticId = "DBA005";
        public const string LocalVariableDiagnosticId = "DBA006";

        private static readonly LocalizableString Title = "Вызов метода BreakDebugger";
        private static readonly LocalizableString MessageFormat = "Метод 'BreakDebugger' вызван";
        private static readonly LocalizableString Description = "Обнаружен вызов метода 'BreakDebugger' в классе 'SyntaxNodeExtensions'";
        private const string Category = "Debug";

        private static readonly DiagnosticDescriptor BreakDebuggerRule = new DiagnosticDescriptor(
            BreakDebuggerDiagnosticId, Title, MessageFormat, Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly DiagnosticDescriptor PropertyRule = new DiagnosticDescriptor(
            PropertyDiagnosticId, "Имя приватного свойства", "Имя приватного свойства должно начинаться с маленькой буквы", Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor EventRule = new DiagnosticDescriptor(
            EventDiagnosticId, "Имя события", "Имя события должно начинаться с маленькой буквы", Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ConstantRule = new DiagnosticDescriptor(
            ConstantDiagnosticId, "Имя константы", "Имя константы должно начинаться с маленькой буквы", Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ClassRule = new DiagnosticDescriptor(
            ClassDiagnosticId, "Имя класса", "Имя класса должно начинаться с большой буквы", Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor LocalVariableRule = new DiagnosticDescriptor(
            LocalVariableDiagnosticId, "Имя локальной переменной", "Имя локальной переменной должно начинаться с маленькой буквы", Category,
            DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            BreakDebuggerRule, PropertyRule, EventRule, ConstantRule, ClassRule, LocalVariableRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.VariableDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeEvent, SyntaxKind.EventDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeConstant, SyntaxKind.FieldDeclaration);
        }

        // Анализ вызова BreakDebugger
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;

            if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;

                if (methodName == "BreakDebugger")
                {
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess);
                    if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                        methodSymbol.ContainingType.Name == "SyntaxNodeExtensions")
                    {
                        var diagnostic = Diagnostic.Create(BreakDebuggerRule, memberAccess.Name.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        // Анализ переменных
        private static void AnalyzeVariable(SyntaxNodeAnalysisContext context)
        {
            var declaration = (VariableDeclarationSyntax)context.Node;

            foreach (var variable in declaration.Variables)
            {
                var variableName = variable.Identifier.Text;

                // Локальные переменные должны начинаться с маленькой буквы
                if (char.IsLower(variableName[0]))
                    continue;

                var diagnostic = Diagnostic.Create(LocalVariableRule, variable.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Анализ классов
        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            var className = classDeclaration.Identifier.Text;
            if (char.IsUpper(className[0]))
                return;

            var diagnostic = Diagnostic.Create(ClassRule, classDeclaration.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        // Анализ свойств
        private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

            // Приватные свойства должны начинаться с маленькой буквы
            if (propertyDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                var propertyName = propertyDeclaration.Identifier.Text;
                if (char.IsLower(propertyName[0]))
                    return;

                var diagnostic = Diagnostic.Create(PropertyRule, propertyDeclaration.Identifier.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Анализ событий
        private static void AnalyzeEvent(SyntaxNodeAnalysisContext context)
        {
            var eventDeclaration = (EventDeclarationSyntax)context.Node;

            var eventName = eventDeclaration.Identifier.Text;
            if (char.IsLower(eventName[0]))
                return;

            var diagnostic = Diagnostic.Create(EventRule, eventDeclaration.Identifier.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        // Анализ констант
        private static void AnalyzeConstant(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

            if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                var constantName = fieldDeclaration.Declaration.Variables.First().Identifier.Text;
                if (char.IsLower(constantName[0]))
                    return;

                var diagnostic = Diagnostic.Create(ConstantRule, fieldDeclaration.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
