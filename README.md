# RaceDay – Event Management System (Part 3)

## Overview

RaceDay is an event management web application developed using ASP.NET Core MVC. It provides a platform where event organisers can manage sporting events, while participants can discover events, register for participation and view their results.

This project represents **Part 3 of the RaceDay system** and builds upon the RESTful Web API developed in Part 2. The MVC application communicates with the existing API to retrieve, submit, update and delete information.

The application provides separate experiences for Organisers and Participants, with functionality and access determined by their respective roles.

## Project Objectives

The main objectives of RaceDay are to:

- Provide an accessible platform for managing sporting events.
- Allow organisers to create, manage and maintain event information.
- Allow participants to browse available events and register.
- Provide a structured way to manage event registrations.
- Allow organisers to manage participant enrolments and their statuses.
- Enable participants to view their personal results.
- Provide profile management functionality for participants.
- Demonstrate the integration of an ASP.NET Core MVC application with a RESTful Web API.

## Technologies Used

The application was developed using the following technologies:

| Technology | Purpose |
|---|---|
| C# | Main programming language |
| ASP.NET Core MVC | Web application framework |
| ASP.NET Core Web API | Backend communication and data management |
| .NET 8 | Application development platform |
| SQL Server | Database management |
| SQL Server LocalDB | Local database environment |
| HTML5 | Web page structure |
| CSS3 | Styling and responsive layouts |
| Bootstrap | UI components and responsive design |
| JavaScript | Client-side interactions |
| HttpClient | Communication between MVC and Web API |
| JSON | Data exchange between applications |
| JWT | API authentication and authorization |
| Git and GitHub | Version control and source code management |

## System Architecture

RaceDay uses a two-part architecture:

### Part 2 – RESTful Web API

The Web API is responsible for backend operations, including:

- Database communication.
- Retrieving and storing information.
- Creating, updating and deleting records.
- User registration and authentication.
- JWT generation and validation.
- Managing events, categories, participants, organisers, enrolments and results.

### Part 3 – ASP.NET Core MVC

The MVC application provides the graphical user interface through which users interact with the system.

It communicates with the Web API using `HttpClient` through a dedicated `ApiService` class.

The MVC application does not directly perform database operations. Instead, it sends HTTP requests to the API and processes the JSON responses.

**Application communication flow:**

1. A user interacts with an MVC page.
2. The relevant MVC controller receives the request.
3. The controller calls the appropriate method in `ApiService`.
4. `ApiService` sends an HTTP request to the Part 2 API.
5. The API processes the request and communicates with SQL Server.
6. The API returns a response to the MVC application.
7. The MVC application displays the returned information to the user.

## Main Features

### 1. User Registration and Authentication

RaceDay supports two types of users:

- Organisers
- Participants

Both user types can register and log in using their account credentials.

JWT authentication is used when communicating with protected API endpoints. The MVC application stores the authenticated user's information and token in session state.

### 2. Organiser Dashboard

Organisers have access to a dedicated dashboard that provides access to the event management functionality.

Organisers can:

- View available events.
- Create new events.
- Edit existing events.
- Delete events.
- Manage event categories.
- View participant enrolments.
- Update enrolment statuses.
- Manage event results.

### 3. Participant Dashboard

Participants have access to a personalised dashboard.

They can:

- Discover available sporting events.
- View event information.
- Register for events.
- View their enrolments.
- Check their enrolment statuses.
- View their personal results.
- Manage their personal profiles.

### 4. Event Management

The event management functionality allows organisers to maintain event information.

Each event includes details such as:

- Event ID.
- Event name.
- Location.
- Event date.
- Category.
- Organiser.
- Event banner.

Participants can browse events and view their details but cannot access organiser-only event management actions.

### 5. Category Management

Organisers can manage event categories through the MVC interface.

The functionality includes:

- Viewing categories.
- Adding categories.
- Updating category information.
- Deleting categories.

Categories help organise sporting events into relevant groups.

### 6. Event Enrolments

Participants can enrol in available events.

The system supports enrolment management, allowing organisers to view participant registrations and manage their statuses.

Enrolment statuses include:

- Pending
- Confirmed

Participants can view the current status of their registrations through their dashboard.

### 7. Results Management

Organisers can manage event results.

Participants can access their own results, allowing them to track their performance in registered events.

### 8. Participant Profile Management

Participants can manage their personal information through the Manage My Profile section.

This includes:

