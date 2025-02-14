﻿// <auto-generated />
using System;
using GameSphere_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameSphere_backend.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Achievements", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NameOfAchievement")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Achievements");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Game", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("TypoOfGame")
                        .HasColumnType("integer");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Question", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.PrimitiveCollection<string[]>("Answers")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<string>("CorrectAnswer")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("QuizzId")
                        .HasColumnType("integer");

                    b.Property<int>("TypeOfAnswer")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("QuizzId");

                    b.ToTable("Question");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Quizz", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("Difficulty")
                        .HasColumnType("integer");

                    b.Property<int>("NumberOfQuests")
                        .HasColumnType("integer");

                    b.Property<DateTime>("RegistrationDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Quizzs");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Requirement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AchievementId")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AchievementId");

                    b.ToTable("Requirements");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Score", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int?>("GameId")
                        .HasColumnType("integer");

                    b.Property<float>("Points")
                        .HasColumnType("real");

                    b.Property<int?>("QuizzId")
                        .HasColumnType("integer");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.HasIndex("QuizzId");

                    b.HasIndex("UserId");

                    b.ToTable("Scores");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("Gender")
                        .HasColumnType("integer");

                    b.Property<string>("HashedPassword")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Image")
                        .HasColumnType("text");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("Level")
                        .HasColumnType("integer");

                    b.Property<DateTime>("RegistrationDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Token")
                        .HasColumnType("text");

                    b.Property<DateTime?>("TokenExpDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("TotalPoints")
                        .HasColumnType("bigint");

                    b.Property<bool>("isActive")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Achievements", b =>
                {
                    b.HasOne("GameSphere_backend.Models.BackendModels.User", "User")
                        .WithMany("Achievements")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Game", b =>
                {
                    b.HasOne("GameSphere_backend.Models.BackendModels.User", null)
                        .WithMany("Games")
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Question", b =>
                {
                    b.HasOne("GameSphere_backend.Models.BackendModels.Quizz", "Quizz")
                        .WithMany("Questions")
                        .HasForeignKey("QuizzId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Quizz");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Quizz", b =>
                {
                    b.HasOne("GameSphere_backend.Models.BackendModels.User", "User")
                        .WithMany("Quizzs")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Requirement", b =>
                {
                    b.HasOne("GameSphere_backend.Models.BackendModels.Achievements", "Achievement")
                        .WithMany("RequirementsForUnlock")
                        .HasForeignKey("AchievementId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Achievement");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Score", b =>
                {
                    b.HasOne("GameSphere_backend.Models.BackendModels.Game", "Game")
                        .WithMany("Scores")
                        .HasForeignKey("GameId");

                    b.HasOne("GameSphere_backend.Models.BackendModels.Quizz", "Quizz")
                        .WithMany("Scores")
                        .HasForeignKey("QuizzId");

                    b.HasOne("GameSphere_backend.Models.BackendModels.User", "User")
                        .WithMany("Scores")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Game");

                    b.Navigation("Quizz");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Achievements", b =>
                {
                    b.Navigation("RequirementsForUnlock");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Game", b =>
                {
                    b.Navigation("Scores");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.Quizz", b =>
                {
                    b.Navigation("Questions");

                    b.Navigation("Scores");
                });

            modelBuilder.Entity("GameSphere_backend.Models.BackendModels.User", b =>
                {
                    b.Navigation("Achievements");

                    b.Navigation("Games");

                    b.Navigation("Quizzs");

                    b.Navigation("Scores");
                });
#pragma warning restore 612, 618
        }
    }
}
