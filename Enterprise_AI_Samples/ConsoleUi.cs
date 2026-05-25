using Spectre.Console;

namespace CAE_AI_Samples;

internal static class ConsoleUi
{
    public static void Clear() => AnsiConsole.Clear();

    public static void WriteBlankLine() => AnsiConsole.WriteLine();

    public static void WriteError(string message) =>
        AnsiConsole.MarkupLine($"[bold red]{Markup.Escape(message)}[/]");

    public static void WriteSuccess(string message) =>
        AnsiConsole.MarkupLine($"[bold green]{Markup.Escape(message)}[/]");

    public static void WriteInfo(string message) =>
        AnsiConsole.MarkupLine(Markup.Escape(message));

    public static void WriteHighlight(string message) =>
        AnsiConsole.MarkupLine($"[bold cyan]{Markup.Escape(message)}[/]");

    public static string PromptRequired(string label, bool secret = false)
    {
        var prompt = new TextPrompt<string>($"[yellow]{Markup.Escape(label)}:[/]")
            .Validate(value => !string.IsNullOrWhiteSpace(value)
                ? ValidationResult.Success()
                : ValidationResult.Error($"[red]{Markup.Escape(label)} is required.[/]"));

        if (secret)
        {
            prompt.Secret();
        }

        return AnsiConsole.Prompt(prompt);
    }

    public static string? PromptOptional(string label)
    {
        AnsiConsole.Markup($"[yellow]{Markup.Escape(label)}:[/] ");
        return Console.ReadLine();
    }
}