using System.Text;
using static MemeIndex.Core.Search.TokenType;

namespace MemeIndex.Core.Search;

public static class Jarvis_v2
{
    public static async Task<SearchResponse> Search_ByColor(string expression)
    {
        var tokens = Lex(expression);
        // expr -lexer-> tokens
        // tokens -compiler-> SQL

        throw new NotImplementedException();
    }

    // RSM+#L+BP-A0LM+(A3|A4)L
    // All terms 2 chars long!
    // Mod is 1..2 chars long!
    private static List<Token> Lex(string expression)
    {
        var result = new List<Token>();
        var i = 0;
        var len = expression.Length;
        var prev = NONE;
        while (i < len)
        {
            var c = expression[i];
            if      (TERM_PREV.HasFlag(prev)
                  && c is >= 'A' and <= 'Z' or '#'
                  && i + 1 < len
                  && expression[i + 1] is >= 'A' and <= 'Z' or >= '0' and <= '9')
                Take(2, TERM);
            else if (OP_ANY_PREV.HasFlag(prev) && c is '+' or '|') Take(1, OP);
            else if (OP_SUB_PREV.HasFlag(prev) && c is '-')        Take(1, OP);
            else if    (MOD_PREV.HasFlag(prev) && c is 'S' or 'M' or 'L')
            {
                var l = i + 1 < len
                     && expression[i + 1] is 'S' or 'M' or 'L'
                    ? 2
                    : 1;
                Take(l, MOD);
            }
            else if (GROUP_OP_PREV.HasFlag(prev) && c is '(') Take(1, GROUP_OP);
            else if (GROUP_ED_PREV.HasFlag(prev) && c is ')') Take(1, GROUP_ED);
            else if (c != ' ')
                throw new Exception($"UNEXPECTED TOKEN {c} at position {i}");

            void Take(int chars, TokenType type)
            {
                var value = expression.Substring(i, chars);
                var token = new Token(value, type);
                result.Add(token);
                i += chars;
                prev = type;
            }
        }

        return result;
    }

    private static StringBuilder Build_SQL_sort   (List<Token> tokens)
    {
        var sb = new StringBuilder();
        // TODO
        return sb;
    }

    private static StringBuilder Build_SQL_HAVING (List<Token> tokens)
    {
        var sb = new StringBuilder();
        var len = tokens.Count;
        for (var i = 0; i < len; i++)
        {
            var token = tokens[i];
            var type  = token.Type;
            if (type == OP)
            {
                var value = token.Value;
                if (value == "-")
                {
                    var expr_start = i == 0 || tokens[i - 1].Type == GROUP_OP;
                    _ = expr_start
                        ? sb.Append("\nNOT ")
                        : sb.Append("\nAND NOT ");
                }
                else if (value == "+") sb.Append("\nAND ");
                else if (value == "|") sb.Append("\nOR ");
            }
            else if (type == TERM)
            {
                sb.Append($"SUM(t.term = '{token.Value}'");
                var negative = false;
                if (i + 1 < len && tokens[i + 1] is { Type: MOD } modifier)
                {
                    int a = 0, b = 0;
                    var mod_set = modifier.Value;
                    var S = mod_set.Contains('S');
                    var M = mod_set.Contains('M');
                    var L = mod_set.Contains('L');
                    if (mod_set.Length == 1)
                    {
                        if      (S) (a, b) = (1, 2);
                        else if (M) (a, b) = (2, 3);
                        else if (L) (a, b) = (3, 4);
                    }
                    else
                    {
                        if      (!S) (a, b) = (2, 4);
                        else if (!L) (a, b) = (1, 3);
                        else if (!M)
                        {
                            (a, b) = (2, 3);
                            negative = true;
                        }
                    }
                    sb.Append($" AND log(t.score) BETWEEN {a} AND {b}");
                }

                var sign = negative ? '=' : '>';
                sb.Append($") {sign} 0");
            }
            else if (type == GROUP_OP) sb.Append("\n(");
            else if (type == GROUP_ED) sb.Append("\n)");
        }

        return sb;
    }
}

public record struct Token(string Value, TokenType Type);

[Flags]
public enum TokenType
{
    NONE = 1,
    TERM = 2,
    MOD  = 4,
    OP   = 8,
    GROUP_OP = 16,
    GROUP_ED = 32,
    TERM_PREV     = NONE | OP  | GROUP_OP,
    OP_ANY_PREV   = TERM | MOD | GROUP_ED,
    OP_SUB_PREV   = TERM | MOD | GROUP_ED | NONE | GROUP_OP,
    MOD_PREV      = TERM | GROUP_ED,
    GROUP_OP_PREV = NONE | OP,
    GROUP_ED_PREV = TERM | MOD,
    //    `X_PREV = A | B` means `X can go after A and B`
}