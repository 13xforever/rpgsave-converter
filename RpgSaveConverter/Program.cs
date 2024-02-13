using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LZStringCSharp;

if (args.Length is 0 or >1
    || !File.Exists(args[0])
    || !(args[0].EndsWith(".rpgsave.json", StringComparison.OrdinalIgnoreCase)
         || args[0].EndsWith(".rpgsave", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("Expected one argument with path to .rpgsave or .rpgsave.json file");
    return -1;
}

var path = args[0];
var utf8 = new UTF8Encoding(false);
await using var input = File.Open(path, new FileStreamOptions
{
    Mode = FileMode.Open,
    Access = FileAccess.Read,
    Share = FileShare.Read,
    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
});
using var reader = new StreamReader(input, utf8);
var inputData = await reader.ReadToEndAsync().ConfigureAwait(false);
string result, outputPath;
if (path.EndsWith(".rpgsave.json", StringComparison.OrdinalIgnoreCase))
{
    outputPath = path[..^5];
    using var json = JsonDocument.Parse(inputData);
    result = JsonSerializer.Serialize(json, new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    });
    result = LZString.CompressToBase64(result);
}
else
{
    outputPath = path + ".json";
    result = LZString.DecompressFromBase64(inputData);
    using var json = JsonDocument.Parse(result);
    result = JsonSerializer.Serialize(json, new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    });
}
await using var output = File.Open(outputPath, new FileStreamOptions
{
    Mode = FileMode.OpenOrCreate,
    Access = FileAccess.Write,
    Share = FileShare.Read,
    Options = FileOptions.Asynchronous | FileOptions.SequentialScan
});
await using var writer = new StreamWriter(output, utf8);
await writer.WriteAsync(result).ConfigureAwait(false);
await writer.FlushAsync().ConfigureAwait(false);
await output.FlushAsync().ConfigureAwait(false);
return 0;