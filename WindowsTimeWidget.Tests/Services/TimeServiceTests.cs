using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Tests.TestData;

namespace WindowsTimeWidget.Tests.Services;

[Trait("Module", "Services.TimeService")]
public class TimeServiceTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static string BASE_URL = "https://timeapi.io/";
    private static string BASE_API_URL = string.Concat(BASE_URL, "api/v1/time/current/zone*");

    private static (TimeService sut, Mock<HttpMessageHandler> handler) CreateSut()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var http = handler.CreateClient();
        http.BaseAddress = new Uri(BASE_URL);

        var settings = new Mock<ISettingsService>(MockBehavior.Loose);
        settings.Setup(s => s.Load()).Returns(TestHarness.ValidSettings());

        return (new TimeService(http, settings.Object), handler);
    }


    [Fact]
    public async Task SyncFromApiAsync_Success_DisablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler) = CreateSut();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"date_time":"2026-09-19T13:31:06.598390+00:00","date":"2026-09-19","time":"13:31:06.598390","day_of_week":"Saturday","dst_active":false,"timezone":"UTC","utc_offset_seconds":0}""",
                    Encoding.UTF8,
                    "application/json")
            }
        );

        // Act
        await sut.SyncFromApiAsync(Utc.Id);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeFalse();
        sut.LastSuccessfulSyncUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncFromApiAsync_Success_RaisesTimeUpdated()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        DateTime? raised = null;
        sut.TimeUpdated += (_, t) => raised = t;

        handler.SetupRequest(HttpMethod.Get, BASE_API_URL)
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(new { dateTime = new DateTime(2024, 3, 15, 12, 0, 0) }));

        // Act
        await sut.SyncFromApiAsync(Utc.Id);

        // Assert
        raised.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncFromApiAsync_HttpError_EnablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler) = CreateSut();

        handler.SetupRequest(HttpMethod.Get, BASE_API_URL)
               .ReturnsResponse(HttpStatusCode.InternalServerError);

        // Act
        await sut.SyncFromApiAsync(Utc.Id);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeTrue();
        sut.LastSuccessfulSyncUtc.Should().BeNull();
    }

    [Fact]
    public async Task SyncFromApiAsync_EmptyPayload_EnablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler) = CreateSut();

        handler.SetupRequest(HttpMethod.Get, BASE_API_URL)
               .ReturnsResponse(HttpStatusCode.OK, "null", "application/json");

        // Act
        await sut.SyncFromApiAsync(Utc.Id);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentTime_AfterSync_ProjectsFromSnapshot()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        var apiLocal = DateTime.UtcNow;

        handler.SetupRequest(HttpMethod.Get, BASE_API_URL)
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(new { dateTime = apiLocal }));

        // Act
        await sut.SyncFromApiAsync(Utc.Id);
        var projected = sut.GetCurrentTime(Utc.Id);

        // Assert
        projected.Should().BeCloseTo(apiLocal, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void GetCurrentTime_WithoutSync_UsesSystemTime()
    {
        // Arrange
        var (sut, _) = CreateSut();

        // Act
        var before = DateTime.UtcNow;
        var result = sut.GetCurrentTime(Utc.Id);
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeOnOrAfter(before.AddSeconds(-1))
                     .And.BeOnOrBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task UseSystemTime_AfterSync_FlipsFlagAndRaisesEvent()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, BASE_API_URL)
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(new { dateTime = DateTime.UtcNow }));
        await sut.SyncFromApiAsync(Utc.Id);

        var raised = false;
        sut.TimeUpdated += (_, _) => raised = true;

        // Act
        sut.UseSystemTime();

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeTrue();
        raised.Should().BeTrue();
    }

    [Fact]
    public async Task SyncFromApiAsync_WhenAlreadyRunning_SecondCallReturnsImmediately()
    {
        // Arrange
        var (sut, handler) = CreateSut();
        var tcs = new TaskCompletionSource<HttpResponseMessage>();

        handler.SetupRequest(HttpMethod.Get, BASE_API_URL)
               .ReturnsAsync(() => tcs.Task.Result);

        // Act
        var first = sut.SyncFromApiAsync(Utc.Id);
        var second = sut.SyncFromApiAsync(Utc.Id);

        // Assert
        second.IsCompleted.Should().BeTrue();

        tcs.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { dateTime = DateTime.UtcNow })
        });
        await first;
    }


    [Fact]
    public void GetCurrentTime_UnknownTimeZone_FallsBackToLocal()
    {
        // Arrange
        var (sut, _) = CreateSut();

        // Act
        var act = () => sut.GetCurrentTime("Not/A/Real/Zone");

        // Assert
        act.Should().NotThrow();
    }
}