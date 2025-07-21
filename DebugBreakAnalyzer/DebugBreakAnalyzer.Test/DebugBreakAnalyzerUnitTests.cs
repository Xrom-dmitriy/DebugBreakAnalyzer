using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = DebugBreakAnalyzer.Test.CSharpCodeFixVerifier<
    DebugBreakAnalyzer.DebugBreakAnalyzerAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace DebugBreakAnalyzer.Test
{
    [TestClass]
    public class DebugBreakAnalyzerCodeFixTests
    {
        [TestMethod]
        public async Task ApplyFixForAllNamingConventions()
        {
            var test = @"
using System;

public static class SyntaxNodeExtensions // класс с маленькой буквы
{
    public static void BreakDebugger() { }
}

class Test
{
    private int localVariable = 0; // локальная переменная с заглавной буквы
    private const int SomeConstant = 42; // константа с маленькой буквы
    public event EventHandler SomeEvent; // событие с заглавной буквы
    private string _PrivateProperty; // приватное свойство с заглавной буквы
}";

            var fixedTest = @"
using System;

public static class SyntaxNodeExtensions // класс с заглавной буквы
{
    public static void BreakDebugger() { }
}

class Test
{
    private int localVariable = 0; // локальная переменная с маленькой буквы
    private const int someConstant = 42; // константа с заглавной буквы
    public event EventHandler someEvent; // событие с маленькой буквы
    private string _privateProperty; // приватное свойство с маленькой буквы

    public void Method()
    {
        SyntaxNodeExtensions.BreakDebugger(); // вызов BreakDebugger с правильным именем
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedTest);
        }
    }
}