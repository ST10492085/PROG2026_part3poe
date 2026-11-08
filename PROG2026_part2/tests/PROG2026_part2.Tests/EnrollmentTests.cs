using System.Net;
using System.Net.Http.Json;
using PROG2026_part2.DTOs;

namespace PROG2026_part2.Tests;

/// <summary>
/// Event enrolment tests: Participants can enter an event by choosing a category, the link between
/// participant, event and category is recorded, and invalid enrolments are rejected.
/// </summary>
public class EnrollmentTests : ApiTestBase
{
    public EnrollmentTests(RaceDayApiFactory factory) : base(factory) { }

    // ------------------------------------------------------------------
    // Success scenarios
    // ------------------------------------------------------------------

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecordedCorrectly()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, participant, participantEmail) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient, "Enrolment Event");
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id, "Senior");

        HttpResponseMessage response = await EnrolAsync(participantClient, raceEvent.Id, category.Id);
        EnrollmentResponse? enrolment = await response.Content.ReadFromJsonAsync<EnrollmentResponse>();

        // The record links participant + event + category.
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(participant.UserId, enrolment!.UserId);
        Assert.Equal(raceEvent.Id, enrolment.EventId);
        Assert.Equal(category.Id, enrolment.CategoryId);
        Assert.Equal("Enrolment Event", enrolment.EventName);
        Assert.Equal("Senior", enrolment.CategoryName);

        // The participant can see it in their own enrolments...
        HttpResponseMessage mine = await participantClient.GetAsync("/api/enrollments/me");
        List<EnrollmentResponse>? myEnrolments = await mine.Content.ReadFromJsonAsync<List<EnrollmentResponse>>();
        Assert.Contains(myEnrolments!, e => e.Id == enrolment.Id);

        // ...and the organizer can see it for their event.
        HttpResponseMessage organizerView = await organizerClient.GetAsync(
            $"/api/organizer/events/{raceEvent.Id}/enrollments");
        List<OrganizerEnrollmentResponse>? all =
            await organizerView.Content.ReadFromJsonAsync<List<OrganizerEnrollmentResponse>>();

        Assert.Equal(HttpStatusCode.OK, organizerView.StatusCode);
        Assert.Contains(all!, e => e.ParticipantEmail == participantEmail && e.CategoryId == category.Id);
    }

    [Fact]
    public async Task Participant_CanCancelEnrolment()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);
        await EnrolAsync(participantClient, raceEvent.Id, category.Id);

        HttpResponseMessage cancel = await participantClient.DeleteAsync(
            $"/api/events/{raceEvent.Id}/enrollments/me");

        HttpResponseMessage mine = await participantClient.GetAsync("/api/enrollments/me");
        List<EnrollmentResponse>? myEnrolments = await mine.Content.ReadFromJsonAsync<List<EnrollmentResponse>>();

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Empty(myEnrolments!);
    }

    [Fact]
    public async Task Organizer_CanRemoveAnEnrolmentFromTheirEvent()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);
        HttpResponseMessage enrolResponse = await EnrolAsync(participantClient, raceEvent.Id, category.Id);
        EnrollmentResponse? enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrollmentResponse>();

        HttpResponseMessage remove = await organizerClient.DeleteAsync(
            $"/api/organizer/events/{raceEvent.Id}/enrollments/{enrolment!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
    }

    // ------------------------------------------------------------------
    // Failure scenarios
    // ------------------------------------------------------------------

    [Fact]
    public async Task Enrol_TwiceInTheSameEvent_ReturnsConflict()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);

        HttpResponseMessage first = await EnrolAsync(participantClient, raceEvent.Id, category.Id);
        HttpResponseMessage second = await EnrolAsync(participantClient, raceEvent.Id, category.Id);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_ReturnsBadRequest()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse eventOne = await CreateEventAsync(organizerClient, "Event One");
        EventResponse eventTwo = await CreateEventAsync(organizerClient, "Event Two");
        CategoryResponse categoryOfEventTwo = await CreateCategoryAsync(organizerClient, eventTwo.Id);

        // The category exists, but it belongs to a different event.
        HttpResponseMessage response = await EnrolAsync(participantClient, eventOne.Id, categoryOfEventTwo.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InEventThatDoesNotExist_ReturnsNotFound()
    {
        var (participantClient, _, _) = await LoginAsParticipantAsync();

        HttpResponseMessage response = await EnrolAsync(participantClient, 999999, 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithInvalidCategoryId_ReturnsBadRequest()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);

        HttpResponseMessage response = await EnrolAsync(participantClient, raceEvent.Id, 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_WhenNotEnrolled_ReturnsNotFound()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (participantClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);

        HttpResponseMessage response = await participantClient.DeleteAsync(
            $"/api/events/{raceEvent.Id}/enrollments/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_CannotViewEnrolmentsOfAnotherOrganizersEvent()
    {
        var (ownerClient, _, _) = await LoginAsOrganizerAsync();
        var (otherClient, _, _) = await LoginAsOrganizerAsync();
        EventResponse raceEvent = await CreateEventAsync(ownerClient);

        HttpResponseMessage response = await otherClient.GetAsync(
            $"/api/organizer/events/{raceEvent.Id}/enrollments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organizer_ViewingEnrolmentsOfMissingEvent_ReturnsNotFound()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();

        HttpResponseMessage response = await organizerClient.GetAsync(
            "/api/organizer/events/999999/enrollments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participants_OnlySeeTheirOwnEnrolments()
    {
        var (organizerClient, _, _) = await LoginAsOrganizerAsync();
        var (firstClient, _, _) = await LoginAsParticipantAsync();
        var (secondClient, _, _) = await LoginAsParticipantAsync();
        EventResponse raceEvent = await CreateEventAsync(organizerClient);
        CategoryResponse category = await CreateCategoryAsync(organizerClient, raceEvent.Id);
        await EnrolAsync(firstClient, raceEvent.Id, category.Id);

        HttpResponseMessage response = await secondClient.GetAsync("/api/enrollments/me");
        List<EnrollmentResponse>? secondParticipantEnrolments =
            await response.Content.ReadFromJsonAsync<List<EnrollmentResponse>>();

        Assert.Empty(secondParticipantEnrolments!);
    }
}
