using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace poepart2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly string connectionString =
            @"Data Source=(localdb)\api_endpoints;Database=raceday;Trusted_Connection=True;";

        // GET: api/Category
        // Get all categories
        [HttpGet]
        public IActionResult GetAllCategories()
        {
            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT categoryID, categoryName, categoryDescription " +
                    "FROM Category";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                using SqlDataReader reader = command.ExecuteReader();

                var categories = new System.Collections.Generic.List<object>();

                while (reader.Read())
                {

                    categories.Add(new
                    {
                        categoryID = reader.GetInt32(0),
                        categoryName = reader.GetString(1).Trim(),
                        categoryDescription = reader.GetString(2).Trim()
                    });
                }

                return Ok(categories);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving categories."
                });
            }
        }

        // GET: api/Category/1
        // Get category by ID
        [HttpGet("{id:int}")]
        public IActionResult GetCategory(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Category ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "SELECT categoryID, categoryName, categoryDescription " +
                    "FROM Category WHERE categoryID = @id";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", id);

                using SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return NotFound(new
                    {
                        message = "Category not found."
                    });
                }

                var category = new
                {
                    categoryID = reader.GetInt32(0),
                    categoryName = reader.GetString(1).Trim(),
                    categoryDescription = reader.GetString(2).Trim()
                };

                return Ok(category);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving the category."
                });
            }
        }

        // POST: api/Category
        // Add category
        [HttpPost]
        public IActionResult AddCategory(
            [FromBody] CategoryRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new
                {
                    message = "Category name and description are required."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "INSERT INTO Category " +
                    "(categoryName, categoryDescription) " +
                    "VALUES (@name, @description); " +
                    "SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@name", request.Name.Trim());

                command.Parameters.AddWithValue(
                    "@description", request.Description.Trim());

                int categoryId = (int)command.ExecuteScalar()!;

                return CreatedAtAction(
                    nameof(GetCategory),
                    new { id = categoryId },
                    new
                    {
                        categoryID = categoryId,
                        categoryName = request.Name.Trim(),
                        categoryDescription = request.Description.Trim()
                    });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while adding the category."
                });
            }
        }

        // PUT: api/Category/1
        // Update category
        [HttpPut("{id:int}")]
        public IActionResult UpdateCategory(
            int id,
            [FromBody] CategoryRequest request)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Category ID must be greater than zero."
                });
            }

            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new
                {
                    message = "Category name and description are required."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "UPDATE Category " +
                    "SET categoryName = @name, " +
                    "categoryDescription = @description " +
                    "WHERE categoryID = @id";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue(
                    "@name", request.Name.Trim());

                command.Parameters.AddWithValue(
                    "@description", request.Description.Trim());

                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Category not found."
                    });
                }

                return Ok(new
                {
                    message = "Category updated successfully."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the category."
                });
            }
        }

        // DELETE: api/Category/1
        // Delete category
        [HttpDelete("{id:int}")]
        public IActionResult DeleteCategory(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "Category ID must be greater than zero."
                });
            }

            try
            {
                using SqlConnection connection =
                    new SqlConnection(connectionString);

                connection.Open();

                string query =
                    "DELETE FROM Category WHERE categoryID = @id";

                using SqlCommand command =
                    new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@id", id);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound(new
                    {
                        message = "Category not found."
                    });
                }

                return Ok(new
                {
                    message = "Category deleted successfully."
                });
            }
            catch (SqlException)
            {
                return Conflict(new
                {
                    message = "The category could not be deleted. It may be referenced by other records."
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while deleting the category."
                });
            }
        }
    }

    // Request model for adding and updating categories
    public class CategoryRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}