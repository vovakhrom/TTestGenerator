using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using TestGeneratorLib.Info;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestGeneratorLib
{
    public static class TestsGenerator
    {
        private static readonly SyntaxToken PublicModifier;
        private static readonly TypeSyntax VoidReturnType;
        private static readonly AttributeSyntax SetupAttribute;
        private static readonly AttributeSyntax MethodAttribute;
        private static readonly AttributeSyntax ClassAttribute;

        static TestsGenerator()
        {
            PublicModifier = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            VoidReturnType = SyntaxFactory.ParseTypeName("void");
            SetupAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("SetUp"));
            MethodAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("Test"));
            ClassAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("TestFixture"));
        }

        public static Dictionary<string, string> GenerateTests(FileInfo fileInfo)
        {
            var fileNameCode = new Dictionary<string, string>();

            foreach (var classInfo in fileInfo.Classes)
            {
                var classDeclaration = GenerateClass(classInfo);
                var compilationUnit = SyntaxFactory.CompilationUnit()
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")))
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework")))
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("MainPart.Files")))
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Moq")))
                    .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")))
                    .AddMembers(classDeclaration);
                fileNameCode.Add(classInfo.ClassName + "Test", compilationUnit.NormalizeWhitespace().ToFullString());
            }

            return fileNameCode;
        }

        private static ClassDeclarationSyntax GenerateClass(ClassInfo classInfo)
        {
            var fields = new List<FieldDeclarationSyntax>();
            VariableDeclarationSyntax variable;
            var interfaces = new Dictionary<string, string>();
            ConstructorInfo constructor = null;
            if (classInfo.Constructors.Count > 0)
            {
                constructor = FindLargestConstructor(classInfo.Constructors);
                interfaces = GetCustomTypeVariables(constructor.Parameters);
                foreach (var custom in interfaces)
                {
                    variable = GenerateVariable("_" + custom.Key, $"Mock<{custom.Value}>");
                    fields.Add(GenerateField(variable));
                }
            }

            variable = GenerateVariable(GetClassVariableName(classInfo.ClassName), classInfo.ClassName);
            fields.Add(GenerateField(variable));
            var methods = new List<MethodDeclarationSyntax>();
            methods.Add(GenerateSetUpMethod(constructor, classInfo.ClassName));
            foreach (var methodInfo in classInfo.Methods)
            {
                methods.Add(GenerateMethod(methodInfo, classInfo.ClassName));
            }

            return SyntaxFactory.ClassDeclaration(classInfo.ClassName + "Test")
                .AddMembers(fields.ToArray())
                .AddMembers(methods.ToArray())
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.AttributeList().Attributes.Add(ClassAttribute)));
        }

        private static ConstructorInfo FindLargestConstructor(List<ConstructorInfo> constructors)
        {
            var constructor = constructors[0];
            foreach (var temp in constructors)
            {
                if (constructor.Parameters.Count < temp.Parameters.Count)
                {
                    constructor = temp;
                }
            }
            return constructor;
        }

        private static Dictionary<string, string> GetBaseTypeVariables(Dictionary<string, string> parameters)
        {
            var res = new Dictionary<string, string>();
            foreach (var parameter in parameters)
            {
                if (parameter.Value[0] != 'I')
                {
                    res.Add(parameter.Key, parameter.Value);
                }
            }

            return res;
        }

        private static Dictionary<string, string> GetCustomTypeVariables(Dictionary<string, string> parameters)
        {
            var res = new Dictionary<string, string>();

            foreach (var parameter in parameters)
            {
                if (parameter.Value[0] == 'I')
                {
                    res.Add(parameter.Key, parameter.Value);
                }
            }

            return res;
        }

        private static string ConvertParametersToStringRepresentation(Dictionary<string, string> parameters)
        {
            var s = "";
            foreach (var pair in parameters)
            {
                s += pair.Value[0] == 'I' ? $"_{pair.Key}.Object" : $"{pair.Key}";
                s += ", ";
            }

            return s.Length > 0 ? s.Remove(s.Length - 2, 2) : "";
        }

        private static string GetClassVariableName(string className)
        {
            return "_" + className[0].ToString().ToLower() + className.Remove(0, 1);
        }

        private static StatementSyntax GenerateBasesTypesAssignStatement(string varName, string varType)
        {
            return SyntaxFactory.ParseStatement(string.Format
            (
                "var {0} = default({1});",
                varName,
                varType
            ));
        }

        private static StatementSyntax GenerateCustomsTypesAssignStatement(string varName, string constructorName, string invokeArgs = "")
        {
            return SyntaxFactory.ParseStatement(string.Format
            (
                "{0} = new {1}{2};",
                varName,
                constructorName,
                $"({invokeArgs})"
            ));
        }

        private static StatementSyntax GenerateFunctionCall(string varName, string funcName, string invokeArgs = "")
        {
            return SyntaxFactory.ParseStatement(string.Format
            (
                "var {0} = {1}{2};",
                varName,
                funcName,
                $"({invokeArgs})"
            ));
        }

        private static StatementSyntax GenerateVoidFunctionCall(string funcName, string invokeArgs = "")
        {
            return SyntaxFactory.ParseStatement(string.Format
            (
                "{0}{1};",
                funcName,
                $"({invokeArgs})"
            ));
        }

        private static VariableDeclarationSyntax GenerateVariable(string varName, string typeName)
        {
            return SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(typeName))
                .AddVariables(SyntaxFactory.VariableDeclarator(varName));
        }

        private static FieldDeclarationSyntax GenerateField(VariableDeclarationSyntax var)
        {
            return SyntaxFactory.FieldDeclaration(var)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        }

        private static void GenerateArrangePart(List<StatementSyntax> body, Dictionary<string, string> parameters)
        {
            var baseTypeVars = GetBaseTypeVariables(parameters);
            foreach (var var in baseTypeVars)
            {
                body.Add(GenerateBasesTypesAssignStatement(var.Key, var.Value));
            }
        }

        private static void GenerateActPart(List<StatementSyntax> body, MethodInfo methodInfo, string checkedClassVariable)
        {
            if (methodInfo.ReturnType != "void")
            {
                body.Add(GenerateFunctionCall("actual", GetClassVariableName(checkedClassVariable) + "." + methodInfo.Name, ConvertParametersToStringRepresentation(methodInfo.Parameters)));
            }
            else
            {
                body.Add(GenerateVoidFunctionCall(GetClassVariableName(checkedClassVariable) + "." + methodInfo.Name, ConvertParametersToStringRepresentation(methodInfo.Parameters)));
            }
        }

        private static InvocationExpressionSyntax GenerateExpression(string firstCall, string secondCall)
        {
            return SyntaxFactory.InvocationExpression(
                       SyntaxFactory.MemberAccessExpression(
                           SyntaxKind.SimpleMemberAccessExpression,
                           SyntaxFactory.IdentifierName(firstCall),
                           SyntaxFactory.IdentifierName(secondCall)));
        }

        private static void GenerateAssertPart(List<StatementSyntax> body, string returnType)
        {
            body.Add(GenerateBasesTypesAssignStatement("expected", returnType));
            var invocationExpression = GenerateExpression("Assert", "That");
            var secondPart = GenerateExpression("Is", "EqualTo").WithArgumentList(ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                new SyntaxNodeOrToken[] {
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("expected"))})));
            var argList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                new SyntaxNodeOrToken[] {
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("actual")),
                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(secondPart.ToString()))}));

            var s = ExpressionStatement(invocationExpression.WithArgumentList(argList));
            body.Add(s);
        }

        private static MethodDeclarationSyntax GenerateSetUpMethod(ConstructorInfo constructorInfo, string className)
        {
            List<StatementSyntax> body = new List<StatementSyntax>();
            if (constructorInfo != null)
            {
                var baseTypeVars = GetBaseTypeVariables(constructorInfo.Parameters);
                foreach (var var in baseTypeVars)
                {
                    body.Add(GenerateBasesTypesAssignStatement(var.Key, var.Value));
                }

                var customVars = GetCustomTypeVariables(constructorInfo.Parameters);
                foreach (var var in customVars)
                {
                    body.Add(GenerateCustomsTypesAssignStatement("_" + var.Key, $"Mock<{var.Value}>", ""));
                }
            }

            body.Add(GenerateCustomsTypesAssignStatement(
                GetClassVariableName(className),
                className,
                constructorInfo != null ? ConvertParametersToStringRepresentation(constructorInfo.Parameters) : ""));
            return SyntaxFactory.MethodDeclaration(VoidReturnType, "SetUp")
                .AddModifiers(PublicModifier)
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.AttributeList().Attributes.Add(SetupAttribute)))
                .WithBody(SyntaxFactory.Block(body)); ;
        }

        private static MethodDeclarationSyntax GenerateMethod(MethodInfo methodInfo, string checkedClassVar)
        {
            List<StatementSyntax> body = new List<StatementSyntax>();
            GenerateArrangePart(body, methodInfo.Parameters);
            GenerateActPart(body, methodInfo, checkedClassVar);
            if (methodInfo.ReturnType != "void")
            {
                GenerateAssertPart(body, methodInfo.ReturnType);
            }

            body.Add(CreateFailExpression());
            return SyntaxFactory.MethodDeclaration(VoidReturnType, methodInfo.Name)
                .AddModifiers(PublicModifier)
                .AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.AttributeList().Attributes.Add(MethodAttribute)))
                .WithBody(SyntaxFactory.Block(body)); ;
        }

        private static ExpressionStatementSyntax CreateFailExpression()
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Assert"),
                            SyntaxFactory.IdentifierName("Fail")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal("autogenerated")))))));
        }
    }
}