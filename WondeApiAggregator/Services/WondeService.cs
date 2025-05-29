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
            // Retrieve staff along with their roles and classes
            var staffResponse = await GetJsonAsync("employees?include=roles,classes&per_page=2");
            var staffData = staffResponse?["data"]?.AsArray() ?? new JsonArray();

            // Collect staff ids so we can request their classes
            var staffIds = staffData.Select(s => s?["id"]?.GetValue<string>()!)?.ToList() ?? new List<string>();

            var classesByTeacherData = new List<JsonArray>();
            foreach (var id in staffIds)
            {
                var classes = await GetJsonAsync($"classes?teachers={id}&include=students");
                classesByTeacherData.Add(classes?["data"]?.AsArray() ?? new JsonArray());
            }

            // Determine all unique student ids from the classes returned
            var studentIds = classesByTeacherData
                .SelectMany(c => c.SelectMany(cls => cls?["students"]?[
                    "data"]?.AsArray() ?? new JsonArray()))
                .Select(n => n?["id"]?.GetValue<string>())
                .Where(id => id is not null)
                .Distinct()!
                .ToList();

            // Request detailed student information
            var studentsData = new JsonArray();
            foreach (var sid in studentIds)
            {
                var student = await GetJsonAsync($"students/{sid}?include=attendance_summary,results,results.aspect,behaviours");
                if (student != null)
                    studentsData.Add(student["data"]!);
            }

            // Build lookup for student details by id
            var studentsById = studentsData
                .ToDictionary(s => s?["id"]!.GetValue<string>(), s => s);

            // Combine staff, classes and students into a single nested structure
            var combinedStaff = new JsonArray();
            for (int i = 0; i < staffData.Count; i++)
            {
                var staffMember = staffData[i]!.AsObject();
                var classesArray = new JsonArray();

                foreach (var cls in classesByTeacherData[i])
                {
                    var clsObj = cls!.AsObject();
                    var studentRefs = clsObj["students"]?["data"]?.AsArray();
                    var nestedStudents = new JsonArray();

                    if (studentRefs != null)
                    {
                        foreach (var sRef in studentRefs)
                        {
                            var sid = sRef?["id"]?.GetValue<string>();
                            if (sid != null && studentsById.TryGetValue(sid, out var studentDetail))
                            {
                                nestedStudents.Add(studentDetail);
                            }
                        }
                    }

                    clsObj["students"] = nestedStudents;
                    classesArray.Add(clsObj);
                }

                staffMember["classes"] = classesArray;
                combinedStaff.Add(staffMember);
            }

            var aggregated = new JsonObject
            {
                ["staff"] = combinedStaff,
                ["classesByTeacher"] = new JsonArray(classesByTeacherData.Select(c => new JsonObject { ["data"] = c })),
                ["students"] = studentsData
            };

            await File.WriteAllTextAsync(
                "aggregated_results.json",
                aggregated.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

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
