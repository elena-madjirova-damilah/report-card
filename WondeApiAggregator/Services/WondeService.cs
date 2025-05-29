using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;

namespace WondeApiAggregator.Services
{
    public class WondeService
    {
        private readonly HttpClient _client;

        private const string BaseUrl = "https://api.wonde.com/v1.0";
        private const string Token = "779c8206c48425b2e48821aaf4e205cc561446db";

        public WondeService(HttpClient client)
        {
            _client = client;
            _client.BaseAddress = new Uri(BaseUrl);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        public async Task<JsonObject> AggregateAsync()
        {
            var staff = await GetJsonAsync("/staff?include=roles,classes");
            var staffIds = staff?["data"]?.AsArray().Select(s => s?["id"]?.GetValue<string>()!)?.ToList() ?? new List<string>();

            var classesByTeacher = new JsonArray();
            foreach (var id in staffIds)
            {
                var classes = await GetJsonAsync($"/classes?teachers={id}&include=students");
                if (classes != null)
                    classesByTeacher.Add(classes);
            }

            var classesWithStudents = await GetJsonAsync("/classes?include=students&has_students=true");

            var studentIds = classesWithStudents?["data"]?
                .AsArray()
                .SelectMany(c => c?["students"]?.AsArray().Select(s => s?["id"]?.GetValue<string>()!))
                .Distinct()
                .ToList() ?? new List<string>();

            var students = new JsonArray();
            foreach (var sid in studentIds)
            {
                var student = await GetJsonAsync($"/students/{sid}?include=attendance_summary,results,results.aspect,behaviours");
                if (student != null)
                    students.Add(student);
            }

            var aggregated = new JsonObject
            {
                ["staff"] = staff,
                ["classesByTeacher"] = classesByTeacher,
                ["classesWithStudents"] = classesWithStudents,
                ["students"] = students
            };

            await File.WriteAllTextAsync("aggregated_results.json", aggregated.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            return aggregated;
        }

        private async Task<JsonObject?> GetJsonAsync(string path)
        {
            using var resp = await _client.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            var node = await JsonNode.ParseAsync(stream);
            return node as JsonObject;
        }
    }
}
