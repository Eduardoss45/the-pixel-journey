using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeEditor.Logic
{
    public class HtmlValidationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string Message { get; set; } = "";
        public float Score { get; set; } = 0f;
    }

    public static class HtmlValidator
    {
        private static readonly HashSet<string> RequiredTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "html", "head", "title", "body"

        };

        private static readonly HashSet<string> SelfClosingTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "img", "meta", "br", "hr", "input", "link", "area", "base", "col", "embed",
            "param", "source", "track", "wbr", "keygen"
        };

        public static HtmlValidationResult Validate(string code)
        {
            var result = new HtmlValidationResult();
            var stack = new Stack<string>();
            var foundTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasDoctype = false;





            var regex = new Regex(
                @"<!DOCTYPE\s+html\b[^>]*>|<(/?)([a-zA-Z0-9]+)(?:\s+[^>]*)?>",
                RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            var matches = regex.Matches(code);

            foreach (Match match in matches)
            {

                if (match.Value.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    hasDoctype = true;
                    continue;
                }


                string closing = match.Groups[1].Value;
                string tag = match.Groups[2].Value.Trim().ToLowerInvariant();


                if (SelfClosingTags.Contains(tag))
                {
                    if (RequiredTags.Contains(tag)) foundTags.Add(tag);
                    continue;
                }

                if (closing == "/")
                {

                    if (stack.Count == 0)
                    {
                        result.Errors.Add($"Tag de fechamento </{tag}> sem abertura correspondente.");
                    }
                    else if (!string.Equals(stack.Peek(), tag, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add($"Fechamento incorreto: </{tag}> (esperava </{stack.Peek()}>).");
                    }
                    else
                    {
                        stack.Pop();
                    }
                }
                else
                {

                    stack.Push(tag);
                    if (RequiredTags.Contains(tag)) foundTags.Add(tag);
                }
            }


            if (stack.Count > 0)
            {
                result.Errors.Add($"Tags não fechadas: {string.Join(", ", stack)}");
            }


            foreach (var req in RequiredTags)
            {
                if (!foundTags.Contains(req))
                {
                    result.Errors.Add($"Falta a tag essencial: <{req}>");
                }
            }


            if (!hasDoctype)
            {
                result.Errors.Add("Falta a declaração <!DOCTYPE html> no início do documento.");
            }

            result.Success = result.Errors.Count == 0;
            result.Score = RequiredTags.Count > 0 ? (float)foundTags.Count / RequiredTags.Count : 1f;
            result.Message = result.Success
                ? "Parabéns! A estrutura HTML básica está correta! 🎉"
                : "Corrigir os erros listados abaixo.";

            return result;
        }
    }
}