- Viewing personal information.
- Updating their first name.
- Updating their surname.
- Updating their email address.
- Uploading a profile picture.
- Replacing an existing profile picture.

Profile pictures are stored locally within the MVC application's `wwwroot` directory.

### 9. Local Image Storage

The application supports local image storage for event banners and participant profile pictures.

Images are stored in the following directories:

**Event banners:**
```text
wwwroot/
    uploads/
        event-banners/
```

**Participant profile pictures:**
```text
wwwroot/
    uploads/
        profile-pictures/
```

Only the relative image paths are stored in the database through the Web API.

Supported image formats include:

- JPG
- JPEG
- PNG
- WEBP

Image uploads are limited to 5 MB.

This implementation uses local file storage rather than cloud-based storage services.

## Project Structure

The MVC application follows the ASP.NET Core MVC architectural pattern.

```text
RaceDay_part_3/
│
├── Controllers/
│   ├── HomeController.cs
│   ├── OrganizerController.cs
│   ├── ParticipantController.cs
│   ├── CategoryController.cs
│   ├── EventController.cs
│   ├── EnrollmentController.cs
│   └── ResultController.cs
│
├── Services/
│   └── ApiService.cs
│
├── ViewModels/
│   ├── OrganizerLoginViewModel.cs
│   ├── OrganizerRegisterViewModel.cs
│   ├── ParticipantLoginViewModel.cs
│   ├── ParticipantRegisterViewModel.cs
│   ├── ParticipantProfileViewModel.cs
│   ├── EventViewModel.cs
│   ├── CategoryViewModel.cs
│   ├── EnrollmentViewModel.cs
│   └── ResultViewModel.cs
│
├── Views/
│   ├── Home/
│   ├── Organizer/
│   ├── Participant/
│   ├── Category/
│   ├── Event/
│   ├── Enrollment/
│   └── Result/
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── images/
│   └── uploads/
│       ├── event-banners/
│       └── profile-pictures/
│
├── appsettings.json
├── Program.cs
└── README.md
```

*The structure above illustrates the main application components. Individual files and folders may vary as the project develops.*

## Installation and Setup

### Prerequisites

Before running the application, ensure that you have installed:

- Visual Studio 2022 or a compatible development environment.
- .NET 8 SDK.
- SQL Server or SQL Server LocalDB.
- The RaceDay Part 2 Web API project.
- A web browser.

### 1. Clone the Repository

Clone the repository to your local machine:

```bash
git clone https://github.com/imitha123/racedaypart3.git
```

Navigate into the project directory:

```bash
cd RaceDay_part_3
```

### 2. Configure the Web API Connection

Open `appsettings.json`.

Ensure that the API base URL points to the running Part 2 Web API.

Example:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7061/"
  }
}
```

The URL must match the address on which your API is running.

### 3. Start the Part 2 Web API

Open the RaceDay Part 2 API project and run it.

Ensure that the API is accessible before starting the MVC application.

The API is responsible for communicating with the SQL Server database.

### 4. Run the MVC Application

Open the RaceDay Part 3 solution in Visual Studio.

Build the solution to ensure that all dependencies are restored and the project compiles successfully.

Run the MVC application.

The application should open in your default browser.

If necessary, navigate to:

```text
https://localhost:<MVC-port>/
```

Use the MVC application's assigned port, not the API port.

## Authentication and Session Management

The application uses session state to maintain information about the currently logged-in user.

Session information includes:

- User ID.
- User name.
- Email address.
- User role.
- JWT authentication token.
- Participant profile image path.

The role information helps determine which dashboard and functionality should be accessible to the user.

Organisers and Participants are provided with different interfaces and permissions.

## Future Improvements

Possible future enhancements include:

- Email notifications for event registrations.
- Advanced event search and filtering.
- Event attendance tracking.
- More detailed performance statistics.
- Additional participant dashboard analytics.
- Cloud-based image storage.
- Enhanced reporting for organisers.

## Developer

**Name:** Imitha Maneli

**Project:** RaceDay – Part 3

**Framework:** ASP.NET Core MVC (.NET 8)

**Repository:** RaceDay Part 3

---

## Conclusion

RaceDay Part 3 demonstrates the development of an ASP.NET Core MVC application integrated with an existing RESTful Web API.

The application provides a centralised platform for sporting event management, supporting both organisers and participants through role-specific functionality.

By separating the presentation layer from backend operations, the project demonstrates the practical application of MVC architecture, API integration, session management, JWT authentication, database-driven functionality and local file storage.
