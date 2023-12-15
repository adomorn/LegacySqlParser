// See https://aka.ms/new-console-template for more information

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;

const string path = @"PATH";
var processedQueries = new HashSet<string>();
foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
{
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
    var root = tree.GetRoot();

    var sqlStringLiterals = root.DescendantNodes()
        .OfType<LiteralExpressionSyntax>()
        .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression) &&
                    (l.Token.ValueText.ToUpperInvariant().Contains("SELECT") || l.Token.ValueText.ToUpperInvariant().Contains("UPDATE") ||
                     l.Token.ValueText.ToUpperInvariant().Contains("INSERT") ))
        .ToList();

    foreach (var literal in sqlStringLiterals)
    {
        var concatenatedSql = ConcatenateSqlString(literal);
        if (!string.IsNullOrWhiteSpace(concatenatedSql) && !processedQueries.Contains(concatenatedSql))
        {
            processedQueries.Add(concatenatedSql);
            var sqlStatements = ParseSql(concatenatedSql);
            foreach (var statement in sqlStatements)
            {
                if (statement is SelectStatement selectStatement)
                {
                    CheckSelectStatement(selectStatement, file, literal.GetLocation().GetLineSpan().StartLinePosition.Line + 1, concatenatedSql);
                }
            }
        }
    }
}


static string ConcatenateSqlString(SyntaxNode node)
{
    var fullExpression = node;
    while (fullExpression.Parent is BinaryExpressionSyntax binaryExpr &&
           binaryExpr.IsKind(SyntaxKind.AddExpression))
    {
        fullExpression = binaryExpr;
    }

    var stringLiterals = fullExpression.DescendantNodesAndSelf()
        .OfType<LiteralExpressionSyntax>()
        .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression))
        .Select(l => l.Token.ValueText);

    return string.Join(" ", stringLiterals);
}
static IList<TSqlStatement> ParseSql(string sql)
{
    TSql130Parser parser = new(false);
    var result = parser.Parse(new StringReader(sql), out var errors);
    if (errors.Count > 0)
    {
        foreach (var error in errors)
        {
            Console.WriteLine("Parse error: " + error.Message);
        }
    }
    if (((TSqlScript)result).Batches.Count > 0)
    {
        return ((TSqlScript)result).Batches[0].Statements;
    }
    else
    {
        return new List<TSqlStatement>(); 
    }
}
static void CheckSelectStatement(SelectStatement selectStatement, string fileName, int lineNumber, string sqlQuery)
{
    var visitor = new MyTableHintVisitor();
    selectStatement.Accept(visitor);
    if (visitor.TablesWithoutNolock.Count > 0)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"File: {fileName}, Line: {lineNumber}");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("SQL Query:");
        Console.WriteLine(sqlQuery);
        Console.ResetColor(); 

        Console.ForegroundColor = ConsoleColor.DarkRed;
        foreach (var table in visitor.TablesWithoutNolock)
        {
            Console.WriteLine($"NOLOCK hint missing for table: {table}");
        }
        Console.ResetColor(); 
    }
}

class MyTableHintVisitor : TSqlFragmentVisitor
{
    public readonly List<string> TablesWithoutNolock = new();

    public override void Visit(NamedTableReference node)
    {
        var hasNolock = node.TableHints.Any(hint => hint.HintKind == TableHintKind.NoLock);
        if (!hasNolock)
        {
            TablesWithoutNolock.Add(node.SchemaObject.BaseIdentifier.Value);
        }
    }
}

