using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FormWind.Analyzers;

/// <summary>
/// FW002: .NET Framework'te <c>AutoScaleMode.Font</c> ile kurulan bir formun <c>Font</c>'u <c>InitializeComponent</c>'te
/// <c>AutoScaleDimensions</c>'tan sonra atanıyor (Designer bu sırayla yazar). Uygulama PerMonitorV2 çalışırken WinForms bu
/// font atamasında ölçeklemeyi atlar: kontroller 96 DPI boyutunda kalır, fontlar ve FormWind boşlukları ise ekran DPI'ıyla
/// büyür; yazılar kırpılır. .NET (Core) WinForms'ta bu sorun yoktur.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FontScalingAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FW002";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "AutoScaleMode.Font formunda font AutoScaleDimensions'tan sonra atanıyor",
        "'{0}' AutoScaleMode.Font kullanıyor ve Font, AutoScaleDimensions'tan sonra atanıyor; .NET Framework'te PerMonitorV2 açıkken form ölçeklenmez. AutoScaleMode.Dpi kullanın (AutoScaleDimensions = 96, 96).",
        "FormWind",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "PerMonitorV2'de .NET Framework WinForms, AutoScaleDimensions'tan sonra değişen font için yerleşimi ölçeklemez; FormWind fontları ve boşlukları DPI'a göre büyüttüğünden yazılar kırpılır.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.GetTypeByMetadataName("FormWind.TailwindProvider") is null) return;
            if (!IsNetFramework(start.Compilation, start.Options)) return;
            start.RegisterSyntaxNodeAction(Analyze, SyntaxKind.MethodDeclaration);
        });
    }

    // Proje özelliği (build_property.TargetFramework = net472) ya da referans verilen WinForms'un sürümü (.NET Framework'te 4.x).
    private static bool IsNetFramework(Compilation compilation, AnalyzerOptions options)
    {
        if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.TargetFramework", out var framework)
            && !string.IsNullOrEmpty(framework))
            return framework.StartsWith("net4", System.StringComparison.OrdinalIgnoreCase);
        var winForms = compilation.ReferencedAssemblyNames.FirstOrDefault(a => a.Name == "System.Windows.Forms");
        return winForms != null && winForms.Version.Major == 4;
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        if (method.Identifier.ValueText != "InitializeComponent" || method.Body is null) return;

        int dimensions = -1;
        bool fontMode = false, usesFormWind = false;
        AssignmentExpressionSyntax? lateFont = null;
        foreach (var node in method.Body.DescendantNodes())
        {
            if (node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "SetClasses" } })
                usesFormWind = true;
            if (node is not AssignmentExpressionSyntax assignment || OwnProperty(assignment.Left) is not string property) continue;
            switch (property)
            {
                case "AutoScaleDimensions":
                    dimensions = assignment.SpanStart;
                    break;
                case "AutoScaleMode":
                    fontMode = assignment.Right is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Font" };
                    break;
                case "Font":
                    if (dimensions >= 0 && assignment.SpanStart > dimensions) lateFont ??= assignment;
                    break;
            }
        }

        if (fontMode && usesFormWind && lateFont != null)
        {
            var type = method.FirstAncestorOrSelf<TypeDeclarationSyntax>()?.Identifier.ValueText ?? "Form";
            context.ReportDiagnostic(Diagnostic.Create(Rule, lateFont.GetLocation(), type));
        }
    }

    // Formun kendi özelliği: "this.Font" ya da "Font" (yeni Designer "this." yazmaz); "btnSave.Font" sayılmaz.
    private static string? OwnProperty(ExpressionSyntax left) => left switch
    {
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => null,
    };
}
