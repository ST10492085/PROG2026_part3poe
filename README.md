# RaceDay – PROG2026 Part 3 (MVC web app + Azure Blob Storage + Docker)

RaceDay lets **Organisers** create race events and **Participants** enrol and see their results.
This repository contains the whole system:

| Folder | What it is |
|---|---|
| `PROG2026_part2/` | The **Part 2 REST API** (ASP.NET Core Web API, EF Core, SQL Server, session authentication, Swagger) – now with **Azure Blob Storage** image uploads |
| `PROG2026_part3/` | The **Part 3 MVC web application** that consumes the API |
| `PROG2026_part2/tests/` | API unit tests (xUnit) |
| `tests/PROG2026_part3.Tests/` | MVC unit tests (xUnit) |
| `docker-compose.yml` | Starts SQL Server + API + MVC with one command |
| `.github/workflows/ci.yml` | GitHub Actions: structure check, build, tests, Docker builds |

| | |
|---|---|
| **Video demonstration (unlisted YouTube)** | 👉 `<paste your YouTube link here>` |

---

## 1. How the pieces fit together

```
 Browser ──► MVC web app (PROG2026_part3) ──► Web API (PROG2026_part2) ──► SQL Server
                 (no database)                         │
                 (never talks to Azure)                └──► Azure Blob Storage (images)
```

* The MVC app has **no database**. Every read and write goes through the API (`Services/ApiService.cs`).
* **File uploads**: the browser sends the image to MVC → MVC forwards it to the API → the **API** stores it in **Azure Blob Storage** and saves the image URL in SQL Server. The MVC app never contacts Azure.
* **Sessions**: on login MVC stores the user's **ID, role and name** in its own session, plus the API's session cookie (so every API call is made as that user).

---

## 2. Run it with Docker (single command)

**Prerequisite:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (it needs about 2 GB of free RAM for SQL Server).

```bash
docker compose up --build
```

Then open:

| What | URL |
|---|---|
| **RaceDay web app (MVC)** | <http://localhost:5000> |
| API + Swagger | <http://localhost:5065/swagger> |

The first start takes a few minutes (it downloads images, builds the apps and creates the database). The database is created and seeded automatically.

Stop everything with `Ctrl+C`, then `docker compose down` (add `-v` to also delete the database).

**Running only the MVC container** (when the API is already running on your machine on port 5065):

```bash
docker build -t raceday-web ./PROG2026_part3
docker run --rm -p 5000:8080 -e ApiSettings__BaseUrl=http://host.docker.internal:5065/ raceday-web
```

The containerised app behaves exactly like the local one: the only difference is the API address, which is passed in with `ApiSettings__BaseUrl`.

### Enabling image uploads (Azure Blob Storage)
Everything works without Azure **except** uploading banners / profile pictures (the app then shows a clear "Azure Blob Storage is not configured" message).

