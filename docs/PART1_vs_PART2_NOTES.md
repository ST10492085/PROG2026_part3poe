# Part 1 plan vs. what was built (honest comparison)

Part 3 is built on the **Part 2 API as implemented**. Where the Part 1 plan and Part 2 differ, Part 2 is the contract.

| Topic | Part 1 plan | Part 2 / Part 3 as built |
|---|---|---|
| Authentication | JWT token returned by `/api/auth/login` | Server-side **session cookie** (the Part 2 brief explicitly asks for session management) |
| Categories routes | `/api/categories/{id}` (get / update / delete) | `/api/events/{eventId}/categories/{categoryId}` |
| Category fields | name, distance, entry fee, max participants, min age, start batch time | name, description, min age, max age, distance |
| Enrolment | `POST /api/categories/{id}/enrollments` with payment status | `POST /api/events/{eventId}/enrollments` with the chosen `categoryId` (no payment) |
| Results | `POST /api/enrollments/{id}/result` (seconds, overall + category position, completion status) | `POST /api/events/{eventId}/results` (finish time `hh:mm:ss`, position) |
| Profiles | separate participant / organiser profile endpoints | one `/api/users/me` profile for both roles |
| Event fields | event name, date, start time, city, province, route info … | name, description, date/time, location, distance, event type (Run / Walk / Cycle) as required by the Part 2 brief |

If your lecturer expects Part 1 and Part 2 to match exactly, either update the Part 1 document to describe the built API, or extend the API – the MVC app only depends on the Part 2 endpoints listed in the README.
