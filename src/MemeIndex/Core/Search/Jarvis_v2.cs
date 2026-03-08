using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MemeIndex.DB;
using static MemeIndex.Core.Search.TokenType;

namespace MemeIndex.Core.Search;

public static class Jarvis_v2
{
    public static void Test_SQL()
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Expression: ");
            Console.ForegroundColor = ConsoleColor.Green;
            var input = Console.ReadLine();
            Console.ResetColor();
            Console.WriteLine();
            try
            {
                if (input == null) return;

                var tokens = Lex(input);
                var sql = Build_SQL(tokens);
                Console.WriteLine(sql);
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Print($"{e}", ConsoleColor.DarkRed);
            }
        }
    }

    // CACHE

    // todo limited cache (size + time)
    private static readonly ConcurrentDictionary<string, File_UI[]> _cache = new();

    public  static void Cache_Clear() => _cache.Clear();

    private static void Cache
        (string expression, File_UI[] files)
        =>
        _cache[expression] = files;

    private static bool Cache_TryGetValue
        (string expression, [MaybeNullWhen(false)] out File_UI[] files)
        =>
        (files = _cache.GetValueOrDefault(expression)) != null;

    // SEARCH

    public static async Task<SearchResponse> Search_ByColor(string expression, int skip = 0, int take = 100)
    {
        LogDebug($"[Jv2] Expression: {expression}");

        var sw = Stopwatch.StartNew();
        var con = await AppDB.ConnectTo_Main();
        sw.Log("db connnect");

        if (Cache_TryGetValue(expression, out var files) == false)
        {
            sw.Log("check cache");
            var tokens = Lex(expression);
            var sql = Build_SQL(tokens);
            sw.Log("expr -> SQL");
            var files_db = await con.Files_UI_GetBySQL(sql);
            sw.Log("db get files");

            files = files_db
                .OrderBy(x => x.sort)
                .Select(x => new File_UI(x))
                .ToArray();
            sw.Log("sort files");

            Cache(expression, files);
            sw.Log("cache files");
        }
        else
            sw.Log("check cache (false)");

        var files_slice = files.Skip(skip).Take(take).ToArray();
        sw.Log("slice files");
        var dir_ids = files_slice.Select(x => x.d).Distinct();
        var dirs_db = await con.Dirs_GetByIds(dir_ids);
        var dirs = dirs_db.ToDictionary(x => x.Id, x => x.Path + Path.DirectorySeparatorChar);
        sw.Log("db get dirs");

        return new SearchResponse
        {
            p = new Pagination(skip, files_slice.Length, files.Length),
            d = dirs,
            f = files_slice,
        };
    }

    // RSM+#L+BP-A0LM+(A3|A4S)
    // All terms 2 chars long!
    // Mod is 1..2 chars long!
    private static List<Token> Lex(string expression)
    {
        var result = new List<Token>();
        var i = 0;
        var len = expression.Length;
        var prev = NONE;
        var depth = 0; // 0(1(2)1)0)-1
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
            else if (GROUP_OP_PREV.HasFlag(prev) && c is '(')
            {
                Take(1, GROUP_OP);
                depth++;
            }
            else if (GROUP_ED_PREV.HasFlag(prev) && c is ')')
            {
                Take(1, GROUP_ED);
                depth--;

                if (depth < 0) Fail(); // ')' closes nothing
            }
            else if (c == ' ') i++;    // allow optional spaces
            else               Fail();

            void Take(int chars, TokenType type)
            {
                var value = expression.Substring(i, chars);
                var token = new Token(value, type);
                result.Add(token);
                i += chars;
                prev = type;
            }

            [StackTraceHidden] void Fail
                () => throw new Exception($"UNEXPECTED TOKEN '{c}' at position {i}");
        }

        if (depth > 0) throw new Exception($"{depth} UNMATCHED '('");

        return result;
    }

    // SQL

    private static string Build_SQL
        (List<Token> tokens)
    {
        const string
            sql_1 =
                """
                SELECT
                f.id, f.dir_id, f.name,
                f.size, f.mdate,
                f.image_w, f.image_h,
                exp
                (
                    SUM
                    (
                CASE t.term
                
                """,
            sql_2 =
                """
                ELSE 0
                END
                    )
                ) AS sort
                FROM files f
                JOIN tags t ON t.file_id = f.id
                GROUP BY f.id
                HAVING
                (
                
                """,
            sql_3 =
                """
                )
                ORDER BY f.id;
                """;

        return new StringBuilder()
            .Append(sql_1)
            .Build_SQL_sort  (tokens)
            .Append(sql_2)
            .Build_SQL_HAVING(tokens)
            .Append(sql_3)
            .ToString();
    }

    private static StringBuilder Build_SQL_sort
        (this StringBuilder sb, List<Token> tokens)
    {
        var terms_to_sort = tokens
            .Where(x => x.Type == TERM)
            .Select(x => x.Value)
            .GroupBy(x => x)
            .Where(x => x.Count() == 1)
            .Select(x => x.Key)
            .ToArray();

        foreach (var term in terms_to_sort)
        {
            var i_token = tokens.FindIndex(x => x.Type is TERM && x.Value == term);

            // find out if it's negative
            var negative = false;
            var negative_count = 0;
            var side_quest = 0; // how deep we are in other groups
            for (var i = i_token - 1; i >= 0; i--)
            {
                var token      = tokens[i];
                var token_next = tokens[i + 1];
                var minus = token is { Type: OP, Value: "-" };

                if      (minus && i == i_token - 1) /*  1ST ITER ONLY! before term  */ negative_count++;
                else if (minus && side_quest == 0 && token_next is { Type: GROUP_OP }) negative_count++;
                else if (token is { Type: GROUP_ED })                                  side_quest++;
                else if (token is { Type: GROUP_OP } && side_quest > 0)
                {
                    side_quest--;
                    i--; // skip potential "-"
                }
            }
            if ((negative_count & 1) == 1) negative = true;

            // account for mods
            var target_log_score = negative ? 1.0 : 4.0;
            var both_S_or_L = false;
            var i_mod = i_token + 1;
            if (i_mod < tokens.Count && tokens[i_mod] is { Type: MOD } modifier)
            {
                var mod_set = modifier.Value;
                var S = mod_set.Contains('S') != negative;
                var M = mod_set.Contains('M') != negative;
                var L = mod_set.Contains('L') != negative;
                if (mod_set.Length == 1 != negative)
                {
                    if      (S) target_log_score = 1.0;
                    else if (M) target_log_score = 2.5;
                    else if (L) target_log_score = 4.0;
                }
                else
                {
                    if      (!S) target_log_score = 3.0;
                    else if (!L) target_log_score = 2.0;
                    else if (!M) both_S_or_L = true;
                }
            }

            if (both_S_or_L) sb.Append($"WHEN '{term}' THEN min(ln(abs(1.0 - log(t.score))), ln(abs(4.0 - log(t.score))))\n");
            else             sb.Append($"WHEN '{term}' THEN ln(abs({target_log_score:F1} - log(t.score)))\n");
        }

        if (terms_to_sort.Length == 0) sb.Append("WHEN '*' THEN 0\n"); // stub for valid SQL

        return sb;
    }

    private static StringBuilder Build_SQL_HAVING
        (this StringBuilder sb, List<Token> tokens)
    {
        var len = tokens.Count;
        for (var i = 0; i < len; i++)
        {
            var expr_start = i == 0 || tokens[i - 1].Type == GROUP_OP;
            var token = tokens[i];
            var type  = token.Type;
            if (type == OP)
            {
                var value = token.Value;
                if (value == "-")
                {
                    _ = expr_start
                        ? sb.Append("    NOT ")
                        : sb.Append("AND NOT ");
                }
                else if (value == "+") sb.Append("AND     ");
                else if (value == "|") sb.Append("OR      ");
            }
            else if (type == TERM)
            {
                if (expr_start) sb.Append("        ");

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
                sb.Append($") {sign} 0\n");
            }
            else if (type == GROUP_OP) sb.Append("\n(\n");
            else if (type == GROUP_ED) sb.Append  (")\n");
        }

        return sb;
    }
}

// TOKENS

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
    MOD_PREV      = TERM,
    GROUP_OP_PREV = GROUP_OP | NONE | OP,
    GROUP_ED_PREV = GROUP_ED | TERM | MOD,
    //    `X_PREV = A | B` means `X can go after A and B`
}