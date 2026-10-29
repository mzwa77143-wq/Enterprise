namespace EnterpriseApp.Application.DTOs.Messaging;

public class SendMessageRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool SimulateDeadLetter { get; set; } = false;
    public Dictionary<string, string> Properties { get; set; } = new();
}
