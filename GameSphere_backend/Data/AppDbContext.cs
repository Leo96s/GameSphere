namespace GameSphere_backend.Data
{
    using GameSphere_backend.Models.BackendModels;
    using Microsoft.EntityFrameworkCore;

    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Represents the session with the database and provides access to entity sets.
        /// </summary>
        /// <remarks>
        /// This class serves as the main data access point for the GameSphere application,
        /// inheriting from Microsoft.EntityFrameworkCore.DbContext. It contains DbSet properties
        /// for each entity that should be persisted to the database.
        /// </remarks>
        public class AppDbContext : DbContext
        {
            /// <summary>
            /// Initializes a new instance of the AppDbContext class.
            /// </summary>
            /// <param name="options">The options to be used by the DbContext.</param>
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            /// <summary>
            /// Gets or sets the Users entity set, which represents the collection of all User entities in the context.
            /// </summary>
            /// <remarks>
            /// This DbSet allows querying and saving instances of the User entity.
            /// Corresponds to the 'Users' table in the database.
            /// </remarks>
            public DbSet<User> Users { get; set; }

            /// <summary>
            /// Gets or sets the Quizzes entity set, which represents the collection of all Quiz entities in the context.
            /// </summary>
            /// <remarks>
            /// This DbSet allows querying and saving instances of the Quiz entity.
            /// Corresponds to the 'Quizzes' table in the database.
            /// Note: The property name 'Quizzs' is kept for backward compatibility.
            /// </remarks>
            public DbSet<Quizz> Quizzs { get; set; }

            /// <summary>
            /// Gets or sets the Requirements entity set, which represents the collection of all Requirement entities in the context.
            /// </summary>
            /// <remarks>
            /// This DbSet allows querying and saving instances of the Requirement entity.
            /// Corresponds to the 'Requirements' table in the database.
            /// </remarks>
            public DbSet<Requirement> Requirements { get; set; }

            /// <summary>
            /// Gets or sets the Games entity set, which represents the collection of all Game entities in the context.
            /// </summary>
            /// <remarks>
            /// This DbSet allows querying and saving instances of the Game entity.
            /// Corresponds to the 'Games' table in the database.
            /// </remarks>
            public DbSet<Game> Games { get; set; }

            /// <summary>
            /// Gets or sets the Achievements entity set, which represents the collection of all Achievement entities in the context.
            /// </summary>
            /// <remarks>
            /// This DbSet allows querying and saving instances of the Achievement entity.
            /// Corresponds to the 'Achievements' table in the database.
            /// Note: The singular form 'Achievement' might be more appropriate for the entity class name.
            /// </remarks>
            public DbSet<Achievements> Achievements { get; set; }

            /// <summary>
            /// Gets or sets the Scores entity set, which represents the collection of all Score entities in the context.
            /// </summary>
            /// <remarks>
            /// This DbSet allows querying and saving instances of the Score entity.
            /// Corresponds to the 'Scores' table in the database.
            /// </remarks>
            public DbSet<Score> Scores { get; set; }

            // Additional DbSets can be added here as the application grows
            // public DbSet<NewEntity> NewEntities { get; set; }

        }
    }
}
