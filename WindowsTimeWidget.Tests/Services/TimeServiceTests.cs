using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using TimeZoneConverter;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Tests.TestData;

namespace WindowsTimeWidget.Tests.Services;

[Trait("Module", "Services.TimeService")]
public class TimeServiceTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    private const string WindowsTehran = "Iran Standard Time";
    private const string WindowsNovosibirsk = "N. Central Asia Standard Time";
    private const string IanaTehran = "Asia/Tehran";
    private const string IanaNovosibirsk = "Asia/Novosibirsk";

    private static (TimeService sut, Mock<HttpMessageHandler> handler, Mock<ISettingsService> settings)
        CreateSut()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var http = handler.CreateClient();
        http.BaseAddress = new Uri("https://timeapi.io/");

        var settings = new Mock<ISettingsService>(MockBehavior.Loose);
        settings.Setup(s => s.Load()).Returns(TestHarness.ValidSettings());

        var sut = new TimeService(http, settings.Object);
        return (sut, handler, settings);
    }

    private static string ZoneUrl(string ianaId) =>
        $"https://timeapi.io/api/v1/time/current/zone?timeZone={Uri.EscapeDataString(ianaId)}";

    private static object Payload(string iana, string iso) => new
    {
        date_time = iso,
        date = iso[..10],
        time = iso.Substring(11, 8),
        day_of_week = "Saturday",
        dst_active = false,
        timezone = iana,
        utc_offset_seconds = 0
    };


    [Fact]
    public async Task SyncFromApiAsync_Success_DisablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaTehran, "2026-09-26T14:26:29+03:30")));

        // Act
        await sut.SyncFromApiAsync(WindowsTehran);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeFalse();
    }

    [Fact]
    public async Task SyncFromApiAsync_Success_RaisesTimeUpdated()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        DateTime? raised = null;
        sut.TimeUpdated += (_, t) => raised = t;

        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaTehran, "2026-09-26T14:26:29+03:30")));

        // Act
        await sut.SyncFromApiAsync(WindowsTehran);

        // Assert
        raised.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncFromApiAsync_ConvertsWindowsIdToIanaInUrl()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaNovosibirsk))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaNovosibirsk, "2026-09-26T14:26:29+07:00")));

        // Act
        await sut.SyncFromApiAsync(WindowsNovosibirsk);

        // Assert
        handler.VerifyAll();
    }

    [Fact]
    public async Task SyncFromApiAsync_AcceptsIanaIdDirectly()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaTehran, "2026-09-26T14:26:29+03:30")));

        // Act
        await sut.SyncFromApiAsync(IanaTehran);

        // Assert
        handler.VerifyAll();
    }

    [Fact]
    public async Task SyncFromApiAsync_EmptyTimeZone_FallsBackToLocalId()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();

        var localIana = TZConvert.TryWindowsToIana(TimeZoneInfo.Local.Id, out var iana)
            ? iana
            : TimeZoneInfo.Local.Id;

        handler.SetupRequest(HttpMethod.Get, ZoneUrl(localIana))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(localIana, "2026-09-26T14:26:29+00:00")));

        // Act
        await sut.SyncFromApiAsync("");

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeFalse();
    }

    [Fact]
    public async Task SyncFromApiAsync_HttpError_EnablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.InternalServerError);

        // Act
        await sut.SyncFromApiAsync(WindowsTehran);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeTrue();
    }

    [Fact]
    public async Task SyncFromApiAsync_EmptyPayload_EnablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK, "null", "application/json");

        // Act
        await sut.SyncFromApiAsync(WindowsTehran);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeTrue();
    }

    [Fact]
    public async Task SyncFromApiAsync_MalformedDateTimeString_EnablesSystemTimeFallback()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK, JsonContent.Create(new
               {
                   date_time = "not-a-date",
                   timezone = IanaTehran,
                   utc_offset_seconds = 0
               }));

        // Act
        await sut.SyncFromApiAsync(WindowsTehran);

        // Assert
        sut.IsUsingSystemTimeFallback.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentTime_AfterSync_ProjectsFromSnapshot()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        var nowUtc = DateTimeOffset.UtcNow;
        var iso = nowUtc.ToString("yyyy-MM-ddTHH:mm:sszzz");
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaTehran, iso)));

        await sut.SyncFromApiAsync(WindowsTehran);

        // Act
        var projected = sut.GetCurrentTime(WindowsTehran);

        // Assert
        var expectedTehran = TimeZoneInfo.ConvertTime(nowUtc, TimeZoneInfo.FindSystemTimeZoneById(WindowsTehran));
        projected.Should().BeCloseTo(expectedTehran.DateTime, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void GetCurrentTime_WithoutSync_UsesSystemTime()
    {
        // Arrange
        var (sut, _, _) = CreateSut();

        // Act
        var before = DateTime.UtcNow;
        var result = sut.GetCurrentTime(Utc.Id);
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeOnOrAfter(before.AddSeconds(-1))
                     .And.BeOnOrBefore(after.AddSeconds(1));
    }

    [Fact]
    public async Task GetCurrentTime_AfterZoneChange_ReflectsNewZone()
    {
        // Arrange
        var instant = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

        var tehranIso = instant.ToOffset(TimeSpan.FromHours(3.5))
                               .ToString("yyyy-MM-ddTHH:mm:sszzz");
        var novosibirskIso = instant.ToOffset(TimeSpan.FromHours(7))
                                    .ToString("yyyy-MM-ddTHH:mm:sszzz");

        var (sut, handler, _) = CreateSut();

        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaTehran, tehranIso)));

        await sut.SyncFromApiAsync(WindowsTehran, CancellationToken.None);

        // Act
        var tehran = sut.GetCurrentTime(WindowsTehran);

        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaNovosibirsk))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaNovosibirsk, novosibirskIso)));

        await sut.SyncFromApiAsync(WindowsNovosibirsk, CancellationToken.None);
        var novosibirsk = sut.GetCurrentTime(WindowsNovosibirsk);

        // Assert
        (novosibirsk - tehran)
            .Should().BeCloseTo(TimeSpan.FromHours(3.5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task GetCurrentTime_ForZoneDifferentFromSnapshot_UsesCorrectOffset()
    {
        // Arrange
        var instant = new DateTimeOffset(2026, 9, 26, 7, 26, 29, TimeSpan.Zero);
        var novosibirskIso = instant.ToOffset(TimeSpan.FromHours(7))
                                    .ToString("yyyy-MM-ddTHH:mm:sszzz");

        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaNovosibirsk))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaNovosibirsk, novosibirskIso)));

        await sut.SyncFromApiAsync(WindowsNovosibirsk);

        // Act
        var asNovosibirsk = sut.GetCurrentTime(WindowsNovosibirsk);
        var asUtc = sut.GetCurrentTime("UTC");

        // Assert
        (asNovosibirsk - asUtc)
            .Should().BeCloseTo(TimeSpan.FromHours(7), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UseSystemTime_AfterSync_FlipsFlagAndRaisesEvent()
    {
        // Arrange
        var (sut, handler, _) = CreateSut();
        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .ReturnsResponse(HttpStatusCode.OK,
                   JsonContent.Create(Payload(IanaTehran, "2026-09-26T14:26:29+03:30")));
        await sut.SyncFromApiAsync(WindowsTehran);

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
        var (sut, handler, _) = CreateSut();
        var tcs = new TaskCompletionSource<HttpResponseMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        handler.SetupRequest(HttpMethod.Get, ZoneUrl(IanaTehran))
               .Returns(() => tcs.Task);

        var first = sut.SyncFromApiAsync(WindowsTehran);

        // Act
        var second = sut.SyncFromApiAsync(WindowsTehran);

        // Assert
        second.IsCompleted.Should().BeTrue();

        tcs.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Payload(IanaTehran, "2026-09-26T14:26:29+03:30"))
        });
        await first;
    }

    [Fact]
    public void GetCurrentTime_UnknownTimeZone_FallsBackToLocal()
    {
        // Arrange
        var (sut, _, _) = CreateSut();

        // Act
        Action act = () => _ = sut.GetCurrentTime("Not/A/Real/Zone");

        // Assert
        act.Should().NotThrow();
    }
}