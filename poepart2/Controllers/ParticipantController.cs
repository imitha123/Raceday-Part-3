using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

using poepart2.Model;
using poepart2.Services;

namespace poepart2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParticipantController : ControllerBase
    {
        private readonly string connectionString =
            @"Data Source=(localdb)\api_endpoints;Database=raceday;Trusted_Connection=True;";

        private readonly PasswordService passwordService;
        private readonly IConfiguration configuration;

        public ParticipantController(
            PasswordService passwordService,
            IConfiguration configuration)
        {
            this.passwordService = passwordService;
            this.configuration = configuration;
        }

        // Validate email address
        private bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) &&
                   email.EndsWith(
                       "@gmail.com",
                       StringComparison.OrdinalIgnoreCase);
        }

        // Validate password
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(c => !char.IsLetterOrDigit(c));
        }

        // Generate JWT token for a Participant
        private string GenerateJwtToken(int participantId, string email)
        {
            string? jwtKey = configuration["Jwt:Key"];
            string? jwtIssuer = configuration["Jwt:Issuer"];
            string? jwtAudience = configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey) ||
                string.IsNullOrWhiteSpace(jwtIssuer) ||
                string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException(
                    "JWT configuration is missing.");
            }

            // The signing key must be Base64 encoded,
            // matching the configuration used by Program.cs.
            byte[] keyBytes = Convert.FromBase64String(jwtKey);

            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException(
                    "The JWT signing key must contain at least 32 bytes.");
            }

            var securityKey = new SymmetricSecurityKey(keyBytes);

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    participantId.ToString()),

                new Claim(
                    ClaimTypes.Email,
                    email),

                new Claim(
                    ClaimTypes.Role,
                    "Participant")
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Get the authenticated Participant's ID from their JWT
        private bool TryGetParticipantId(out int participantId)
        {
            string? id = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(id, out participantId);
        }

        // POST: api/Participant/Register
        // Register a new Participant
        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult AddParticipant(
            [FromQuery] string participantName,
            [FromQuery] string participantSurname,
            [FromQuery] string email,
            [FromQuery] string password)
        {
            if (string.IsNullOrWhiteSpace(participantName) ||
                string.IsNullOrWhiteSpace(participantSurname))
            {
                return BadRequest(new
                {
                    message = "Participant name and surname are required."
                });
            }

            if (!IsValidEmail(email))
            {
                return BadRequest(new
                {
                    message = "Email must end with @gmail.com."
                });
            }

            if (!IsValidPassword(password))
            {
                return BadRequest(new
                {
                    message = "Password must contain at least 8 characters, " +
                              "an uppercase letter, a lowercase letter, " +
                              "a number, and a special character."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Check whether the email already exists
                string checkQuery =
                    "SELECT COUNT(*) FROM Participant WHERE LTRIM(RTRIM(Email)) = @email";

                using (SqlCommand checkCommand =
                    new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@email", email);

                    int existingParticipants =
                        (int)checkCommand.ExecuteScalar()!;

                    if (existingParticipants > 0)
                    {
                        return Conflict(new
                        {
                            message = "A participant with this email already exists."
                        });
                    }
                }

                // Hash the password before storing it
                string hashedPassword =
                    passwordService.HashPassword(password);

                string insertQuery =
                    "INSERT INTO Participant " +
                    "(Participantname, Participantsurname, Email, Password) " +
                    "VALUES (@name, @surname, @email, @password); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using SqlCommand command =
                    new SqlCommand(insertQuery, connection);

                command.Parameters.AddWithValue(
                    "@name", participantName.Trim());

                command.Parameters.AddWithValue(
                    "@surname", participantSurname.Trim());

                command.Parameters.AddWithValue(
                    "@email", email.Trim());

                command.Parameters.AddWithValue(
                    "@password", hashedPassword);

                int participantId = (int)command.ExecuteScalar()!;

                return StatusCode(201, new
                {
                    message = "Participant registered successfully.",
                    participantId,
                    email = email.Trim()
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while registering the participant."
                });
            }
        }

        // POST: api/Participant/Login
        // Login and return a JWT token
        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(
            [FromBody] ParticipantLoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new
                {
                    message = "Email must end with @gmail.com."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT participantID, Participantname, " +
                    "Participantsurname, Email, Password " +
                    "FROM Participant " +
                    "WHERE LTRIM(RTRIM(Email)) = @email";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@email", request.Email.Trim());

                using SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email or password."
                    });
                }

                int participantId = reader.GetInt32(0);

                string name = reader.GetString(1).Trim();
                string surname = reader.GetString(2).Trim();
                string email = reader.GetString(3).Trim();
                string storedPassword = reader.GetString(4).TrimEnd();

                // Verify the supplied password against the stored hash
                bool passwordIsValid =
                    passwordService.VerifyPassword(
                        storedPassword,
                        request.Password);

                if (!passwordIsValid)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email or password."
                    });
                }

                // Close the reader before generating the response
                reader.Close();

                string token = GenerateJwtToken(
                    participantId,
                    email);

                return Ok(new
                {
                    message = "Participant login successful.",
                    participantId,
                    name,
                    surname,
                    email,
                    role = "Participant",
                    tokenType = "Bearer",
                    token
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while logging in."
                });
            }
        }

        // PUT: api/Participant/1
        // A Participant can update only their own account
        [Authorize(Roles = "Participant")]
        [HttpPut("{id}")]
        public IActionResult UpdateParticipant(
            [FromRoute] int id,
            [FromBody] ParticipantModel updatedParticipant)
        {
            if (updatedParticipant == null)
            {
                return BadRequest(new
                {
                    message = "Invalid participant details."
                });
            }

            if (!TryGetParticipantId(out int authenticatedId))
            {
                return Unauthorized();
            }

            if (authenticatedId != id)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(updatedParticipant.Participantname) ||
                string.IsNullOrWhiteSpace(updatedParticipant.Participantsurname))
            {
                return BadRequest(new
                {
                    message = "Participant name and surname are required."
                });
            }

            if (!IsValidEmail(updatedParticipant.participantEmail))
            {
                return BadRequest(new
                {
                    message = "Email must end with @gmail.com."
                });
            }

            if (!IsValidPassword(updatedParticipant.participantPassword))
            {
                return BadRequest(new
                {
                    message = "Password must contain at least 8 characters, " +
                              "an uppercase letter, a lowercase letter, " +
                              "a number, and a special character."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Hash the new password before updating the database
                string hashedPassword =
                    passwordService.HashPassword(
                        updatedParticipant.participantPassword);

                string query =
                    "UPDATE Participant SET " +
                    "Participantname = @name, " +
                    "Participantsurname = @surname, " +
                    "Email = @email, " +
                    "Password = @password " +
                    "WHERE participantID = @id";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@name",
                    updatedParticipant.Participantname.Trim());

                command.Parameters.AddWithValue(
                    "@surname",
                    updatedParticipant.Participantsurname.Trim());

                command.Parameters.AddWithValue(
                    "@email",
                    updatedParticipant.participantEmail.Trim());

                command.Parameters.AddWithValue(
                    "@password",
                    hashedPassword);

                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Participant not found."
                    });
                }

                return Ok(new
                {
                    message = "Participant updated successfully."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the participant."
                });
            }
        }

        // DELETE: api/Participant/1
        // A Participant can delete only their own account
        [Authorize(Roles = "Participant")]
        [HttpDelete("{id}")]
        public IActionResult DeleteParticipant(
            [FromRoute] int id)
        {
            if (!TryGetParticipantId(out int authenticatedId))
            {
                return Unauthorized();
            }

            if (authenticatedId != id)
            {
                return Forbid();
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "DELETE FROM Participant WHERE participantID = @id";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Participant not found."
                    });
                }

                return Ok(new
                {
                    message = "Participant deleted successfully."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while deleting the participant."
                });
            }
        }

        // GET: api/Participant/Profile
        // Retrieve the logged-in participant's profile
        [Authorize(Roles = "Participant")]
        [HttpGet("Profile")]
        public IActionResult GetParticipantProfile()
        {
            if (!TryGetParticipantId(out int participantId))
            {
                return Unauthorized();
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT participantID, Participantname, " +
                    "Participantsurname, Email, ProfileImagePath " +
                    "FROM Participant WHERE participantID = @id";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", participantId);

                using SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new
                    {
                        message = "Participant not found."
                    });
                }

                return Ok(new
                {
                    participantId = reader.GetInt32(0),
                    participantName = reader.GetString(1).Trim(),
                    participantSurname = reader.GetString(2).Trim(),
                    email = reader.GetString(3).Trim(),
                    profileImagePath = reader.IsDBNull(4)
                        ? null
                        : reader.GetString(4).Trim()
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving the profile."
                });
            }
        }


        // PUT: api/Participant/Profile
        // Update the logged-in participant's personal information
        [Authorize(Roles = "Participant")]
        [HttpPut("Profile")]
        public IActionResult UpdateParticipantProfile(
            [FromBody] ParticipantProfileRequest request)
        {
            if (request == null)
            {
                return BadRequest();
            }

            if (!TryGetParticipantId(out int participantId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Participantname) ||
                string.IsNullOrWhiteSpace(request.Participantsurname))
            {
                return BadRequest(new
                {
                    message = "Name and surname are required."
                });
            }

            if (!IsValidEmail(request.ParticipantEmail))
            {
                return BadRequest(new
                {
                    message = "Email must end with @gmail.com."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                // Check whether another participant uses this email
                string checkQuery =
                    "SELECT COUNT(*) FROM Participant " +
                    "WHERE LTRIM(RTRIM(Email)) = @email " +
                    "AND participantID <> @id";

                using (SqlCommand checkCommand =
                    new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue(
                        "@email", request.ParticipantEmail.Trim());

                    checkCommand.Parameters.AddWithValue(
                        "@id", participantId);

                    int existingEmail =
                        (int)checkCommand.ExecuteScalar()!;

                    if (existingEmail > 0)
                    {
                        return Conflict(new
                        {
                            message = "This email address is already in use."
                        });
                    }
                }

                string updateQuery =
                    "UPDATE Participant SET " +
                    "Participantname = @name, " +
                    "Participantsurname = @surname, " +
                    "Email = @email, " +
                    "ProfileImagePath = @imagePath " +
                    "WHERE participantID = @id";

                using SqlCommand command =
                    new SqlCommand(updateQuery, connection);

                command.Parameters.AddWithValue(
                    "@name", request.Participantname.Trim());

                command.Parameters.AddWithValue(
                    "@surname", request.Participantsurname.Trim());

                command.Parameters.AddWithValue(
                    "@email", request.ParticipantEmail.Trim());

                command.Parameters.AddWithValue(
                    "@imagePath",
                    (object?)request.ProfileImagePath ?? DBNull.Value);

                command.Parameters.AddWithValue("@id", participantId);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    message = "Profile updated successfully."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the profile."
                });
            }
        }
    }

    // Request model used by the Participant login endpoint
    public class ParticipantLoginRequest
    {
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}