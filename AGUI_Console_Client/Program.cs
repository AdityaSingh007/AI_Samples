using AGUI.Client;
using AGUI_Console_Client;

string baseUrl = "https://localhost:7183/test-assistant";
using var httpClient = new HttpClient();
var aguiClient = new AGUIChatClient(new(httpClient, baseUrl));

var filePath = "C:\\CAE_Docs\\IPV6 format importing\\Two Devices with same address port IPV6_valid.csv";
if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    return;
}

var fileName = Path.GetFileName(filePath);
var fileExtension = Path.GetExtension(filePath);
var fileMimeType = fileExtension.ToLowerInvariant() switch
{
    ".txt" => "text/plain",
    ".pdf" => "application/pdf",
    ".json" => "application/json",
    ".csv" => "text/csv",
    _ => "application/octet-stream"
};

Console.WriteLine($"File name: {fileName}");
Console.WriteLine($"File extension: {fileExtension}");
Console.WriteLine($"File MIME type: {fileMimeType}");

var dataBytes = await File.ReadAllBytesAsync(filePath);


await SampleClient.RunAsync(aguiClient, Console.Out, dataBytes, fileName: fileName, fileMimeType: fileMimeType).ConfigureAwait(false);
