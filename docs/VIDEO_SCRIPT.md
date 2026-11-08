# Video script – RaceDay Part 3 (about 15 minutes)

Your Part 2 feedback said the video must cover **all endpoints, authentication, Swagger and unit tests with clear code explanation**. This script covers those again (for the API your Part 3 depends on) **plus** everything in the Part 3 rubric. Record your screen, speak while you click, and *open the code file each time you mention it*.

**Before recording:** run `docker compose up --build`, make sure Azure is configured (README §2), clear your browser tab history, and have two browser windows ready (Organiser + Participant, one in a private window).

| Time | Show | Say / do (rubric item) |
|---|---|---|
| 0:00 | Title slide / README | Introduce yourself, the RaceDay system and the three parts. Show the architecture diagram in the README. |
| 0:45 | Repo + GitHub | Folder structure, **20+ commits** on the commit history page, the green **Actions** run (CI/CD marks). |
| 1:30 | `docker-compose.yml`, both `Dockerfile`s | **Docker:** multi-stage build, restore layer cached, non-root user, port 8080. Run command: `docker compose up --build`. Show the three containers running (`docker ps`). Mention the app behaves the same locally because only `ApiSettings__BaseUrl` changes. |
| 3:00 | Swagger (`/swagger`) | **Swagger:** scroll through **every endpoint group** (Auth, Users, Events, Categories, Enrollments, Organizer enrolments, Results, Dashboard, upload endpoints). Open 2–3 endpoints and point at *purpose, expected input, responses*. |
| 4:30 | Swagger – try it | `POST /api/auth/login` → then call an Organiser endpoint, then log in as Participant and call it again → **403**. Call one without logging in → **401**. This proves **role-based access**. |
| 5:30 | API code | `Program.cs` (session, auth scheme), `SessionAuthenticationMiddleware.cs`, `PasswordService.cs` (**hashing + salt**), one controller (e.g. `EventController`) – explain `[Authorize(Roles=...)]` and the ownership check. |
| 7:00 | Unit tests | Open `PROG2026_part2/tests`. Explain `RaceDayApiFactory` (in-memory DB + fake Azure). Run `dotnet test` and show **all tests passing**. Name the test classes: Auth, EventManagement, RoleAccess, Enrollment, Category, Result, BlobUpload. |
| 8:30 | MVC app – public | Home page: colours, filters (search, type, dates). Open an event: banner image from Azure, categories. |
| 9:15 | Register + Login | Register a **Participant** and an **Organiser** (two windows). Explain `AccountController` + `ApiService.LoginAsync` + what is stored in the MVC session (UserId, Role, FullName, API cookie). Show a protected URL while logged out → **redirect to Login**. |
| 10:15 | **Organiser** window | Dashboard → create an event **with a banner** → add categories → show the image in Azure Portal (Storage account → container `raceday-images`) → edit the event. Mention `OrganizerEventsController` uploads through the API, **MVC never talks to Azure directly** (show `ApiService.UploadFileAsync` and the API's `AzureBlobStorageService`). |
| 12:00 | **Participant** window | Home → event details → **enrol** and choose a category → My Events (status badge) → Profile: update details and **upload a profile picture**. |
| 13:00 | Organiser again | Enrolments page (participant, category, status) → **Results entry**: add a result → switch to Participant → **My Results** shows "1st out of 1 finishers". |
| 14:00 | Role navigation + logout | Compare the two menus. Try an Organiser URL as Participant → **Access denied**. Log out → returns to Login, session cleared. |
| 14:30 | Wrap-up | Summarise: API + MVC + Azure Blob + Docker + CI. Thank the marker. |

## Tips
* Say *why*, not just *what*: "I hash passwords so a database leak does not expose them", "the API checks the owner so one organiser cannot edit another's event".
* Keep the code on screen for at least 10 seconds each time you explain it.
* Upload as **Unlisted** on YouTube and paste the link into the README.
