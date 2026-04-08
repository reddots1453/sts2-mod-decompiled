using System.Text.Json;
using System.Text.Json.Serialization;

namespace ContribTests;

public class TestResult
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; set; } = "";

    [JsonPropertyName("scenario_name")]
    public string ScenarioName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("passed")]
    public bool Passed { get; set; } = true;

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("skip_reason")]
    public string? SkipReason { get; set; }

    [JsonPropertyName("expected")]
    public Dictionary<string, string> ExpectedValues { get; set; } = new();

    [JsonPropertyName("actual")]
    public Dictionary<string, string> ActualValues { get; set; } = new();

    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }

    public bool Skipped => SkipReason != null;

    /// <summary>Record an assertion failure. First failure wins.</summary>
    public void Fail(string field, string expected, string actual)
    {
        ExpectedValues[field] = expected;
        ActualValues[field] = actual;
        if (Passed)
        {
            Passed = false;
            FailureReason = $"{field}: expected {expected}, got {actual}";
        }
    }

    /// <summary>Record a passing assertion for documentation.</summary>
    public void Pass(string field, string value)
    {
        ExpectedValues[field] = value;
        ActualValues[field] = value;
    }
}

public class TestReport
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = "";

    [JsonPropertyName("total")]
    public int TotalTests { get; set; }

    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("results")]
    public List<TestResult> Results { get; set; } = new();

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        return JsonSerializer.Serialize(this, options);
    }
}
