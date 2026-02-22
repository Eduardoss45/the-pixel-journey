#nullable enable

using Godot;
using Jint;
using Jint.Native;
using System;
using System.Collections.Generic;

public static class SandboxExecutor
{
    public static ExecutionResult Execute(string playerCode, LevelData? level)
    {
        try
        {
            var engine = new Jint.Engine(cfg => cfg.Strict());

            string target = level?.RequiredFunction
                            ?? level?.RequiredVariable
                            ?? "_dummy";

            string wrappedCode;

            bool isFunction = !string.IsNullOrEmpty(level?.RequiredFunction);

            if (isFunction)
            {
                wrappedCode = $@"
                    'use strict';
                    {playerCode}
                    let resultado = {target}();
                    resultado;
                ";
            }
            else
            {
                wrappedCode = $@"
                    'use strict';
                    let {target} = undefined;
                    {playerCode}
                    {target};
                ";
            }

            GD.Print($"Executando código do jogador:\n{playerCode}");
            GD.Print($"Wrapper gerado:\n{wrappedCode}");

            JsValue result = engine.Evaluate(wrappedCode);

            var extracted = new Dictionary<string, Variant>();


            if (!result.IsUndefined() && !result.IsNull())
            {
                extracted[target] = ConvertJintToGodotVariant(result);
                GD.Print($"Valor capturado para {target}: {extracted[target]}");
            }


            if (!isFunction && engine.Global.HasProperty(target))
            {
                var globalVal = engine.Global.Get(target);

                if (!globalVal.IsUndefined() && !globalVal.IsNull())
                {
                    extracted[target] = ConvertJintToGodotVariant(globalVal);
                    GD.Print($"Valor global capturado para {target}: {extracted[target]}");
                }
            }

            if (extracted.Count == 0)
                return new ExecutionResult(false, $"Nada foi definido/retornado para '{target}'.");

            return new ExecutionResult(true, "Executado com sucesso", extracted);
        }
        catch (Jint.Runtime.JavaScriptException jex)
        {
            GD.Print($"Erro JS completo: {jex.Message}");
            return new ExecutionResult(false, $"Erro JS: {jex.Message}\nLinha: {jex.Location.Start.Line}");
        }
        catch (Exception ex)
        {
            GD.Print($"Erro interno completo: {ex}");
            return new ExecutionResult(false, $"Erro interno: {ex.Message}");
        }
    }

    private static Variant ConvertJintToGodotVariant(JsValue value)
    {
        if (value.IsNumber())
            return Variant.From(value.AsNumber());

        if (value.IsString())
            return Variant.From(value.AsString());

        if (value.IsBoolean())
            return Variant.From(value.AsBoolean());

        return new Variant();
    }
}

public class ExecutionResult
{
    public bool Success { get; }
    public string Message { get; }
    public Dictionary<string, Variant> Variables { get; }

    public ExecutionResult(bool success, string message, Dictionary<string, Variant>? vars = null)
    {
        Success = success;
        Message = message;
        Variables = vars ?? new Dictionary<string, Variant>();
    }
}
