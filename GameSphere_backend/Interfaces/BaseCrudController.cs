using GameSphere_backend.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GameSphere_backend.Interfaces
{
    /// <summary>
    /// Abstract base controller providing CRUD operations for entities.
    /// </summary>
    /// <typeparam name="T">The type of entity this controller handles.</typeparam>
    /// <remarks>
    /// This base class implements the core Create, Read, Update, Delete (CRUD) operations
    /// that can be inherited by specific entity controllers. It ensures consistent API behavior
    /// across all entity endpoints while allowing customization in derived controllers.
    /// </remarks>
    public abstract class BaseCrudController<T> : ResponseController
    {
        /// <summary>
        /// Initializes a new instance of the BaseCrudController class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        protected BaseCrudController(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Retrieves a single entity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the entity.</param>
        /// <returns>
        /// An IActionResult containing:
        /// - 200 OK with the entity if found
        /// - 404 Not Found if the entity doesn't exist
        /// - 500 Internal Server Error for unexpected failures
        /// </returns>
        public abstract Task<IActionResult> GetEntityById(int id);

        /// <summary>
        /// Creates a new entity in the system.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <returns>
        /// An IActionResult containing:
        /// - 201 Created with the new entity and location header if successful
        /// - 400 Bad Request if the entity is invalid
        /// - 409 Conflict if the entity already exists
        /// - 500 Internal Server Error for unexpected failures
        /// </returns>
        public abstract Task<IActionResult> CreateEntity(T entity);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to update.</param>
        /// <param name="entity">The updated entity data.</param>
        /// <returns>
        /// An IActionResult containing:
        /// - 200 OK with the updated entity if successful
        /// - 400 Bad Request if the entity is invalid
        /// - 404 Not Found if the entity doesn't exist
        /// - 500 Internal Server Error for unexpected failures
        /// </returns>
        public abstract Task<IActionResult> UpdateEntity(int id, T entity);

        /// <summary>
        /// Deletes an entity from the system.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to delete.</param>
        /// <returns>
        /// An IActionResult containing:
        /// - 204 No Content if deletion was successful
        /// - 404 Not Found if the entity doesn't exist
        /// - 500 Internal Server Error for unexpected failures
        /// </returns>
        public abstract Task<IActionResult> DeleteEntity(int id);
    }
}
