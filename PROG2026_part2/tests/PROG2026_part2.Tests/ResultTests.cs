using System.Net;
using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Result tests: Organizers capture finish times and positions for enrolled Participants,
/// Participants can view their own results, and invalid or unauthorised requests are rejected.
/// </summary>
public class ResultTests : ApiTestBase
{
    public ResultTests(RaceDayApiFactory factory) : base(factory) { }

    /// <summary>
    /// Arranges the common starting point: an event with a category, and a Participant enrolled in it.
    /// </summary>
    private async Task<(HttpClient Organizer, HttpClient Participant, AuthResponse ParticipantUser, EventResponse Event, CategoryResponse Category)>
        SetUpEnrolledParticipantAsync()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, participant, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient, "Results Event");
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);

        HttpResponseMessage enrol = await EnrolAsync(participantClient, raceEvent.Id, category.Id);
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        return (organizerClient, participantClient, participant, raceEvent, category);
    }

    private static CreateResultRequest ResultFor(AuthResponse participant, CategoryResponse category) => new()
    {
        UserId = participant.UserId,
        CategoryId = category.Id,
        FinishTime = TimeSpan.FromMinutes(45) + TimeSpan.FromSeconds(30),
        Position = 1
    };

    // ------------------------------------------------------------------
    // Success scenarios
    // ------------------------------------------------------------------

    [Fact]
    public async Task Organizer_CanRecordResult_AndParticipantCanViewIt()
    {
        var (organizer, participantClient, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();

        HttpResponseMessage create = await organizer.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/results", ResultFor(participant, category));
        ResultResponse? created = await create.Content.ReadFromJsonAsync<ResultResponse>();

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(1, created!.Position);
        Assert.Equal(TimeSpan.FromSeconds(45 * 60 + 30), created.FinishTime);

        // The participant sees their own result.
        HttpResponseMessage mine = await participantClient.GetAsync("/api/results/me");
        List<ResultResponse>? myResults = await mine.Content.ReadFromJsonAsync<List<ResultResponse>>();

        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Contains(myResults!, r => r.Id == created.Id);
    }

    [Fact]
    public async Task Anonymous_CanViewResultsOfAnEvent()
    {
        var (organizer, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", ResultFor(participant, category));
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync($"/api/events/{raceEvent.Id}/results");
        List<ResultResponse>? results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(results!);
    }

    [Fact]
    public async Task Organizer_CanUpdateAndDeleteResult()
    {
        var (organizer, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        HttpResponseMessage create = await organizer.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/results", ResultFor(participant, category));
        ResultResponse? created = await create.Content.ReadFromJsonAsync<ResultResponse>();

        HttpResponseMessage update = await organizer.PutAsJsonAsync(
            $"/api/events/{raceEvent.Id}/results/{created!.Id}",
            new UpdateResultRequest { FinishTime = TimeSpan.FromMinutes(40), Position = 2 });
        ResultResponse? updated = await update.Content.ReadFromJsonAsync<ResultResponse>();

        HttpResponseMessage delete = await organizer.DeleteAsync(
            $"/api/events/{raceEvent.Id}/results/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(2, updated!.Position);
        Assert.Equal(TimeSpan.FromMinutes(40), updated.FinishTime);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var (organizer, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", ResultFor(participant, category));
        var (otherParticipantClient, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await otherParticipantClient.GetAsync("/api/results/me");
        List<ResultResponse>? results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();

        Assert.Empty(results!);
    }

    // ------------------------------------------------------------------
    // Failure scenarios
    // ------------------------------------------------------------------

    [Fact]
    public async Task RecordResult_ForParticipantNotEnrolled_ReturnsBadRequest()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (_, notEnrolled, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);

        HttpResponseMessage response = await organizerClient.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/results", ResultFor(notEnrolled, category));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_Twice_ReturnsConflict()
    {
        var (organizer, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        CreateResultRequest request = ResultFor(participant, category);

        HttpResponseMessage first = await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", request);
        HttpResponseMessage second = await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", request);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithZeroFinishTime_ReturnsBadRequest()
    {
        var (organizer, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        CreateResultRequest request = ResultFor(participant, category);
        request.FinishTime = TimeSpan.Zero;

        HttpResponseMessage response = await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_WithInvalidPosition_ReturnsBadRequest()
    {
        var (organizer, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        CreateResultRequest request = ResultFor(participant, category);
        request.Position = 0;

        HttpResponseMessage response = await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotRecordResult_ForAnotherOrganizersEvent()
    {
        var (_, _, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        var (otherOrganizer, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await otherOrganizer.PostAsJsonAsync(
            $"/api/events/{raceEvent.Id}/results", ResultFor(participant, category));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrolment_CannotBeCancelled_AfterResultIsRecorded()
    {
        var (organizer, participantClient, participant, raceEvent, category) = await SetUpEnrolledParticipantAsync();
        await organizer.PostAsJsonAsync($"/api/events/{raceEvent.Id}/results", ResultFor(participant, category));

        HttpResponseMessage response = await participantClient.DeleteAsync(
            $"/api/events/{raceEvent.Id}/enrollments/me");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetResults_ForEventThatDoesNotExist_ReturnsNotFound()
    {
        HttpClient anonymous = CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/events/999999/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
