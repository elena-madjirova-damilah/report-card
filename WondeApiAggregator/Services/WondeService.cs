using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;

namespace WondeApiAggregator.Services
{
    public class WondeService
    {
        private readonly HttpClient _client;

        private const string BaseUrl = "https://api.wonde.com/v1.0/schools/A1930499544/";

        private const string Token = "779c8206c48425b2e48821aaf4e205cc561446db";


        public WondeService(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri(BaseUrl);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        public async Task<JsonObject> AggregateAsync()
        {
            var staff = await GetJsonAsync("employees?include=roles,classes&per_page=2");
            var staffIds = staff?["data"]?.AsArray().Select(s => s?["id"]?.GetValue<string>()!)?.ToList() ?? new List<string>();

            var classesByTeacher = new JsonArray();
            foreach (var id in staffIds)
            {
                var classes = await GetJsonAsync($"classes?teachers={id}&include=students");
                if (classes != null)
                    classesByTeacher.Add(classes);
            }

            var studentIds = classesByTeacher.AsArray()
                .SelectMany(c => c?["data"]?.AsArray().Select(s => s?["id"]?.GetValue<string>()!))
                .Distinct()
                .ToList() ?? new List<string>();

            var students = new JsonArray();
            foreach (var sid in studentIds)
            {
                var student = await GetJsonAsync($"students/{sid}?include=attendance_summary,results,results.aspect,behaviours");
                if (student != null)
                    students.Add(student);
            }

            var aggregated = new JsonObject
            {
                ["staff"] = staff,
                ["classesByTeacher"] = classesByTeacher,
                ["students"] = students
            };

            await File.WriteAllTextAsync("aggregated_results.json", aggregated.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            return aggregated;
        }

        private async Task<JsonObject?> GetJsonAsync(string path)
        {
            using var resp = await _client.GetAsync(path);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Requesting URL: {_client.BaseAddress}{path}");
                // Optionally log the missing resource here
                return null;
            }

            resp.EnsureSuccessStatusCode();

            var stream = await resp.Content.ReadAsStreamAsync();
            var node = JsonNode.Parse(await new StreamReader(stream).ReadToEndAsync());
            return node as JsonObject;
        }
    }
}
