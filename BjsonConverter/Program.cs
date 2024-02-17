using System.Text;
using System.Text.Encodings.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

if (args.Length is 0 or >1
    || !File.Exists(args[0])
    || !(args[0].EndsWith(".bjson.json", StringComparison.OrdinalIgnoreCase)
         || args[0].EndsWith(".bjson", StringComparison.OrdinalIgnoreCase)))
{
    Console.Error.WriteLine("Expected one argument with path to .bjson or .bjson.json file");
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
string outputPath;
var serializer = JsonSerializer.Create(new()
{
    Formatting = Formatting.Indented,
});
if (path.EndsWith(".bjson.json", StringComparison.OrdinalIgnoreCase))
{
    using var jsonReader = new JsonTextReader(reader);
    outputPath = path[..^5];
    await using var output = File.Open(outputPath, new FileStreamOptions
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    });
    using var bsonWriter = new BsonDataWriter(output);
    var data = serializer.Deserialize(jsonReader);
    serializer.Serialize(bsonWriter, data);
    await bsonWriter.FlushAsync().ConfigureAwait(false);
    await output.FlushAsync().ConfigureAwait(false);
}
else
{
    using var bsonReader = new BsonDataReader(input);
    outputPath = path + ".json";
    await using var output = File.Open(outputPath, new FileStreamOptions
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    });
    await using var writer = new StreamWriter(output, utf8);
    using var jsonWriter = new JsonTextWriter(writer);
    var data = serializer.Deserialize(bsonReader);
    serializer.Serialize(jsonWriter, data);
    await jsonWriter.FlushAsync().ConfigureAwait(false);
    await writer.FlushAsync().ConfigureAwait(false);
    await output.FlushAsync().ConfigureAwait(false);
}
return 0;