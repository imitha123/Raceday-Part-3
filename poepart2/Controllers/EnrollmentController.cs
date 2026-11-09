using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace poepart2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly string connectionString =
        @"Data Source=(localdb)\api_endpoints;Database=raceday;Trusted_Connection=True;";


    // GET: api/Enrollment
    // Get all enrollments
    [HttpGet]
        public IActionResult GetAllEnrollments()
        {
            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string query = @"
SELECT EventID, ParticipantID, Status
FROM Enrollment";

                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();

                var enrollments = new List<object>();

                while (reader.Read())
                {
                    enrollments.Add(new
                    {
                        EventID = reader.GetInt32(0),
                        ParticipantID = reader.GetInt32(1),
                        Status = reader.GetString(2)
                    });
                }

                return Ok(enrollments);
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while retrieving enrollments."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while retrieving enrollments."
                });
            }
        }

        // GET: api/Enrollment/1/2
        // Get an enrollment by event ID and participant ID
        [HttpGet("{eventId:int}/{participantId:int}")]
        public IActionResult GetEnrollment(int eventId, int participantId)
        {
            if (eventId <= 0 || participantId <= 0)
            {
                return BadRequest(new
                {
                    message = "Event ID and participant ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string query = @"
SELECT EventID, ParticipantID, Status
FROM Enrollment
WHERE EventID = @eventId
AND ParticipantID = @participantId";

                using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@eventId", eventId);
                command.Parameters.AddWithValue("@participantId", participantId);

                using SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new
                    {
                        message = "Enrollment not found."
                    });
                }

                var enrollment = new
                {
                    EventID = reader.GetInt32(0),
                    ParticipantID = reader.GetInt32(1),
                    Status = reader.GetString(2)
                };

                return Ok(enrollment);
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while retrieving the enrollment."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while retrieving the enrollment."
                });
            }
        }

        // POST: api/Enrollment?eventId=1&participantId=2
        // Add an enrollment
        [HttpPost]
        public IActionResult AddEnrollment(
            [FromQuery] int eventId,
            [FromQuery] int participantId)
        {
            if (eventId <= 0 || participantId <= 0)
            {
                return BadRequest(new
                {
                    message = "Event ID and participant ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                // Check whether the event exists.
                string eventQuery = @"
                SELECT COUNT(*)
                FROM Event_
                WHERE EventID = @eventId";

                using SqlCommand eventCommand =
                    new SqlCommand(eventQuery, connection);

                eventCommand.Parameters.AddWithValue("@eventId", eventId);

                int eventExists = (int)eventCommand.ExecuteScalar()!;

                if (eventExists == 0)
                {
                    return BadRequest(new
                    {
                        message = "The specified event does not exist."
                    });
                }

                // Check whether the participant exists.
                string participantQuery = @"
                SELECT COUNT(*)
                FROM Participant
                WHERE participantID = @participantId";

                using SqlCommand participantCommand =
                    new SqlCommand(participantQuery, connection);

                participantCommand.Parameters.AddWithValue(
                    "@participantId", participantId);

                int participantExists =
                    (int)participantCommand.ExecuteScalar()!;

                if (participantExists == 0)
                {
                    return BadRequest(new
                    {
                        message = "The specified participant does not exist."
                    });
                }

                // Prevent duplicate enrollments.
                string duplicateQuery = @"
                SELECT COUNT(*)
                FROM Enrollment
                WHERE EventID = @eventId
                AND ParticipantID = @participantId";

                using SqlCommand duplicateCommand =
                    new SqlCommand(duplicateQuery, connection);

                duplicateCommand.Parameters.AddWithValue("@eventId", eventId);
                duplicateCommand.Parameters.AddWithValue("@participantId", participantId);

                int duplicateExists =
                    (int)duplicateCommand.ExecuteScalar()!;

                if (duplicateExists > 0)
                {
                    return Conflict(new
                    {
                        message = "This participant is already enrolled in this event."
                    });
                }

                string insertQuery = @"
                INSERT INTO Enrollment (EventID, ParticipantID)
                VALUES (@eventId, @participantId)";

                using SqlCommand insertCommand =
                    new SqlCommand(insertQuery, connection);

                insertCommand.Parameters.AddWithValue("@eventId", eventId);
                insertCommand.Parameters.AddWithValue("@participantId", participantId);

                int rowsAffected = insertCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return CreatedAtAction(
                        nameof(GetEnrollment),
                        new { eventId, participantId },
                       new
                       {
                           EventID = eventId,
                           ParticipantID = participantId,
                           Status = "Pending",
                           Message = "Enrollment added successfully."
                       });
                }

                return StatusCode(500, new
                {
                    message = "Enrollment could not be added."
                });
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while adding the enrollment."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while adding the enrollment."
                });
            }
        }

        // PUT: api/Enrollment/1/2?newEventId=3&newParticipantId=4
        // Update an enrollment
        [HttpPut("{eventId:int}/{participantId:int}")]
        public IActionResult UpdateEnrollment(
            int eventId,
            int participantId,
            [FromQuery] int newEventId,
            [FromQuery] int newParticipantId)
        {
            if (eventId <= 0 || participantId <= 0 ||
                newEventId <= 0 || newParticipantId <= 0)
            {
                return BadRequest(new
                {
                    message = "All event and participant IDs must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                // Check that the original enrollment exists.
                string existingQuery = @"
                SELECT COUNT(*)
                FROM Enrollment
                WHERE EventID = @eventId
                AND ParticipantID = @participantId";

                using SqlCommand existingCommand =
                    new SqlCommand(existingQuery, connection);

                existingCommand.Parameters.AddWithValue("@eventId", eventId);
                existingCommand.Parameters.AddWithValue("@participantId", participantId);

                int enrollmentExists =
                    (int)existingCommand.ExecuteScalar()!;

                if (enrollmentExists == 0)
                {
                    return NotFound(new
                    {
                        message = "The enrollment to update was not found."
                    });
                }

                // Check that the new event exists.
                string eventQuery = @"
                SELECT COUNT(*)
                FROM Event_
                WHERE EventID = @newEventId";

                using SqlCommand eventCommand =
                    new SqlCommand(eventQuery, connection);

                eventCommand.Parameters.AddWithValue("@newEventId", newEventId);

                if ((int)eventCommand.ExecuteScalar()! == 0)
                {
                    return BadRequest(new
                    {
                        message = "The specified new event does not exist."
                    });
                }

                // Check that the new participant exists.
                string participantQuery = @"
                SELECT COUNT(*)
                FROM Participant
                WHERE participantID = @newParticipantId";

                using SqlCommand participantCommand =
                    new SqlCommand(participantQuery, connection);

                participantCommand.Parameters.AddWithValue(
                    "@newParticipantId", newParticipantId);

                if ((int)participantCommand.ExecuteScalar()! == 0)
                {
                    return BadRequest(new
                    {
                        message = "The specified new participant does not exist."
                    });
                }

                // Prevent a duplicate composite key.
                string duplicateQuery = @"
                SELECT COUNT(*)
                FROM Enrollment
                WHERE EventID = @newEventId
                AND ParticipantID = @newParticipantId
                AND NOT (
                    EventID = @eventId
                    AND ParticipantID = @participantId
                )";

                using SqlCommand duplicateCommand =
                    new SqlCommand(duplicateQuery, connection);

                duplicateCommand.Parameters.AddWithValue("@newEventId", newEventId);
                duplicateCommand.Parameters.AddWithValue(
                    "@newParticipantId", newParticipantId);
                duplicateCommand.Parameters.AddWithValue("@eventId", eventId);
                duplicateCommand.Parameters.AddWithValue(
                    "@participantId", participantId);

                if ((int)duplicateCommand.ExecuteScalar()! > 0)
                {
                    return Conflict(new
                    {
                        message = "The participant is already enrolled in the specified event."
                    });
                }

                string updateQuery = @"
                UPDATE Enrollment
                SET EventID = @newEventId,
                    ParticipantID = @newParticipantId
                WHERE EventID = @eventId
                AND ParticipantID = @participantId";

                using SqlCommand updateCommand =
                    new SqlCommand(updateQuery, connection);

                updateCommand.Parameters.AddWithValue("@newEventId", newEventId);
                updateCommand.Parameters.AddWithValue(
                    "@newParticipantId", newParticipantId);
                updateCommand.Parameters.AddWithValue("@eventId", eventId);
                updateCommand.Parameters.AddWithValue(
                    "@participantId", participantId);

                int rowsAffected = updateCommand.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new
                    {
                        EventID = newEventId,
                        ParticipantID = newParticipantId,
                        Message = "Enrollment updated successfully."
                    });
                }

                return NotFound(new
                {
                    message = "Enrollment not found."
                });
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while updating the enrollment."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while updating the enrollment."
                });
            }
        }

        // DELETE: api/Enrollment/1/2
        // Delete an enrollment
        [HttpDelete("{eventId:int}/{participantId:int}")]
        public IActionResult DeleteEnrollment(int eventId, int participantId)
        {
            if (eventId <= 0 || participantId <= 0)
            {
                return BadRequest(new
                {
                    message = "Event ID and participant ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();

                string query = @"
                DELETE FROM Enrollment
                WHERE EventID = @eventId
                AND ParticipantID = @participantId";

                using SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@eventId", eventId);
                command.Parameters.AddWithValue("@participantId", participantId);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Enrollment not found."
                    });
                }

                return Ok(new
                {
                    message = "Enrollment deleted successfully."
                });
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while deleting the enrollment."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred while deleting the enrollment."
                });
            }
        }
        // PUT: api/Enrollment/1/2/status?status=Confirmed
        // Update enrollment status
        [HttpPut("{eventId:int}/{participantId:int}/status")]
        public IActionResult UpdateEnrollmentStatus(
            int eventId,
            int participantId,
            [FromQuery] string status)
        {
            if (eventId <= 0 || participantId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid event or participant ID."
                });
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new
                {
                    message = "Enrollment status is required."
                });
            }

            status = status.Trim();

            if (status != "Pending" && status != "Confirmed")
            {
                return BadRequest(new
                {
                    message = "Status must be Pending or Confirmed."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query = @"
            UPDATE Enrollment
            SET Status = @status
            WHERE EventID = @eventId
            AND ParticipantID = @participantId";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@eventId", eventId);
                command.Parameters.AddWithValue("@participantId", participantId);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Enrollment not found."
                    });
                }

                return Ok(new
                {
                    EventID = eventId,
                    ParticipantID = participantId,
                    Status = status,
                    Message = "Enrollment status updated successfully."
                });
            }
            catch (SqlException)
            {
                return StatusCode(500, new
                {
                    message = "A database error occurred while updating enrollment status."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An unexpected error occurred."
                });
            }
        }
    }


}
