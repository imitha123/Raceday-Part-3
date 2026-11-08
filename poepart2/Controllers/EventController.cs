using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using poepart2.Model;

namespace poepart2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly string connectionString =
        @"Data Source=(localdb)\api_endpoints;Database=raceday;Trusted_Connection=True;";


    // GET: api/Event
    // Get all events
    [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAllEvents()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string query = @"
                SELECT eventId, eventName, eventLocation, eventDate,
                       categoryID, organizerID
                FROM Event_";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                var events = new List<object>();

                while (reader.Read())
                {
                    events.Add(new
                    {
                        EventID = reader.GetInt32(0),
                        EventName = reader.GetString(1).Trim(),
                        EventLocation = reader.GetString(2).Trim(),
                        EventDate = reader.GetValue(3),
                        CategoryID = reader.GetInt32(4),
                        OrganizerID = reader.GetInt32(5)
                    });
                }

                return Ok(events);
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while retrieving events."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while retrieving events."
                });
            }
        }

        // GET: api/Event/1
        // Get event by ID
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public IActionResult GetEventById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Event ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string query = @"
                SELECT eventId, eventName, eventLocation, eventDate,
                       categoryID, organizerID
                FROM Event_
                WHERE eventId = @id";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", id);

                using SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new
                    {
                        message = "Event not found."
                    });
                }

                var eventDetails = new
                {
                    EventID = reader.GetInt32(0),
                    EventName = reader.GetString(1).Trim(),
                    EventLocation = reader.GetString(2).Trim(),
                    EventDate = reader.GetValue(3),
                    CategoryID = reader.GetInt32(4),
                    OrganizerID = reader.GetInt32(5)
                };

                return Ok(eventDetails);
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while retrieving the event."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while retrieving the event."
                });
            }
        }

        // POST: api/Event
        // Add event
        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public IActionResult AddEvent([FromBody] EventModel newEvent)
        {
            if (newEvent == null ||
                string.IsNullOrWhiteSpace(newEvent.EventName) ||
                string.IsNullOrWhiteSpace(newEvent.EventLocation) ||
                newEvent.categoryID <= 0 ||
                newEvent.organizerID <= 0)
            {
                return BadRequest(new
                {
                    message = "Valid event name, location, category ID and organizer ID are required."
                });
            }

            if (!DateTime.TryParse(
                    Convert.ToString(newEvent.EventDate),
                    out DateTime parsedEventDate))
            {
                return BadRequest(new
                {
                    message = "A valid event date is required."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                // Confirm that the category exists.
                string categoryQuery = @"
                SELECT COUNT(*)
                FROM Category
                WHERE categoryID = @categoryID";

                using SqlCommand categoryCommand =
                    new SqlCommand(categoryQuery, connection);

                categoryCommand.Parameters.AddWithValue(
                    "@categoryID", newEvent.categoryID);

                if ((int)categoryCommand.ExecuteScalar()! == 0)
                {
                    return BadRequest(new
                    {
                        message = "The specified category does not exist."
                    });
                }

                // Confirm that the organizer exists.
                string organizerQuery = @"
                SELECT COUNT(*)
                FROM Organizer
                WHERE organizerID = @organizerID";

                using SqlCommand organizerCommand =
                    new SqlCommand(organizerQuery, connection);

                organizerCommand.Parameters.AddWithValue(
                    "@organizerID", newEvent.organizerID);

                if ((int)organizerCommand.ExecuteScalar()! == 0)
                {
                    return BadRequest(new
                    {
                        message = "The specified organizer does not exist."
                    });
                }

                string query = @"
                INSERT INTO Event_
                    (eventName, eventLocation, eventDate, categoryID, organizerID)
                VALUES
                    (@name, @location, @date, @categoryID, @organizerID);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@name", newEvent.EventName.Trim());
                command.Parameters.AddWithValue("@location", newEvent.EventLocation.Trim());
                command.Parameters.AddWithValue("@date", parsedEventDate);
                command.Parameters.AddWithValue("@categoryID", newEvent.categoryID);
                command.Parameters.AddWithValue("@organizerID", newEvent.organizerID);

                int eventId = (int)command.ExecuteScalar()!;

                return CreatedAtAction(
                    nameof(GetEventById),
                    new { id = eventId },
                    new
                    {
                        EventID = eventId,
                        EventName = newEvent.EventName.Trim(),
                        EventLocation = newEvent.EventLocation.Trim(),
                        EventDate = parsedEventDate,
                        CategoryID = newEvent.categoryID,
                        OrganizerID = newEvent.organizerID,
                        Message = "Event added successfully."
                    });
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while adding the event."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while adding the event."
                });
            }
        }

        // PUT: api/Event/1
        // Update event by ID
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organizer")]
        public IActionResult UpdateEvent(
            int id,
            [FromBody] EventModel updatedEvent)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Event ID must be greater than zero."
                });
            }

            if (updatedEvent == null ||
                string.IsNullOrWhiteSpace(updatedEvent.EventName) ||
                string.IsNullOrWhiteSpace(updatedEvent.EventLocation))
            {
                return BadRequest(new
                {
                    message = "Valid event name and location are required."
                });
            }

            if (!DateTime.TryParse(
                    Convert.ToString(updatedEvent.EventDate),
                    out DateTime parsedEventDate))
            {
                return BadRequest(new
                {
                    message = "A valid event date is required."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                // Confirm that the event exists.
                string existsQuery = @"
                SELECT COUNT(*)
                FROM Event_
                WHERE eventId = @id";

                using SqlCommand existsCommand =
                    new SqlCommand(existsQuery, connection);

                existsCommand.Parameters.AddWithValue("@id", id);

                if ((int)existsCommand.ExecuteScalar()! == 0)
                {
                    return NotFound(new
                    {
                        message = "Event not found."
                    });
                }



                string query = @"
               UPDATE Event_
SET eventName = @name,
    eventLocation = @location,
    eventDate = @date,
    categoryID = @categoryID
WHERE eventId = @id";

                using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@name", updatedEvent.EventName.Trim());
                command.Parameters.AddWithValue("@location", updatedEvent.EventLocation.Trim());
                command.Parameters.AddWithValue("@date", parsedEventDate);
                command.Parameters.AddWithValue("@categoryID",updatedEvent.categoryID);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Event not found."
                    });
                }

                return Ok(new
                {
                    message = "Event updated successfully."
                });
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while updating the event."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while updating the event."
                });
            }
        }

        // DELETE: api/Event/1
        // Delete event by ID
        [HttpDelete("{eventId:int}")]
        [Authorize(Roles = "Organizer")]
        public IActionResult DeleteEvent(int eventId)
        {
            if (eventId <= 0)
            {
                return BadRequest(new
                {
                    message = "Event ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string query = @"
                DELETE FROM Event_
                WHERE eventId = @id";

                using SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", eventId);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Event not found."
                    });
                }

                return Ok(new
                {
                    message = "Event deleted successfully."
                });
            }
            catch (SqlException)
            {
                return Conflict(new
                {
                    message = "The event could not be deleted. It may be referenced by enrollments or results."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while deleting the event."
                });
            }
        }
    }


}
