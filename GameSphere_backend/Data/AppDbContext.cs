﻿namespace GameSphere_backend.Data
{
    using GameSphere_backend.Models;
    using Microsoft.EntityFrameworkCore;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Defina os DbSets para as suas entidades
        public DbSet<User> Users { get; set; }

        public DbSet<Quizz> Quizzs { get; set; }
        //public DbSet<Quiz> Quizzes { get; set; }
        // Adicione outras entidades aqui
    }
}