1. In the [Azure Portal](https://portal.azure.com) create a **Storage account** (Standard, LRS is fine).
2. Storage account → **Settings → Configuration** → set **Allow Blob anonymous access** to **Enabled** and save (needed so the browser can display the images).
3. Storage account → **Security + networking → Access keys** → copy a **Connection string**.
4. Copy `.env.example` to `.env` and paste the connection string:
   ```
   AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net
   ```
5. Run `docker compose up --build` again. The API creates the `raceday-images` container the first time you upload.

> ⚠️ Never commit your connection string. `.env` is listed in `.gitignore`.

---

## 3. Run it locally without Docker

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and SQL Server LocalDB (installed with Visual Studio) or any SQL Server.

**Terminal 1 – the API**
```bash
cd PROG2026_part2
# optional: enable image uploads
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>"
dotnet run --launch-profile http        # http://localhost:5065  (Swagger at /swagger)
```
The API applies the EF Core migrations and seeds sample data automatically.
Change `ConnectionStrings:DefaultConnection` in `PROG2026_part2/appsettings.json` if you do not use LocalDB.

**Terminal 2 – the MVC app**
```bash
cd PROG2026_part3
dotnet run --launch-profile http        # http://localhost:5100
```
`PROG2026_part3/appsettings.json` already points to the API at `http://localhost:5065/`.

In **Visual Studio** you can instead open `RaceDay.sln`, choose *Configure Startup Projects → Multiple startup projects* and start both `PROG2026_part2` and `PROG2026_part3` (http profiles).

### Sample accounts (seeded automatically)
| Role | Email | Password |
|---|---|---|
| Organiser | `organizer@raceday.local` | `Organizer123!` |
| Participant | `participant@raceday.local` | `Participant123!` |

You can also register new accounts on the Register page and choose a role.

---

## 4. The two roles

| | **Organiser** | **Participant** |
|---|---|---|
| After login lands on | Dashboard | Home (events) |
| Menu | Dashboard · My Events · Browse Events · Profile | Events · My Events · My Results · Profile |
| Dashboard with event summary, enrolments per event, upcoming dates | ✅ | – |
| Create / edit / delete events (name, description, date, location, distance, type) | ✅ | – |
| Upload an **event banner** (Azure Blob Storage) | ✅ | – |
| Add / edit / delete **categories** for an event | ✅ | – |
| See everybody **enrolled** for an event, with category and status | ✅ | – |
| **Capture / publish results** (finish time + position) | ✅ | – |
| Browse upcoming events with **filters** (search, type, date range) | ✅ | ✅ |
| Event details with banner and categories | ✅ | ✅ |
| **Enrol** for an event and choose a category | – | ✅ |
| **My Events** – enrolments with status, cancel | – | ✅ |
| **My Results** – e.g. "47th out of 312 finishers" | – | ✅ |
| Profile: update details + **profile picture** (Azure Blob Storage) | ✅ | ✅ |

**Status colours** (green *Upcoming/Confirmed*, orange *Today*, amber *Awaiting result*, blue *Completed*) and event-type colours (orange *Run*, teal *Walk*, purple *Cycle*) are used throughout.

**Access control:** not logged in → redirected to the Login page; wrong role → "Access denied" page. Logging out clears the session and returns to the Login page.

---

## 5. Authentication and sessions (MVC)

1. **Register / Login** forms post to `AccountController`, which calls the API (`/api/auth/register`, `/api/auth/login`). The API hashes passwords (PBKDF2 + salt).
2. On a successful login the API returns the user and a session cookie. `ApiService.LoginAsync` keeps that cookie in the **MVC session**, and `AccountController` stores `UserId`, `Role` and `FullName` there too.
3. `[SessionAuthorize]` / `[SessionAuthorize("Organizer")]` / `[SessionAuthorize("Participant")]` (`Filters/SessionAuthorizeAttribute.cs`) protect every page.
4. `Logout` calls `/api/auth/logout`, clears the MVC session and redirects to Login.
5. The API enforces roles again on every endpoint, so the rules cannot be bypassed from the web app.

---

## 6. What was added to the API in Part 3

| Method | Route | Role | Purpose |
|---|---|---|---|
| POST | `/api/events/{eventId}/banner` | Organiser (owner) | Upload an event banner image → Azure Blob Storage, URL saved on the event |
| POST | `/api/users/me/profile-picture` | Any logged-in user | Upload a profile picture → Azure Blob Storage, URL saved on the user |

* New columns `Events.BannerImageUrl` and `Users.ProfilePictureUrl` (EF Core migration `AddImageUrls`).
* `EventResponse.BannerImageUrl` and `UserProfileResponse.ProfilePictureUrl` are returned by the existing endpoints.
* Images must be JPG, PNG, GIF or WEBP and at most 5 MB (checked by the web app **and** the API).
* All Part 2 endpoints are unchanged (see Swagger for the full list).

---

## 7. Unit tests

```bash
dotnet test RaceDay.sln
```

* **API tests** (`PROG2026_part2/tests/PROG2026_part2.Tests`): registration/login, event management, role enforcement, enrolments, categories, results **and** banner / profile-picture uploads (Azure is replaced by a fake, so no Azure account is needed).
* **MVC tests** (`tests/PROG2026_part3.Tests`): the helpers that produce statuses, colours, "47th" ordinals and finish times.

---

## 8. CI/CD (GitHub Actions)

`.github/workflows/ci.yml` runs on every push and pull request. It: checks the repository structure → restores → builds (Release) → runs all unit tests → builds both Docker images.

**Screenshot of a successful (green) build:**

<!-- TODO before submitting: save a screenshot of the green run (GitHub → Actions tab) as docs/ci-green-build.png and commit it. -->
![Green GitHub Actions build](docs/ci-green-build.png)

---

## 9. Project structure (MVC)

```
PROG2026_part3/
├── Controllers/     One controller per page group (Account, Home, Events, Participant, Profile,
│                    Organizer, OrganizerEvents, OrganizerCategories, OrganizerEnrolments, OrganizerResults)
├── Filters/         SessionAuthorizeAttribute (login + role check)
├── Helpers/         Session keys, status/colour/ordinal helpers, image checker
├── Models/Api/      Classes that match the JSON sent by the API
├── Models/ViewModels/  Classes bound to the forms
├── Services/        ApiService – the only class that talks to the API
├── Views/           Razor pages (one folder per controller + Shared layout)
├── wwwroot/         CSS theme and default images
├── Dockerfile       Multi-stage build, non-root user
└── Program.cs       Startup (session, HttpClient, routing)
```

## 10. Consistency between the parts

* **Part 2 → Part 3:** every MVC page uses an existing Part 2 endpoint; the only API additions are the two upload endpoints above.
* **Part 1 → Part 2:** see the note in the submission – the Part 2 API implements the Part 1 plan using sessions (not JWT) and a simplified model; the differences are listed in `docs/PART1_vs_PART2_NOTES.md`.
