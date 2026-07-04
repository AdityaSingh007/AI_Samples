namespace CopiloKit_MAF_Backend_Agent.Models
{
    public sealed record ConfirmActionInput(
        string Message,
        bool IsConfirmed = false);
}
